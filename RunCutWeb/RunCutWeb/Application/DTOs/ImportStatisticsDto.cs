namespace RunCutWeb.Application.DTOs;

public class ImportStatisticsDto
{
    public int RoutesImported { get; set; }
    public int StopsImported { get; set; }
    public int StopTimingsImported { get; set; }
    public int TripsImported { get; set; }
    public int CalendarDatesImported { get; set; }
}
