namespace RunCutWeb.Domain.Entities;

public class Route
{
    public int Id { get; set; }
    public int DataSetId { get; set; }
    public string RouteId { get; set; } = string.Empty;
    public string? AgencyId { get; set; }
    public string? RouteShortName { get; set; }
    public string? RouteLongName { get; set; }
    public string? RouteDesc { get; set; }
    public int? RouteType { get; set; }
    public string? RouteUrl { get; set; }
    public string? RouteColor { get; set; }
    public string? RouteTextColor { get; set; }
    
    // Navigation property
    public virtual DataSet DataSet { get; set; } = null!;
    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
}
