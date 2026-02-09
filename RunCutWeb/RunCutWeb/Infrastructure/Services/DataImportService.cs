using System.Globalization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using RunCutWeb.Application.DTOs;
using RunCutWeb.Application.Interfaces;
using RunCutWeb.Domain.Entities;
using RunCutWeb.Infrastructure.Data;
using RouteEntity = RunCutWeb.Domain.Entities.Route;

namespace RunCutWeb.Infrastructure.Services;

public class DataImportService : IDataImportService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataImportService> _logger;

    public DataImportService(ApplicationDbContext context, ILogger<DataImportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ImportResultDto> ImportDataSetAsync(ImportRequestDto request, CancellationToken cancellationToken = default)
    {
        var result = new ImportResultDto();
        var statistics = new ImportStatisticsDto();

        try
        {
            // Validate dataset name is not duplicate (case-insensitive)
            var trimmedName = request.DataSetName.Trim();
            var existingDataSet = await _context.DataSets
                .FirstOrDefaultAsync(d => d.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

            if (existingDataSet != null)
            {
                result.IsSuccess = false;
                result.Message = $"A dataset with the name '{trimmedName}' already exists. Please choose a different name.";
                result.Errors.Add($"Dataset name '{trimmedName}' is already in use.");
                return result;
            }

            // Create DataSet
            var dataSet = new DataSet
            {
                Name = trimmedName,
                CreatedDate = DateTime.UtcNow
            };

            _context.DataSets.Add(dataSet);
            await _context.SaveChangesAsync(cancellationToken);

            result.DataSetId = dataSet.Id;

            // Import Routes
            if (request.RoutesFile != null && request.RoutesFile.Length > 0)
            {
                var routes = await ParseRoutesCsvAsync(request.RoutesFile, dataSet.Id, cancellationToken);
                if (routes.Any())
                {
                    await _context.Routes.AddRangeAsync(routes, cancellationToken);
                    statistics.RoutesImported = routes.Count;
                }
            }

            // Import Stops
            if (request.StopsFile != null && request.StopsFile.Length > 0)
            {
                var stops = await ParseStopsCsvAsync(request.StopsFile, dataSet.Id, cancellationToken);
                if (stops.Any())
                {
                    await _context.Stops.AddRangeAsync(stops, cancellationToken);
                    statistics.StopsImported = stops.Count;
                }
            }

            // Import Trips
            if (request.TripsFile != null && request.TripsFile.Length > 0)
            {
                var trips = await ParseTripsCsvAsync(request.TripsFile, dataSet.Id, cancellationToken);
                if (trips.Any())
                {
                    await _context.Trips.AddRangeAsync(trips, cancellationToken);
                    statistics.TripsImported = trips.Count;
                }
            }

            // Import StopTimings in batches (should be imported after Trips and Stops)
            if (request.StopTimingsFile != null && request.StopTimingsFile.Length > 0)
            {
                var importedCount = await ImportStopTimingsInBatchesAsync(request.StopTimingsFile, dataSet.Id, cancellationToken);
                statistics.StopTimingsImported = importedCount;
            }

            // Import CalendarDates
            if (request.CalendarDatesFile != null && request.CalendarDatesFile.Length > 0)
            {
                try
                {
                    _logger.LogInformation($"Starting Calendar Dates import. File size: {request.CalendarDatesFile.Length} bytes, File name: {request.CalendarDatesFile.FileName}");
                    var calendarDates = await ParseCalendarDatesCsvAsync(request.CalendarDatesFile, dataSet.Id, cancellationToken);
                    _logger.LogInformation($"Parsed {calendarDates.Count} calendar dates from CSV");
                    
                    if (calendarDates.Any())
                    {
                        _context.ChangeTracker.AutoDetectChangesEnabled = false;
                        await _context.CalendarDates.AddRangeAsync(calendarDates, cancellationToken);
                        var savedCount = await _context.SaveChangesAsync(cancellationToken);
                        _context.ChangeTracker.AutoDetectChangesEnabled = true;
                        _context.ChangeTracker.Clear();
                        statistics.CalendarDatesImported = calendarDates.Count;
                        _logger.LogInformation($"Successfully imported {calendarDates.Count} calendar dates. SaveChanges returned: {savedCount}");
                    }
                    else
                    {
                        _logger.LogWarning("No calendar dates were parsed from the CSV file. Check the file format and column names.");
                        result.Errors.Add("No calendar dates were parsed from the CSV file. Please check the file format.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error importing calendar dates");
                    result.Errors.Add($"Error importing calendar dates: {ex.Message}");
                }
            }
            else
            {
                _logger.LogInformation("No Calendar Dates file provided for import");
            }

            await _context.SaveChangesAsync(cancellationToken);

            result.IsSuccess = true;
            result.Message = "Data imported successfully";
            result.Statistics = statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing data set");
            result.IsSuccess = false;
            result.Message = $"Error importing data: {ex.Message}";
            result.Errors.Add(ex.Message);
        }

        return result;
    }

    private async Task<List<RouteEntity>> ParseRoutesCsvAsync(IFormFile file, int dataSetId, CancellationToken cancellationToken)
    {
        var routes = new List<RouteEntity>();
        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        await foreach (var record in csv.GetRecordsAsync<dynamic>(cancellationToken))
        {
            var route = new RouteEntity
            {
                DataSetId = dataSetId,
                RouteId = GetPropertyValue(record, "route_id") ?? string.Empty,
                AgencyId = GetPropertyValue(record, "agency_id"),
                RouteShortName = GetPropertyValue(record, "route_short_name"),
                RouteLongName = GetPropertyValue(record, "route_long_name"),
                RouteDesc = GetPropertyValue(record, "route_desc"),
                RouteType = ParseInt(GetPropertyValue(record, "route_type")),
                RouteUrl = GetPropertyValue(record, "route_url"),
                RouteColor = GetPropertyValue(record, "route_color"),
                RouteTextColor = GetPropertyValue(record, "route_text_color")
            };
            routes.Add(route);
        }

        return routes;
    }

    private async Task<List<Stop>> ParseStopsCsvAsync(IFormFile file, int dataSetId, CancellationToken cancellationToken)
    {
        var stops = new List<Stop>();
        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        await foreach (var record in csv.GetRecordsAsync<dynamic>(cancellationToken))
        {
            var stop = new Stop
            {
                DataSetId = dataSetId,
                StopId = GetPropertyValue(record, "stop_id") ?? string.Empty,
                StopCode = GetPropertyValue(record, "stop_code"),
                StopName = GetPropertyValue(record, "stop_name"), // Also handles "stop_nam"
                StopDesc = GetPropertyValue(record, "stop_desc"),
                StopLat = ParseDecimal(GetPropertyValue(record, "stop_lat")),
                StopLon = ParseDecimal(GetPropertyValue(record, "stop_lon")),
                ZoneId = GetPropertyValue(record, "zone_id"),
                StopUrl = GetPropertyValue(record, "stop_url"),
                LocationType = ParseInt(GetPropertyValue(record, "location_type")), // Also handles "location_t"
                ParentStation = GetPropertyValue(record, "parent_station"),
                StopTimeZone = GetPropertyValue(record, "stop_timezone"),
                WheelchairBoarding = ParseInt(GetPropertyValue(record, "wheelchair_boarding"))
            };
            stops.Add(stop);
        }

        return stops;
    }

    private async Task<List<Trip>> ParseTripsCsvAsync(IFormFile file, int dataSetId, CancellationToken cancellationToken)
    {
        var trips = new List<Trip>();
        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        await foreach (var record in csv.GetRecordsAsync<dynamic>(cancellationToken))
        {
            var trip = new Trip
            {
                DataSetId = dataSetId,
                RouteId = GetPropertyValue(record, "route_id") ?? string.Empty,
                ServiceId = GetPropertyValue(record, "service_id") ?? string.Empty,
                TripId = GetPropertyValue(record, "trip_id") ?? string.Empty,
                TripHeadsign = GetPropertyValue(record, "trip_headsign"),
                TripShortName = GetPropertyValue(record, "trip_short_name"),
                DirectionId = ParseInt(GetPropertyValue(record, "direction_id")),
                BlockId = GetPropertyValue(record, "block_id"),
                ShapeId = GetPropertyValue(record, "shape_id"),
                WheelchairAccessible = ParseInt(GetPropertyValue(record, "wheelchair_accessible")),
                BikesAllowed = ParseInt(GetPropertyValue(record, "bikes_allowed"))
            };
            trips.Add(trip);
        }

        return trips;
    }


    private async Task<int> ImportStopTimingsInBatchesAsync(IFormFile file, int dataSetId, CancellationToken cancellationToken)
    {
        const int batchSize = 1000; // Process 1000 records at a time
        var totalImported = 0;
        var batch = new List<StopTiming>(batchSize);

        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HeaderValidated = null,
            MissingFieldFound = null
        });

        await foreach (var record in csv.GetRecordsAsync<dynamic>(cancellationToken))
        {
            try
            {
                var stopTiming = new StopTiming
                {
                    DataSetId = dataSetId,
                    TripId = GetPropertyValue(record, "trip_id") ?? string.Empty,
                    ArrivalTime = GetPropertyValue(record, "arrival_time"), // Also handles "arrival_tin"
                    DepartureTime = GetPropertyValue(record, "departure_time"), // Also handles "departure"
                    StopId = GetPropertyValue(record, "stop_id") ?? string.Empty,
                    StopSequence = ParseInt(GetPropertyValue(record, "stop_sequence")), // Also handles "stop_sequ"
                    StopHeadsign = GetPropertyValue(record, "stop_headsign"), // Also handles "stop_heac"
                    PickupType = ParseInt(GetPropertyValue(record, "pickup_type")), // Also handles "pickup_typ"
                    DropOffType = ParseInt(GetPropertyValue(record, "drop_off_type")), // Also handles "drop_off_t"
                    ShapeDistTraveled = ParseDecimal(GetPropertyValue(record, "shape_dist_traveled")), // Also handles "shape_dis"
                    Timepoint = ParseInt(GetPropertyValue(record, "timepoint"))
                };

                batch.Add(stopTiming);

                // When batch is full, save to database
                if (batch.Count >= batchSize)
                {
                    _context.ChangeTracker.AutoDetectChangesEnabled = false;
                    await _context.StopTimings.AddRangeAsync(batch, cancellationToken);
                    await _context.SaveChangesAsync(cancellationToken);
                    _context.ChangeTracker.AutoDetectChangesEnabled = true;
                    _context.ChangeTracker.Clear(); // Clear change tracker to free memory
                    totalImported += batch.Count;
                    _logger.LogInformation($"Imported batch of {batch.Count} stop timings. Total: {totalImported}");
                    batch.Clear();
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error parsing stop timing record, skipping");
                continue;
            }
        }

        // Save remaining records in the batch
        if (batch.Any())
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            await _context.StopTimings.AddRangeAsync(batch, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
            _context.ChangeTracker.Clear();
            totalImported += batch.Count;
            _logger.LogInformation($"Imported final batch of {batch.Count} stop timings. Total: {totalImported}");
        }

        return totalImported;
    }

    private async Task<List<CalendarDate>> ParseCalendarDatesCsvAsync(IFormFile file, int dataSetId, CancellationToken cancellationToken)
    {
        var calendarDates = new List<CalendarDate>();
        var recordCount = 0;
        var skippedCount = 0;
        
        try
        {
            // Reset stream position to beginning
            using var stream = file.OpenReadStream();
            stream.Position = 0;
            
            using var reader = new StreamReader(stream, System.Text.Encoding.UTF8, true, 1024, true);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null,
                TrimOptions = TrimOptions.Trim,
                BadDataFound = null
            });

            // Read header
            await csv.ReadAsync();
            csv.ReadHeader();
            var headers = csv.HeaderRecord;
            
            if (headers == null || headers.Length == 0)
            {
                _logger.LogWarning("Calendar Dates CSV file has no headers");
                return calendarDates;
            }
            
            _logger.LogInformation($"Calendar Dates CSV headers found ({headers.Length}): {string.Join(", ", headers)}");
            
            // Process records row by row
            while (await csv.ReadAsync())
            {
                recordCount++;
                try
                {
                    // Get values directly from CSV reader - try multiple column name variations
                    var serviceId = csv.GetField<string>("service_id")?.Trim() ?? 
                                   csv.GetField<string>("serviceid")?.Trim() ??
                                   csv.GetField<string>("ServiceId")?.Trim() ??
                                   string.Empty;
                    
                    var dateValue = csv.GetField<string>("date")?.Trim() ??
                                   csv.GetField<string>("Date")?.Trim() ??
                                   string.Empty;
                    
                    var exceptionTypeValue = csv.GetField<string>("exception_type")?.Trim() ??
                                           csv.GetField<string>("exceptiontype")?.Trim() ??
                                           csv.GetField<string>("ExceptionType")?.Trim() ??
                                           csv.GetField<string>("exception_typ")?.Trim() ??
                                           "1";

                    if (string.IsNullOrWhiteSpace(serviceId))
                    {
                        skippedCount++;
                        if (recordCount <= 5)
                        {
                            _logger.LogWarning($"Record {recordCount}: Skipping calendar date - service_id is empty");
                        }
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(dateValue))
                    {
                        skippedCount++;
                        if (recordCount <= 5)
                        {
                            _logger.LogWarning($"Record {recordCount}: Skipping calendar date - date is empty for service_id: {serviceId}");
                        }
                        continue;
                    }

                    // Try to parse date - handle YYYYMMDD format
                    var date = ParseDate(dateValue);
                    if (!date.HasValue)
                    {
                        skippedCount++;
                        if (recordCount <= 5)
                        {
                            _logger.LogWarning($"Record {recordCount}: Skipping calendar date - could not parse date '{dateValue}' for service_id: {serviceId}");
                        }
                        continue;
                    }

                    var exceptionType = ParseInt(exceptionTypeValue) ?? 1;

                    var calendarDate = new CalendarDate
                    {
                        DataSetId = dataSetId,
                        ServiceId = serviceId,
                        Date = date.Value,
                        ExceptionType = exceptionType
                    };
                    calendarDates.Add(calendarDate);
                    
                    // Log first few successful records for debugging
                    if (calendarDates.Count <= 5)
                    {
                        _logger.LogInformation($"Parsed calendar date {calendarDates.Count}: ServiceId={serviceId}, Date={date.Value:yyyyMMdd}, ExceptionType={exceptionType}");
                    }
                }
                catch (Exception ex)
                {
                    skippedCount++;
                    var serviceIdDebug = "";
                    var dateDebug = "";
                    try
                    {
                        serviceIdDebug = csv.GetField<string>("service_id") ?? "N/A";
                        dateDebug = csv.GetField<string>("date") ?? "N/A";
                    }
                    catch { }
                    _logger.LogWarning(ex, $"Record {recordCount}: Error parsing calendar date record. ServiceId: {serviceIdDebug}, Date: {dateDebug}");
                    continue;
                }
            }

            _logger.LogInformation($"Calendar Dates parsing complete: {recordCount} records processed, {calendarDates.Count} valid, {skippedCount} skipped");
            
            if (calendarDates.Count == 0 && recordCount > 0)
            {
                _logger.LogError($"No calendar dates were successfully parsed from {recordCount} records. Check CSV format and column names.");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error reading Calendar Dates CSV file");
        }
        
        return calendarDates;
    }

    private static List<string> GetAvailableFields(dynamic record)
    {
        try
        {
            var dict = record as IDictionary<string, object>;
            if (dict != null)
            {
                return dict.Keys.ToList();
            }
        }
        catch
        {
            // Ignore errors
        }
        return new List<string>();
    }

    private static string? GetPropertyValue(dynamic record, string propertyName)
    {
        try
        {
            if (record == null)
                return null;

            // CsvHelper returns records as IDictionary<string, object>
            var dict = record as IDictionary<string, object>;
            if (dict == null)
            {
                // Try to access as dynamic property
                try
                {
                    var value = ((object)record).GetType().GetProperty(propertyName)?.GetValue(record);
                    return value?.ToString();
                }
                catch
                {
                    return null;
                }
            }

            // First try exact match (case-insensitive)
            var key = dict.Keys.FirstOrDefault(k => 
                string.Equals(k, propertyName, StringComparison.OrdinalIgnoreCase));
            if (key != null && dict.TryGetValue(key, out var propValue) && propValue != null)
            {
                return propValue.ToString();
            }
            
            // Try abbreviated versions for common GTFS fields
            var abbreviatedMappings = new Dictionary<string, string[]>
            {
                // Routes
                { "route_short_name", new[] { "route_sho", "route_short" } },
                { "route_long_name", new[] { "route_long", "route_longname" } },
                { "route_desc", new[] { "route_des", "route_description" } },
                { "route_color", new[] { "route_col", "route_colour" } },
                { "route_text_color", new[] { "route_text_col", "route_text_colour" } },
                // Stops
                { "stop_name", new[] { "stop_nam" } },
                { "location_type", new[] { "location_t" } },
                // Stop Times
                { "arrival_time", new[] { "arrival_tin" } },
                { "departure_time", new[] { "departure" } },
                { "stop_sequence", new[] { "stop_sequ" } },
                { "stop_headsign", new[] { "stop_heac" } },
                { "pickup_type", new[] { "pickup_typ" } },
                { "drop_off_type", new[] { "drop_off_t" } },
                { "shape_dist_traveled", new[] { "shape_dis" } }
            };
            
            if (abbreviatedMappings.TryGetValue(propertyName, out var abbreviations))
            {
                foreach (var abbrev in abbreviations)
                {
                    key = dict.Keys.FirstOrDefault(k => 
                        string.Equals(k, abbrev, StringComparison.OrdinalIgnoreCase));
                    if (key != null && dict.TryGetValue(key, out var abbrevValue) && abbrevValue != null)
                    {
                        return abbrevValue.ToString();
                    }
                }
            }
            
            return null;
        }
        catch
        {
            // Return null on any error
            return null;
        }
    }

    private static int? ParseInt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return int.TryParse(value, out var result) ? result : null;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        return decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) ? result : null;
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        
        // GTFS date format is YYYYMMDD
        if (value.Length == 8 && DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result))
            return result;
        
        return DateTime.TryParse(value, out var parsed) ? parsed : null;
    }
}
