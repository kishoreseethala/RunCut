using RunCutWeb.Application.DTOs;

namespace RunCutWeb.Application.Interfaces;

public interface IDataImportService
{
    Task<ImportResultDto> ImportDataSetAsync(ImportRequestDto request, CancellationToken cancellationToken = default);
}
