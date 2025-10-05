using System.Text.Json.Serialization;

namespace MetroScheduler.Domain.Entities;

public sealed class GeoLocation
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int? Zoom { get; set; }
}
