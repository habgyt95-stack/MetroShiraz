using System.Text.Json.Serialization;

namespace MetroScheduler.Domain.Entities;

public sealed class MetroLineDto
{
    [JsonPropertyName("createdBy")] public CreatedBy? CreatedBy { get; set; }
    [JsonPropertyName("_id")] public string Id { get; set; } = default!;
    public string? Name { get; set; }
    public int? Number { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public bool? CitizenCanView { get; set; }
    public int? __v { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public List<MetroStation> Stations { get; set; } = new();
}
