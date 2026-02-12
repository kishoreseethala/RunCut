using Microsoft.AspNetCore.Mvc;
using RunCutWeb.Application.DTOs;
using RunCutWeb.Application.Interfaces;

namespace RunCutWeb.Controllers;

public class DataImporterController : Controller
{
    private readonly IDataImportService _dataImportService;
    private readonly ILogger<DataImporterController> _logger;

    public DataImporterController(IDataImportService dataImportService, ILogger<DataImporterController> logger)
    {
        _dataImportService = dataImportService;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(ImportRequestDto request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Json(new { success = false, message = "Invalid model state", errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });
        }

        if (string.IsNullOrWhiteSpace(request.DataSetName))
        {
            return Json(new { success = false, message = "Dataset name is required" });
        }

        try
        {
            _logger.LogInformation("Import: DataSetName={Name}, RoutesFile={R}, StopsFile={S}, TripsFile={T}, StopTimingsFile={ST}, CalendarDatesFile={C}",
                request.DataSetName,
                request.RoutesFile != null ? request.RoutesFile.Length + " bytes" : "null",
                request.StopsFile != null ? request.StopsFile.Length + " bytes" : "null",
                request.TripsFile != null ? request.TripsFile.Length + " bytes" : "null",
                request.StopTimingsFile != null ? request.StopTimingsFile.Length + " bytes" : "null",
                request.CalendarDatesFile != null ? request.CalendarDatesFile.Length + " bytes" : "null");
            var result = await _dataImportService.ImportDataSetAsync(request, cancellationToken);

            if (result.IsSuccess)
            {
                return Json(new
                {
                    success = true,
                    message = result.Message,
                    dataSetId = result.DataSetId,
                    statistics = result.Statistics
                });
            }
            else
            {
                return Json(new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during data import");
            var message = ex.Message;
            if (ex.InnerException != null)
                message += " " + ex.InnerException.Message;
            return Json(new { success = false, message = $"Error importing data: {message}", errors = new[] { message } });
        }
    }
}
