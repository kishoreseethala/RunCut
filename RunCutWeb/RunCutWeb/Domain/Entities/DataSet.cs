namespace RunCutWeb.Domain.Entities;

public class DataSet
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? LastModifiedDate { get; set; }
    
    // Navigation properties
    public virtual ICollection<Route> Routes { get; set; } = new List<Route>();
    public virtual ICollection<Stop> Stops { get; set; } = new List<Stop>();
    public virtual ICollection<StopTiming> StopTimings { get; set; } = new List<StopTiming>();
    public virtual ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public virtual ICollection<CalendarDate> CalendarDates { get; set; } = new List<CalendarDate>();
}
