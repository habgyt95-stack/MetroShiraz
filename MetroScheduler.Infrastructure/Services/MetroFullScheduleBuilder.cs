using Microsoft.EntityFrameworkCore;
using MetroScheduler.Domain.Entities;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MetroScheduler.Infrastructure.Services;

public interface IMetroFullScheduleBuilder
{
    Task BuildAsync(string lineId, bool isHoliday, CancellationToken ct = default);
    Task BuildAllAsync(CancellationToken ct = default);
}

public sealed class MetroFullScheduleBuilder : IMetroFullScheduleBuilder
{
    private readonly MetroDbContext _db;

    public MetroFullScheduleBuilder(MetroDbContext db)
    {
        _db = db;
    }

    public async Task BuildAllAsync(CancellationToken ct = default)
    {
        var lineIds = await _db.MetroLines.AsNoTracking().Select(l => l.Id).ToListAsync(ct);
        int totalSteps = lineIds.Count * 2;
        int currentStep = 0;
        var sw = Stopwatch.StartNew();
        foreach (var lineId in lineIds)
        {
            currentStep++;
            Console.WriteLine($"[MetroFullSchedule] {currentStep * 100 / Math.Max(1,totalSteps),3}% - Line {lineId} (IsHoliday=false)");
            await BuildAsync(lineId, false, ct);
            var elapsed = sw.Elapsed;
            var eta = TimeSpan.FromTicks(elapsed.Ticks * Math.Max(0, totalSteps - currentStep) / Math.Max(1, currentStep));
            Console.WriteLine($"[MetroFullSchedule] Done {currentStep}/{totalSteps} | Elapsed: {elapsed:mm\\:ss} | ETA: {eta:mm\\:ss}");

            currentStep++;
            Console.WriteLine($"[MetroFullSchedule] {currentStep * 100 / Math.Max(1,totalSteps),3}% - Line {lineId} (IsHoliday=true)");
            await BuildAsync(lineId, true, ct);
            elapsed = sw.Elapsed;
            eta = TimeSpan.FromTicks(elapsed.Ticks * Math.Max(0, totalSteps - currentStep) / Math.Max(1, currentStep));
            Console.WriteLine($"[MetroFullSchedule] Done {currentStep}/{totalSteps} | Elapsed: {elapsed:mm\\:ss} | ETA: {eta:mm\\:ss}");
        }
        sw.Stop();
        Console.WriteLine($"[MetroFullSchedule] All done in {sw.Elapsed:mm\\:ss}");
    }

