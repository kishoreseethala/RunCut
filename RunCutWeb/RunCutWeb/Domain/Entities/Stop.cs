namespace RunCutWeb.Domain.Entities;

public class Stop
{
    public int Id { get; set; }
    public int DataSetId { get; set; }
    public string StopId { get; set; } = string.Empty;
    public string? StopCode { get; set; }
    public string? StopName { get; set; }
    public string? StopDesc { get; set; }
    public decimal? StopLat { get; set; }
    public decimal? StopLon { get; set; }
    public string? ZoneId { get; set; }
    public string? StopUrl { get; set; }
    public int? LocationType { get; set; }
    public string? ParentStation { get; set; }
    public string? StopTimeZone { get; set; }
    public int? WheelchairBoarding { get; set; }
    
    // Navigation property
    public virtual DataSet DataSet { get; set; } = null!;
    public virtual ICollection<StopTiming> StopTimings { get; set; } = new List<StopTiming>();
}
