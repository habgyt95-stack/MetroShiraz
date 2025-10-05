using System.Text.Json.Serialization;

namespace MetroScheduler.Domain.Entities;

public sealed class StationSchedule
{
    [JsonPropertyName("_id")] public string StationId { get; set; } = default!;
    public string? StartStation { get; set; }
    public string? EndStation { get; set; }
    public string? Name { get; set; }
    public int? Interval { get; set; }
    public List<string> StartTimeToEndStation { get; set; } = new();

    // Not in API payload; set by caller based on query param
    public bool IsHoliday { get; set; }

    // Synthetic, stable identifier for a specific schedule list variant of a station
    // Computed from Start/End/Interval/Times to differentiate multiple lists
    public string ListKey { get; set; } = default!;

    // Sequential number per (StationId, IsHoliday) starting from 1
    public int ListNo { get; set; }
}