    private static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var t = s.Trim();
        t = t.Replace('\u200c', ' '); // zero-width non-joiner
        t = t.Replace("?", " "); // possible ZWNJ variant
        t = t.Replace('?', '?').Replace('?', '?'); // Arabic to Persian chars
        t = Regex.Replace(t, "\\s+", " "); // collapse spaces
        return t.ToLowerInvariant();
    }

    public async Task BuildAsync(string lineId, bool isHoliday, CancellationToken ct = default)
    {
        var stations = await _db.MetroStations.AsNoTracking()
            .Where(s => s.MetroLineId == lineId)
            .OrderBy(s => s.OrderIndex).ThenBy(s => s.Name)
            .ToListAsync(ct);
        if (stations.Count == 0) return;

        var line = await _db.MetroLines.AsNoTracking().FirstOrDefaultAsync(l => l.Id == lineId, ct);

        var schedulesByStation = new Dictionary<string, List<StationSchedule>>();
        foreach (var st in stations)
        {
            var sch = await _db.StationSchedules.AsNoTracking()
                .Where(x => x.StationId == st.Id && x.IsHoliday == isHoliday)
                .OrderBy(x => x.ListNo)
                .ToListAsync(ct);
            schedulesByStation[st.Id] = sch;
        }

        // Direction terminals
        var terminalFirst = stations.First().Name;
        var terminalLast = stations.Last().Name;
        var nFirst = Normalize(terminalFirst);
        var nLast = Normalize(terminalLast);

        // Build a normalized name -> index map for the line
        var nameToIndex = stations
            .Select((s, idx) => new { Name = Normalize(s.Name), idx })
            .GroupBy(x => x.Name)
            .ToDictionary(g => g.Key, g => g.First().idx);

        int GetIndexByName(string? name)
        {
            var key = Normalize(name);
            return nameToIndex.TryGetValue(key, out var idx) ? idx : -1;
        }

        // Classify schedules into forward/backward per station based on EndStation index relative to station index
        var fwdByStation = new Dictionary<string, List<StationSchedule>>();
        var bwdByStation = new Dictionary<string, List<StationSchedule>>();

        for (int stationIdx = 0; stationIdx < stations.Count; stationIdx++)
        {
            var st = stations[stationIdx];
            var all = schedulesByStation[st.Id];
            var fwd = new List<StationSchedule>();
            var bwd = new List<StationSchedule>();

            foreach (var sch in all)
            {
                var endIdx = GetIndexByName(sch.EndStation);
                if (endIdx >= 0)
                {
                    if (endIdx > stationIdx)
                    {
                        fwd.Add(sch);
                        continue;
                    }
                    if (endIdx < stationIdx)
                    {
                        bwd.Add(sch);
                        continue;
                    }
                    // endIdx == stationIdx is ambiguous; fall through to fallback rules
                }

                // Fallback to terminal name equality if EndStation not mapped
                var nEnd = Normalize(sch.EndStation);
                if (nEnd == nLast)
                {
                    fwd.Add(sch);
                }
                else if (nEnd == nFirst)
                {
                    bwd.Add(sch);
                }
                else
                {
                    // Could not classify; default to forward to avoid data loss (rare)
                    fwd.Add(sch);
                }
            }

            // Ensure stable ordering by ListNo within each direction
            fwdByStation[st.Id] = fwd.OrderBy(x => x.ListNo).ToList();
            bwdByStation[st.Id] = bwd.OrderBy(x => x.ListNo).ToList();
        }

        // Build list groups aligned by ListKey across all stations for each direction
        // Forward groups: key = ListKey, value = list of (stationIdx, schedule) for that ListKey
        var fwdGroups = new Dictionary<string, List<(int stationIdx, StationSchedule sch)>>();
        for (int stationIdx = 0; stationIdx < stations.Count; stationIdx++)
        {
            var st = stations[stationIdx];
            var fwdLists = fwdByStation[st.Id];
            foreach (var sch in fwdLists)
            {
                if (!fwdGroups.ContainsKey(sch.ListKey))
                    fwdGroups[sch.ListKey] = new();
                fwdGroups[sch.ListKey].Add((stationIdx, sch));
            }
        }
        // Keep only groups where all stations have that ListKey
        var fwdAlignedGroups = fwdGroups
            .Where(g => g.Value.Count == stations.Count)
            .Select(g => g.Value.OrderBy(x => x.stationIdx).Select(x => x.sch).ToList())
            .OrderBy(group => group.First().ListNo)
            .ToList();

        // Backward groups
        var bwdGroups = new Dictionary<string, List<(int stationIdx, StationSchedule sch)>>();
        for (int stationIdx = 0; stationIdx < stations.Count; stationIdx++)
        {
            var st = stations[stationIdx];
            var bwdLists = bwdByStation[st.Id];
            foreach (var sch in bwdLists)
            {
                if (!bwdGroups.ContainsKey(sch.ListKey))
                    bwdGroups[sch.ListKey] = new();
                bwdGroups[sch.ListKey].Add((stationIdx, sch));
            }
        }
        var bwdAlignedGroups = bwdGroups
            .Where(g => g.Value.Count == stations.Count)
            .Select(g => g.Value.OrderBy(x => x.stationIdx).Select(x => x.sch).ToList())
            .OrderBy(group => group.First().ListNo)
            .ToList();

        int fwdCount = fwdAlignedGroups.Count;
        int bwdCount = bwdAlignedGroups.Count;

        // Diagnostic logging
        Console.WriteLine($"[MetroFullSchedule] Line {line?.Name} IsHoliday={isHoliday}: Forward groups={fwdCount}, Backward groups={bwdCount}");
        if (fwdCount == 0 && bwdCount == 0) return;

        // Wipe existing rows for this line/day
        _db.MetroFullSchedules.RemoveRange(_db.MetroFullSchedules.Where(f => f.LineId == lineId && f.IsHoliday == isHoliday));

        // Helper for progress
        void PrintProgress(string dirLabel, int k, int kTotal, int row, int rowTotal, Stopwatch swLocal)
        {
            var percent = (int)Math.Round(((k - 1) * rowTotal + row) * 100.0 / Math.Max(1, kTotal * rowTotal));
            var elapsed = swLocal.Elapsed;
            var doneSteps = (k - 1) * rowTotal + row;
            var totalSteps = Math.Max(1, kTotal * rowTotal);
            var eta = doneSteps > 0 ? TimeSpan.FromTicks(elapsed.Ticks * (totalSteps - doneSteps) / doneSteps) : TimeSpan.Zero;
            if (row % 10 == 0 || row == rowTotal)
            {
                Console.WriteLine($"[MetroFullSchedule:{dirLabel}] {percent,3}% | Line {line?.Name} | ListIndex {k}/{kTotal} | Row {row}/{rowTotal} | Elapsed: {elapsed:mm\\:ss} | ETA: {eta:mm\\:ss}");
            }
        }

        var sw = Stopwatch.StartNew();

        // Build Forward direction using forward lists only
        for (int k = 0; k < fwdCount; k++)
        {
            // Use k-th aligned group (all stations have the same ListKey)
            var schedulePerStation = fwdAlignedGroups[k];
            // rows count (assumed equal); use min for safety
            var rowsPerStation = schedulePerStation.Select(s => s.StartTimeToEndStation.Count).ToList();
            var rowCount = rowsPerStation.Min();

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var trainNumber = (long)(rowIndex + 1);
                for (int originIdx = 0; originIdx < stations.Count; originIdx++)
                {
                    var origin = stations[originIdx];
                    var originSch = schedulePerStation[originIdx];
                    var originTime = originSch.StartTimeToEndStation[rowIndex];
                    for (int destIdx = originIdx; destIdx < stations.Count; destIdx++)
                    {
                        var dest = stations[destIdx];
                        var destTime = schedulePerStation[destIdx].StartTimeToEndStation[rowIndex];
                        _db.MetroFullSchedules.Add(new MetroFullSchedule
                        {
                            TrainNumber = trainNumber,
                            ListNo = originSch.ListNo, // use origin's actual list number
                            EndStation = terminalLast, // forward direction goes to last terminal
                            TimeOrigin = originTime,
                            TimeDestination = destTime,
                            IsHoliday = isHoliday,
                            LineId = line?.Id,
                            LineName = line?.Name,
                            StationIdOrigin = origin.Id,
                            StationIdDestination = dest.Id,
                            StationNameOrigin = origin.Name,
                            StationNameDestination = dest.Name,
                            SchedulesNumber = rowCount,
                            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        });
                    }
                }
                PrintProgress("FWD", k + 1, fwdCount, rowIndex + 1, rowCount, sw);
            }
        }

        // Build Backward direction using backward lists only
        for (int k = 0; k < bwdCount; k++)
        {
            // Use k-th aligned group (all stations have the same ListKey)
            var schedulePerStation = bwdAlignedGroups[k];
            var rowsPerStation = schedulePerStation.Select(s => s.StartTimeToEndStation.Count).ToList();
            var rowCount = rowsPerStation.Min();

            for (int rowIndex = 0; rowIndex < rowCount; rowIndex++)
            {
                var trainNumber = (long)(rowIndex + 1);
                for (int originIdx = stations.Count - 1; originIdx >= 0; originIdx--)
                {
                    var origin = stations[originIdx];
                    var originSch = schedulePerStation[originIdx];
                    var originTime = originSch.StartTimeToEndStation[rowIndex];
                    for (int destIdx = originIdx; destIdx >= 0; destIdx--)
                    {
                        var dest = stations[destIdx];
                        var destTime = schedulePerStation[destIdx].StartTimeToEndStation[rowIndex];
                        _db.MetroFullSchedules.Add(new MetroFullSchedule
                        {
                            TrainNumber = trainNumber,
                            ListNo = originSch.ListNo,
                            EndStation = terminalFirst, // backward direction goes to first terminal
                            TimeOrigin = originTime,
                            TimeDestination = destTime,
                            IsHoliday = isHoliday,
                            LineId = line?.Id,
                            LineName = line?.Name,
                            StationIdOrigin = origin.Id,
                            StationIdDestination = dest.Id,
                            StationNameOrigin = origin.Name,
                            StationNameDestination = dest.Name,
                            SchedulesNumber = rowCount,
                            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                            LastUpdated = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                        });
                    }
                }
                PrintProgress("BWD", k + 1, bwdCount, rowIndex + 1, rowCount, sw);
            }
        }

        sw.Stop();
        await _db.SaveChangesAsync(ct);
    }
}
