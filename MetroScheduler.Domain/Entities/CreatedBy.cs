using System.Text.Json.Serialization;

namespace MetroScheduler.Domain.Entities;

public sealed class CreatedBy
{
    [JsonPropertyName("_id")] public string Id { get; set; } = default!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}
