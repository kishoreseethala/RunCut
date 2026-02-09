namespace RunCutWeb.Application.DTOs;

public class ImportResultDto
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int DataSetId { get; set; }
    public ImportStatisticsDto? Statistics { get; set; }
    public List<string> Errors { get; set; } = new();
}
