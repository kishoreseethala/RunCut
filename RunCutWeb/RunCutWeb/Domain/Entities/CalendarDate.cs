namespace RunCutWeb.Domain.Entities;

public class CalendarDate
{
    public int Id { get; set; }
    public int DataSetId { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int ExceptionType { get; set; }
    
    // Navigation property
    public virtual DataSet DataSet { get; set; } = null!;
}
