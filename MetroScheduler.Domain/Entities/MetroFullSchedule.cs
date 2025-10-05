namespace MetroScheduler.Domain.Entities;

public sealed class MetroFullSchedule
{
    public int Id { get; set; }
    public long TrainNumber { get; set; }
    public int ListNo { get; set; }
    public string? EndStation { get; set; }
    public string? TimeOrigin { get; set; }
    public string? TimeDestination { get; set; }
    public bool IsHoliday { get; set; }
    public string? LineId { get; set; }
    public string? LineName { get; set; }
    public string? StationIdOrigin { get; set; }
    public string? StationIdDestination { get; set; }
    public string? StationNameOrigin { get; set; }
    public string? StationNameDestination { get; set; }
    public int SchedulesNumber { get; set; }
    public long CreatedAt { get; set; }
    public long LastUpdated { get; set; }
}
