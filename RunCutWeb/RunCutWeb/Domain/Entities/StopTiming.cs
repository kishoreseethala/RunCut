namespace RunCutWeb.Domain.Entities;

public class StopTiming
{
    public int Id { get; set; }
    public int DataSetId { get; set; }
    public string TripId { get; set; } = string.Empty;
    public string? ArrivalTime { get; set; }
    public string? DepartureTime { get; set; }
    public string StopId { get; set; } = string.Empty;
    public int? StopSequence { get; set; }
    public string? StopHeadsign { get; set; }
    public int? PickupType { get; set; }
    public int? DropOffType { get; set; }
    public decimal? ShapeDistTraveled { get; set; }
    public int? Timepoint { get; set; }
    
    // Navigation property
    public virtual DataSet DataSet { get; set; } = null!;
    public virtual Stop? Stop { get; set; }
    public virtual Trip? Trip { get; set; }
}
