using System.Text.Json.Serialization;

namespace MetroScheduler.Domain.Entities;

public sealed class LinesOfStationResponse
{
    [JsonPropertyName("_id")] public string LineId { get; set; } = default!;
    public string? Name { get; set; }
    public int? Number { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public CreatedBy? CreatedBy { get; set; }
    public List<MetroStation> Stations { get; set; } = new();
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public int? __v { get; set; }
    public bool? CitizenCanView { get; set; }

    public List<CurrentTimeScheduleItem> TimeSchedule { get; set; } = new();
    public MetroStation? CurrentStation { get; set; }
}

public sealed class CurrentTimeScheduleItem
{
    [JsonPropertyName("_id")] public string StationId { get; set; } = default!;
    public string? StartStation { get; set; }
    public string? EndStation { get; set; }
    public string? Name { get; set; }
    public int? Interval { get; set; }
    public string? ClosestMovement { get; set; }
    public string? DifferenceToMovement { get; set; }
}
