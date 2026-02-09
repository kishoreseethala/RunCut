using Microsoft.AspNetCore.Http;

namespace RunCutWeb.Application.DTOs;

public class ImportRequestDto
{
    public string DataSetName { get; set; } = string.Empty;
    public IFormFile? RoutesFile { get; set; }
    public IFormFile? StopsFile { get; set; }
    public IFormFile? StopTimingsFile { get; set; }
    public IFormFile? TripsFile { get; set; }
    public IFormFile? CalendarDatesFile { get; set; }
}
