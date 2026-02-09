using System.ComponentModel.DataAnnotations.Schema;

namespace RunCutWeb.Domain.Entities;

/// <summary>Date dimension table for looking up day of week and other date attributes.</summary>
[Table("d_Date")]
public class DDate
{
    /// <summary>Primary key: date as integer YYYYMMDD (e.g. 20250206).</summary>
    public int DateKey { get; set; }

    /// <summary>The calendar date.</summary>
    public DateTime Date { get; set; }

    /// <summary>Day of week: 0 = Sunday, 1 = Monday, ... 6 = Saturday (System.DayOfWeek).</summary>
    public int DayOfWeek { get; set; }

    /// <summary>Full day name (e.g. "Monday").</summary>
    public string DayName { get; set; } = string.Empty;
}
