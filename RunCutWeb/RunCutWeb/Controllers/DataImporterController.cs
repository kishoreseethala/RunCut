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
