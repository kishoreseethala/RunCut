namespace RunCutWeb.Domain.Entities;

public class Trip
{
    public int Id { get; set; }
    public int DataSetId { get; set; }
    public string RouteId { get; set; } = string.Empty;
    public string ServiceId { get; set; } = string.Empty;
    public string TripId { get; set; } = string.Empty;
    public string? TripHeadsign { get; set; }
    public string? TripShortName { get; set; }
    public int? DirectionId { get; set; }
    public string? BlockId { get; set; }
    public string? ShapeId { get; set; }
    public int? WheelchairAccessible { get; set; }
    public int? BikesAllowed { get; set; }
    
    // Navigation property
    public virtual DataSet DataSet { get; set; } = null!;
    public virtual Route? Route { get; set; }
    public virtual ICollection<StopTiming> StopTimings { get; set; } = new List<StopTiming>();
}
