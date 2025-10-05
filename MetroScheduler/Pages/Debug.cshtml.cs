using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using MetroScheduler.Infrastructure;

namespace MetroScheduler.Pages;

public class DebugModel : PageModel
{
    private readonly IDbContextFactory<MetroDbContext> _dbFactory;

    public DebugModel(IDbContextFactory<MetroDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public string? LineId { get; private set; }
    public bool IsHoliday { get; private set; }
    public string? OriginId { get; private set; }
    public string? DestinationId { get; private set; }

    public List<(string EndStation, int ListNo)> DistinctEndStationListNo { get; private set; } = new();
    public List<(long TrainNumber, int ListNo, string? EndStation, string? TimeOrigin, string? TimeDestination)> Samples { get; private set; } = new();

    public async Task OnGet(string lineId, string originId, string destinationId, bool? isHoliday)
    {
        LineId = lineId; OriginId = originId; DestinationId = destinationId; IsHoliday = isHoliday ?? false;

        using var db = _dbFactory.CreateDbContext();
        var baseQuery = db.MetroFullSchedules.AsNoTracking()
            .Where(f => f.LineId == lineId && f.IsHoliday == IsHoliday &&
                        f.StationIdOrigin == originId && f.StationIdDestination == destinationId);

        DistinctEndStationListNo = await baseQuery
            .GroupBy(f => new { f.EndStation, f.ListNo })
            .OrderBy(g => g.Key.EndStation)
            .ThenBy(g => g.Key.ListNo)
            .Select(g => new ValueTuple<string, int>(g.Key.EndStation!, g.Key.ListNo))
            .ToListAsync();

        Samples = await baseQuery
            .OrderBy(f => f.ListNo)
            .ThenBy(f => f.TrainNumber)
            .Take(30)
            .Select(f => new ValueTuple<long, int, string?, string?, string?>(f.TrainNumber, f.ListNo, f.EndStation, f.TimeOrigin, f.TimeDestination))
            .ToListAsync();
    }
}
