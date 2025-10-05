using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MetroScheduler.Infrastructure;
using MetroScheduler.Domain.Entities;

namespace MetroScheduler.Pages;

public class IndexModel : PageModel
{
    private readonly IDbContextFactory<MetroDbContext> _dbFactory;

    public IndexModel(IDbContextFactory<MetroDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public List<MetroLine> Lines { get; private set; } = new();
    public List<MetroStation> Stations { get; private set; } = new();
    public List<MetroFullSchedule> FullSchedules { get; private set; } = new();

    public string? SelectedLineId { get; set; }
    public string? SelectedOriginId { get; set; }
    public string? SelectedDestId { get; set; }
    public bool IsHoliday { get; set; }

    public async Task OnGet(string? selectedLineId, string? selectedOriginId, string? selectedDestId, bool? isHoliday)
    {
        SelectedLineId = selectedLineId;
        SelectedOriginId = selectedOriginId;
        SelectedDestId = selectedDestId;
        IsHoliday = isHoliday ?? false;

        using var db = _dbFactory.CreateDbContext();
        Lines = await db.MetroLines.AsNoTracking().OrderBy(l => l.Number).ThenBy(l => l.Name).ToListAsync();

        if (!string.IsNullOrEmpty(SelectedLineId))
        {
            // Align with builder ordering
            Stations = await db.MetroStations.AsNoTracking()
                .Where(s => s.MetroLineId == SelectedLineId)
                .OrderBy(s => s.OrderIndex).ThenBy(s => s.Name)
                .ToListAsync();

            if (!string.IsNullOrEmpty(SelectedOriginId) && !string.IsNullOrEmpty(SelectedDestId))
            {
                var orderById = Stations.Select((s, idx) => new { s.Id, idx }).ToDictionary(x => x.Id, x => x.idx);
                if (!orderById.TryGetValue(SelectedOriginId, out var originIdx) || !orderById.TryGetValue(SelectedDestId, out var destIdx))
                {
                    FullSchedules = new();
                    return;
                }

                // Determine direction and its terminal
                var forward = destIdx >= originIdx;
                var terminalWanted = forward ? Stations.Last().Name : Stations.First().Name;

                var baseQuery = db.MetroFullSchedules.AsNoTracking()
                    .Where(f => f.LineId == SelectedLineId && f.IsHoliday == IsHoliday &&
                                f.StationIdOrigin == SelectedOriginId && f.StationIdDestination == SelectedDestId);

                var dirQuery = baseQuery.Where(f => f.EndStation == terminalWanted);

                // Select the largest ListNo within the correct direction (ignore time completeness here)
                var listNo = await dirQuery
                    .OrderByDescending(f => f.ListNo)
                    .Select(f => f.ListNo)
                    .FirstOrDefaultAsync();

                if (listNo == 0)
                {
                    FullSchedules = new();
                    return;
                }

                // Show only rows that have both times for the chosen list in the chosen direction
                FullSchedules = await dirQuery
                    .Where(f => f.ListNo == listNo &&
                                !string.IsNullOrWhiteSpace(f.TimeOrigin) && !string.IsNullOrWhiteSpace(f.TimeDestination))
                    .OrderBy(f => f.TrainNumber)
                    .ToListAsync();
            }
        }
    }
}
