using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MetroScheduler.Domain.Entities;
using MetroScheduler.Infrastructure;

namespace MetroScheduler.Infrastructure.Services;

public interface IMetroUpsertService
{
    Task RunFullRefreshAsync(CancellationToken ct = default);
}

public sealed class MetroUpsertService : IMetroUpsertService
{
    private readonly MetroDbContext _db;

    public MetroUpsertService(MetroDbContext db)
    {
        _db = db;
    }

    public async Task RunFullRefreshAsync(CancellationToken ct = default)
    {
        using var client = new HttpClient { BaseAddress = new Uri("https://api-sarv.shiraz.ir") };
        client.DefaultRequestHeaders.Add("Authorization", "6b87277da5b047bd9b680122e943df3d9eba1626bd424dd993589322e09d6f9f");

        // 1) Lines
        var lines = await GetAsync<List<MetroLineDto>>(client, "/api/v1/metro/line", ct) ?? new();
        foreach (var line in lines)
        {
            await UpsertLineAsync(line, ct);
            for (int idx = 0; idx < line.Stations.Count; idx++)
            {
                var st = line.Stations[idx];
                st.MetroLineId = line.Id;
                st.OrderIndex = idx; // preserve order as API returns
                await UpsertStationAsync(st, ct);
            }
        }
        await _db.SaveChangesAsync(ct);

        // 2) Stations per line (overrides/extends)
        foreach (var line in lines)
        {
            var stations = await GetAsync<List<MetroStation>>(client, $"/api/v1/metro/station?lineId={Uri.EscapeDataString(line.Id)}", ct) ?? new();
            // assign order here as well based on appearance order if not set
            for (int idx = 0; idx < stations.Count; idx++)
            {
                var st = stations[idx];
                if (st.OrderIndex is null) st.OrderIndex = idx;
                await UpsertStationAsync(st, ct);
            }
        }
        await _db.SaveChangesAsync(ct);

        // 3) Scheduling per station per line for both holidays
        foreach (var line in lines)
        {
            var stationIds = await _db.MetroStations.AsNoTracking()
                .Where(s => s.MetroLineId == line.Id)
                .OrderBy(s => s.OrderIndex)
                .Select(s => s.Id)
                .ToListAsync(ct);

            foreach (var stationId in stationIds)
            {
                foreach (var isHoliday in new[] { true, false })
                {
                    var schedules = await GetAsync<List<StationSchedule>>(client, $"/api/v1/metro/{Uri.EscapeDataString(line.Id)}/station/{Uri.EscapeDataString(stationId)}/scheduling/?isHoliday={(isHoliday ? "true" : "false")}", ct) ?? new();

                    // Preserve API order strictly: first item = ListNo=1, ...
                    for (int i = 0; i < schedules.Count; i++)
                    {
                        var sch = schedules[i];
                        sch.IsHoliday = isHoliday;
                        sch.ListKey = BuildListKey(sch);
                        sch.ListNo = i + 1;
                    }

                    foreach (var sch in schedules)
                    {
                        var local = _db.StationSchedules.Local.FirstOrDefault(x =>
                            x.StationId == sch.StationId && x.IsHoliday == sch.IsHoliday && x.ListKey == sch.ListKey);
                        if (local is not null)
                        {
                            local.StartStation = sch.StartStation;
                            local.EndStation = sch.EndStation;
                            local.Name = sch.Name;
                            local.Interval = sch.Interval;
                            local.StartTimeToEndStation = sch.StartTimeToEndStation;
                            local.ListNo = sch.ListNo;
                            continue;
                        }

                        var existing = await _db.StationSchedules.FirstOrDefaultAsync(x =>
                            x.StationId == sch.StationId && x.IsHoliday == sch.IsHoliday && x.ListKey == sch.ListKey, ct);
                        if (existing is null)
                        {
                            _db.StationSchedules.Add(sch);
                        }
                        else
                        {
                            existing.StartStation = sch.StartStation;
                            existing.EndStation = sch.EndStation;
                            existing.Name = sch.Name;
                            existing.Interval = sch.Interval;
                            existing.StartTimeToEndStation = sch.StartTimeToEndStation;
                            existing.ListNo = sch.ListNo;
                        }
                    }
                }
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private static string BuildListKey(StationSchedule sch)
    {
        var sb = new StringBuilder();
        sb.Append(sch.StartStation).Append("->").Append(sch.EndStation).Append('|');
        sb.Append(sch.Interval?.ToString() ?? "").Append('|');
        if (sch.StartTimeToEndStation is { Count: > 0 })
        {
            var times = sch.StartTimeToEndStation;
            var sample = times.Take(3).Concat(times.Skip(Math.Max(0, times.Count - 3))).ToArray();
            sb.Append(string.Join(',', sample));
        }
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(hash)[..24];
    }

    private async Task<T?> GetAsync<T>(HttpClient client, string path, CancellationToken ct)
    {
        using var resp = await client.GetAsync(path, ct);
        var json = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode)
        {
            return default;
        }
        try
        {
            return JsonSerializer.Deserialize<T>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return default;
        }
    }

    private async Task UpsertLineAsync(MetroLineDto dto, CancellationToken ct)
    {
        var existing = await _db.MetroLines.FirstOrDefaultAsync(l => l.Id == dto.Id, ct);
        if (existing is null)
        {
            existing = new MetroLine { Id = dto.Id };
            _db.MetroLines.Add(existing);
        }
        existing.Name = dto.Name;
        existing.Number = dto.Number;
        existing.Description = dto.Description;
        existing.IsActive = dto.IsActive;
        existing.CitizenCanView = dto.CitizenCanView;
        existing.__v = dto.__v;
        existing.CreatedAt = dto.CreatedAt;
        existing.UpdatedAt = dto.UpdatedAt;
        existing.CreatedById = dto.CreatedBy?.Id;
    }

    private async Task UpsertStationAsync(MetroStation station, CancellationToken ct)
    {
        var existing = await _db.MetroStations.FirstOrDefaultAsync(s => s.Id == station.Id, ct);
        if (existing is null)
        {
            existing = new MetroStation { Id = station.Id };
            _db.MetroStations.Add(existing);
        }
        existing.Name = station.Name;
        existing.Address = station.Address;
        existing.Description = station.Description;
        existing.Deleted = station.Deleted;
        existing.IsActive = station.IsActive;
        existing.CitizenCanView = station.CitizenCanView;
        existing.Location = station.Location is null ? null : new GeoLocation
        {
            Latitude = station.Location.Latitude,
            Longitude = station.Location.Longitude,
            Zoom = station.Location.Zoom
        };
        existing.MetroLineId = station.MetroLineId;
        existing.OrderIndex = station.OrderIndex;
        existing.CreatedAt = station.CreatedAt;
        existing.UpdatedAt = station.UpdatedAt;
        existing.__v = station.__v;
    }
}
