using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RunCutWeb.Application.DTOs;
using RunCutWeb.Infrastructure.Data;
using RouteEntity = RunCutWeb.Domain.Entities.Route;
using ClosedXML.Excel;

namespace RunCutWeb.Controllers;

public class DataViewerController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<DataViewerController> _logger;

    public DataViewerController(ApplicationDbContext context, ILogger<DataViewerController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetDataSets()
    {
        try
        {
            var dataSets = await _context.DataSets
                .OrderByDescending(d => d.CreatedDate)
                .Select(d => new { d.Id, d.Name, d.CreatedDate })
                .ToListAsync();
            
            return Json(new { success = true, data = dataSets });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting datasets");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetRoutes(int dataSetId)
    {
        try
        {
            var routes = await _context.Routes
                .Where(r => r.DataSetId == dataSetId)
                .OrderBy(r => r.RouteId)
                .Select(r => new { r.Id, RouteId = r.RouteId, RouteShortName = r.RouteShortName, RouteLongName = r.RouteLongName })
                .ToListAsync();
            
            return Json(new { success = true, data = routes });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting routes");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetServiceIds(int dataSetId, string routeId)
    {
        try
        {
            var serviceIds = await _context.Trips
                .Where(t => t.DataSetId == dataSetId && t.RouteId == routeId)
                .Select(t => t.ServiceId)
                .Distinct()
                .OrderBy(s => s)
                .ToListAsync();
            return Json(new { success = true, data = serviceIds });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting service IDs");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetTrips(int dataSetId, string routeId)
    {
        try
        {
            var trips = await _context.Trips
                .Where(t => t.DataSetId == dataSetId && t.RouteId == routeId)
                .OrderBy(t => t.TripId)
                .ToListAsync();
            
            return Json(new { success = true, data = trips });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting trips");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetStops(int dataSetId)
    {
        try
        {
            var stops = await _context.Stops
                .Where(s => s.DataSetId == dataSetId)
                .OrderBy(s => s.StopId)
                .ToListAsync();
            
            return Json(new { success = true, data = stops });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stops");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetStopTimings(int dataSetId, string routeId)
    {
        try
        {
            var stopTimings = await _context.StopTimings
                .Where(st => st.DataSetId == dataSetId && 
                            _context.Trips.Any(t => t.DataSetId == dataSetId && 
                                                   t.RouteId == routeId && 
                                                   t.TripId == st.TripId))
                .OrderBy(st => st.TripId)
                .ThenBy(st => st.StopSequence)
                .ToListAsync();
            
            return Json(new { success = true, data = stopTimings });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting stop timings");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateTrips([FromBody] List<TripUpdateDto> trips)
    {
        try
        {
            foreach (var tripDto in trips)
            {
                var trip = await _context.Trips.FindAsync(tripDto.Id);
                if (trip != null)
                {
                    trip.TripHeadsign = tripDto.TripHeadsign;
                    trip.TripShortName = tripDto.TripShortName;
                    trip.DirectionId = tripDto.DirectionId;
                    trip.BlockId = tripDto.BlockId;
                    trip.ShapeId = tripDto.ShapeId;
                    trip.WheelchairAccessible = tripDto.WheelchairAccessible;
                    trip.BikesAllowed = tripDto.BikesAllowed;
                }
            }
            
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Trips updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating trips");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStops([FromBody] List<StopUpdateDto> stops)
    {
        try
        {
            foreach (var stopDto in stops)
            {
                var stop = await _context.Stops.FindAsync(stopDto.Id);
                if (stop != null)
                {
                    stop.StopCode = stopDto.StopCode;
                    stop.StopName = stopDto.StopName;
                    stop.StopDesc = stopDto.StopDesc;
                    stop.StopLat = stopDto.StopLat;
                    stop.StopLon = stopDto.StopLon;
                    stop.LocationType = stopDto.LocationType;
                    stop.WheelchairBoarding = stopDto.WheelchairBoarding;
                }
            }
            
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Stops updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stops");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateStopTimings([FromBody] List<StopTimingUpdateDto> stopTimings)
    {
        try
        {
            foreach (var stDto in stopTimings)
            {
                var stopTiming = await _context.StopTimings.FindAsync(stDto.Id);
                if (stopTiming != null)
                {
                    stopTiming.ArrivalTime = stDto.ArrivalTime;
                    stopTiming.DepartureTime = stDto.DepartureTime;
                    stopTiming.StopSequence = stDto.StopSequence;
                    stopTiming.StopHeadsign = stDto.StopHeadsign;
                    stopTiming.PickupType = stDto.PickupType;
                    stopTiming.DropOffType = stDto.DropOffType;
                    stopTiming.ShapeDistTraveled = stDto.ShapeDistTraveled;
                    stopTiming.Timepoint = stDto.Timepoint;
                }
            }
            
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Stop timings updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stop timings");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetCalendarDates(int dataSetId)
    {
        try
        {
            var calendarDates = await _context.CalendarDates
                .Where(cd => cd.DataSetId == dataSetId)
                .OrderBy(cd => cd.ServiceId)
                .ThenBy(cd => cd.Date)
                .ToListAsync();
            
            return Json(new { success = true, data = calendarDates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting calendar dates");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> UpdateCalendarDates([FromBody] List<CalendarDateUpdateDto> calendarDates)
    {
        try
        {
            foreach (var cdDto in calendarDates)
            {
                var calendarDate = await _context.CalendarDates.FindAsync(cdDto.Id);
                if (calendarDate != null)
                {
                    calendarDate.ServiceId = cdDto.ServiceId;
                    // Parse YYYYMMDD format to DateTime
                    if (!string.IsNullOrWhiteSpace(cdDto.Date) && cdDto.Date.Length == 8)
                    {
                        if (DateTime.TryParseExact(cdDto.Date, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, 
                            System.Globalization.DateTimeStyles.None, out var parsedDate))
                        {
                            calendarDate.Date = DateTime.SpecifyKind(parsedDate, DateTimeKind.Utc);
                        }
                    }
                    calendarDate.ExceptionType = cdDto.ExceptionType;
                }
            }
            
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Calendar dates updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating calendar dates");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteTrips([FromBody] List<int> ids)
    {
        try
        {
            var trips = await _context.Trips.Where(t => ids.Contains(t.Id)).ToListAsync();
            _context.Trips.RemoveRange(trips);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"{trips.Count} trip(s) deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting trips");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteStops([FromBody] List<int> ids)
    {
        try
        {
            var stops = await _context.Stops.Where(s => ids.Contains(s.Id)).ToListAsync();
            _context.Stops.RemoveRange(stops);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"{stops.Count} stop(s) deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting stops");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteStopTimings([FromBody] List<int> ids)
    {
        try
        {
            var stopTimings = await _context.StopTimings.Where(st => ids.Contains(st.Id)).ToListAsync();
            _context.StopTimings.RemoveRange(stopTimings);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"{stopTimings.Count} stop timing(s) deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting stop timings");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCalendarDates([FromBody] List<int> ids)
    {
        try
        {
            var calendarDates = await _context.CalendarDates.Where(cd => ids.Contains(cd.Id)).ToListAsync();
            _context.CalendarDates.RemoveRange(calendarDates);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = $"{calendarDates.Count} calendar date(s) deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting calendar dates");
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>Returns filtered data in run-cut layout: only main stops (timepoint=1), only stop names and times. Rows = main stops, columns = trips.</summary>
    [HttpGet]
    public async Task<IActionResult> GetFilteredDataForExport(int dataSetId, string routeId, string? serviceId = null)
    {
        try
        {
            var tripsQuery = _context.Trips
                .Where(t => t.DataSetId == dataSetId && t.RouteId == routeId);
            if (!string.IsNullOrWhiteSpace(serviceId))
                tripsQuery = tripsQuery.Where(t => t.ServiceId == serviceId);
            var trips = await tripsQuery.OrderBy(t => t.TripId).ToListAsync();

            if (!trips.Any())
                return Json(new { success = true, stopNames = Array.Empty<string>(), tripIds = Array.Empty<string>(), matrix = Array.Empty<object[]>() });

            var tripIds = trips.Select(t => t.TripId).ToList();

            // Only stop_timings where timepoint == 1 (main stops). Exclude timepoint 0, null, or any other value.
            var stopTimings = await _context.StopTimings
                .Where(st => st.DataSetId == dataSetId && tripIds.Contains(st.TripId) && st.Timepoint != null && st.Timepoint == 1)
                .OrderBy(st => st.TripId)
                .ThenBy(st => st.StopSequence ?? 0)
                .ToListAsync();

            // Row order = first trip's main stops in stop_sequence order (same stop can appear twice: start and end)
            var firstTripId = tripIds[0];
            var orderedStopIds = stopTimings
                .Where(st => st.TripId == firstTripId)
                .OrderBy(st => st.StopSequence ?? 0)
                .Select(st => st.StopId)
                .ToList();

            var distinctStopIds = orderedStopIds.Distinct().ToList();
            var stops = await _context.Stops
                .Where(s => s.DataSetId == dataSetId && distinctStopIds.Contains(s.StopId))
                .ToDictionaryAsync(s => s.StopId);

            var stopNames = orderedStopIds
                .Select(sid => stops.TryGetValue(sid, out var s) ? (s.StopName ?? sid) : sid)
                .ToList();

            // Lookup by (tripId, rowIndex) so first and last row (same stop, e.g. Southport) get different times
            var timeLookup = new Dictionary<(string, int), string>();
            foreach (var tripId in tripIds)
            {
                var timingsForTrip = stopTimings
                    .Where(st => st.TripId == tripId)
                    .OrderBy(st => st.StopSequence ?? 0)
                    .ToList();
                for (var i = 0; i < timingsForTrip.Count; i++)
                {
                    var st = timingsForTrip[i];
                    var time = !string.IsNullOrEmpty(st.DepartureTime) ? st.DepartureTime : st.ArrivalTime ?? "-";
                    if (time.Length > 5) time = time.Substring(0, 5); // HH:MM:SS -> HH:MM
                    timeLookup[(tripId, i)] = time;
                }
            }

            // Build matrix: rows = stops in sequence, columns = trips; cell = time at that position in the trip
            var matrix = new List<object[]>();
            for (var i = 0; i < orderedStopIds.Count; i++)
            {
                var stopId = orderedStopIds[i];
                var row = new object[tripIds.Count + 1];
                row[0] = stops.TryGetValue(stopId, out var s) ? (s.StopName ?? stopId) : stopId;
                for (var j = 0; j < tripIds.Count; j++)
                {
                    row[j + 1] = timeLookup.TryGetValue((tripIds[j], i), out var t) ? t : "-";
                }
                matrix.Add(row);
            }

            return Json(new { success = true, stopNames, tripIds, matrix });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting filtered data for export");
            return Json(new { success = false, message = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GenerateData(int dataSetId, string routeId, int? directionId = null, string? serviceId = null,
        bool includeWeekdays = true, bool includeSaturday = true, bool includeSundaysAndHolidays = true, bool groupByDate = false, bool oneRowPerTrip = false)
    {
        try
        {
            // 1. Build joined rows (one per stop_time: trip + route + stop_timing + stop)
            var tripsQuery = _context.Trips
                .Where(t => t.DataSetId == dataSetId && t.RouteId == routeId);
            if (directionId.HasValue)
                tripsQuery = tripsQuery.Where(t => t.DirectionId == directionId.Value);
            if (!string.IsNullOrWhiteSpace(serviceId))
                tripsQuery = tripsQuery.Where(t => t.ServiceId == serviceId);

            var trips = await tripsQuery.ToListAsync();
            if (!trips.Any())
                return Json(new { success = true, data = new List<object>() });

            var tripIds = trips.Select(t => t.TripId).ToList();
            var route = await _context.Routes.FirstOrDefaultAsync(r => r.DataSetId == dataSetId && r.RouteId == routeId);

            var stopTimings = await _context.StopTimings
                .Where(st => st.DataSetId == dataSetId && tripIds.Contains(st.TripId))
                .OrderBy(st => st.TripId)
                .ThenBy(st => st.StopSequence ?? 0)
                .ToListAsync();

            var stopIds = stopTimings.Select(st => st.StopId).Distinct().ToList();
            var stops = await _context.Stops
                .Where(s => s.DataSetId == dataSetId && stopIds.Contains(s.StopId))
                .ToListAsync();
            var stopMap = stops.ToDictionary(s => s.StopId);

            // 2. BuildFromJoined: group by trip_id, order by stop_sequence → one row per trip
            var tripLevelRows = new List<TripLevelRow>();
            foreach (var trip in trips)
            {
                var rowsForTrip = stopTimings.Where(st => st.TripId == trip.TripId).OrderBy(st => st.StopSequence ?? 0).ToList();
                if (!rowsForTrip.Any()) continue;

                var first = rowsForTrip.First();
                var last = rowsForTrip.Last();
                var firstStop = stopMap.GetValueOrDefault(first.StopId);
                var lastStop = stopMap.GetValueOrDefault(last.StopId);

                tripLevelRows.Add(new TripLevelRow
                {
                    RouteId = route?.RouteId ?? trip.RouteId,
                    RouteShortName = route?.RouteShortName,
                    RouteLongName = route?.RouteLongName,
                    RouteType = route?.RouteType,
                    AgencyId = route?.AgencyId,
                    ServiceId = trip.ServiceId,
                    TripId = trip.TripId,
                    TripHeadsign = trip.TripHeadsign,
                    DirectionId = trip.DirectionId,
                    ShapeId = trip.ShapeId,
                    FirstStopId = first.StopId,
                    FirstStopName = firstStop?.StopName,
                    StartTime = first.DepartureTime ?? first.ArrivalTime,
                    LastStopId = last.StopId,
                    LastStopName = lastStop?.StopName,
                    EndTime = last.ArrivalTime ?? last.DepartureTime,
                    StopCount = rowsForTrip.Count
                });
            }

            // 3. GetServiceDatesForRow: for each service_id, get dates where exception_type == 1 (service added)
            var serviceIds = tripLevelRows.Select(t => t.ServiceId).Distinct().ToList();
            var calendarDatesAdded = await _context.CalendarDates
                .Where(cd => cd.DataSetId == dataSetId && serviceIds.Contains(cd.ServiceId) && cd.ExceptionType == 1)
                .ToListAsync();
            var serviceDatesByServiceId = calendarDatesAdded
                .GroupBy(cd => cd.ServiceId)
                .ToDictionary(g => g.Key, g => g.Select(cd => cd.Date.ToString("yyyyMMdd")).OrderBy(d => d).ToList());

            // 4. Day of week: derive in MakeOutputRow from service date (no d_Date dependency so app works without that table)
            var dateKeyToDayName = new Dictionary<int, string>();

            List<object> filtered;

            if (oneRowPerTrip)
            {
                // One row per trip: no date expansion. Keep only trips that run on at least one selected day type (or have no dates).
                // For each trip we show the first matching service date and its day of week so Service Date and Day of Week columns aren't blank.
                var tripsMatchingDayFilter = tripLevelRows.Where(row =>
                {
                    var dates = GetServiceDatesForRow(row.ServiceId, serviceDatesByServiceId);
                    if (dates.Count == 0) return true;
                    return GetFirstMatchingServiceDate(row.ServiceId, serviceDatesByServiceId, dateKeyToDayName, includeWeekdays, includeSaturday, includeSundaysAndHolidays) != null;
                }).ToList();

                filtered = tripsMatchingDayFilter
                    .Select(row =>
                    {
                        var firstDate = GetFirstMatchingServiceDate(row.ServiceId, serviceDatesByServiceId, dateKeyToDayName, includeWeekdays, includeSaturday, includeSundaysAndHolidays) ?? "";
                        return (object)MakeOutputRow(row, firstDate, dateKeyToDayName);
                    })
                    .OrderBy(x => ((dynamic)x).TripId?.ToString() ?? "")
                    .ToList();
            }
            else
            {
                // 5. ExpandToTripPerServiceDate: one row per (trip, service_date)
                var result = new List<object>();
                foreach (var row in tripLevelRows)
                {
                    var dates = GetServiceDatesForRow(row.ServiceId, serviceDatesByServiceId);
                    if (dates.Count == 0)
                    {
                        result.Add(MakeOutputRow(row, "", dateKeyToDayName));
                    }
                    else
                    {
                        foreach (var date in dates)
                            result.Add(MakeOutputRow(row, date, dateKeyToDayName));
                    }
                }

                var orderedResult = result
                    .OrderBy(x => ((dynamic)x).ServiceDate?.ToString() ?? "")
                    .ThenBy(x => ((dynamic)x).TripId?.ToString() ?? "")
                    .ToList();

                // 6. Filter by day type: weekdays, Saturday, Sundays & holidays
                filtered = orderedResult.Where(x =>
                {
                    var dayName = ((dynamic)x).DayOfWeekName?.ToString() ?? "";
                    if (string.IsNullOrEmpty(dayName)) return true; // no day info = keep
                    var isWeekday = IsWeekday(dayName);
                    var isSaturday = string.Equals(dayName, "Saturday", StringComparison.OrdinalIgnoreCase);
                    var isSundayOrHoliday = string.Equals(dayName, "Sunday", StringComparison.OrdinalIgnoreCase); // TODO: add holiday check if you have a holidays table
                    return (includeWeekdays && isWeekday) || (includeSaturday && isSaturday) || (includeSundaysAndHolidays && isSundayOrHoliday);
                }).ToList();
            }

            // 7. Optional: group by service date so one row per date (with trip count) instead of one row per (trip, date)
            var final = oneRowPerTrip
                ? filtered.Select(x => AddTripCount(x, 1)).ToList()
                : groupByDate
                    ? GroupByServiceDate(filtered)
                    : filtered.Select(x => AddTripCount(x, 1)).ToList();

            // Prefer time direction 1 in the table; if none, show all (e.g. direction 0)
            var filteredDirection1 = filtered.Where(x =>
            {
                try
                {
                    var dir = ((dynamic)x).DirectionId;
                    return dir != null && Convert.ToInt32(dir) == 1;
                }
                catch { return false; }
            }).ToList();
            var filteredForDisplay = filteredDirection1.Count > 0 ? filteredDirection1 : filtered;

            // 8. Build matrix format: columns = stop names (timepoint == 1 only), rows = one per (trip or trip+date) with HH:MM timings only
            // Use first displayed trip for column order so headers (e.g. Marshall, Southport) match the data
            var firstDisplayTripId = filteredForDisplay.Count > 0 ? ((dynamic)filteredForDisplay[0]).TripId?.ToString() ?? "" : "";
            var columnOrderTrip = string.IsNullOrEmpty(firstDisplayTripId) ? trips[0] : trips.FirstOrDefault(t => t.TripId == firstDisplayTripId) ?? trips[0];
            var orderedStopIds = stopTimings
                .Where(st => st.TripId == columnOrderTrip.TripId && st.Timepoint == 1)
                .OrderBy(st => st.StopSequence ?? 0)
                .Select(st => st.StopId)
                .ToList();
            var stopNames = orderedStopIds
                .Select(sid => stopMap.TryGetValue(sid, out var s) ? (s.StopName ?? sid) : sid)
                .ToList();

            // Key by (tripId, stopId) so times align with column headers regardless of trip stop order
            var timeLookup = new Dictionary<(string TripId, string StopId), string>();
            foreach (var trip in trips)
            {
                var timingsForTrip = stopTimings
                    .Where(st => st.TripId == trip.TripId && st.Timepoint == 1)
                    .OrderBy(st => st.StopSequence ?? 0)
                    .ToList();
                foreach (var st in timingsForTrip)
                {
                    var raw = !string.IsNullOrEmpty(st.DepartureTime) ? st.DepartureTime : st.ArrivalTime ?? "-";
                    var time = raw.Length > 5 ? raw.Substring(0, 5) : raw; // HH:MM:SS -> HH:MM
                    timeLookup[(trip.TripId, st.StopId)] = time;
                }
            }

            var matrixRowsSource = filteredForDisplay; // each row has TripId
            var rows = matrixRowsSource
                .Select(row =>
                {
                    var tripId = ((dynamic)row).TripId?.ToString() ?? "";
                    return orderedStopIds
                        .Select(stopId => timeLookup.TryGetValue((tripId, stopId), out var t) ? t : "-")
                        .ToList();
                })
                .ToList();

            return Json(new { success = true, stopNames, rows });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating data");
            return Json(new { success = false, message = ex.Message });
        }
    }

    /// <summary>Group rows by ServiceDate; return one row per date with TripCount and first trip's details.</summary>
    private static List<object> GroupByServiceDate(List<object> rows)
    {
        return rows
            .GroupBy(x => ((dynamic)x).ServiceDate?.ToString() ?? "")
            .Select(g =>
            {
                var first = (dynamic)g.First();
                return (object)new
                {
                    first.RouteId,
                    first.RouteShortName,
                    first.RouteLongName,
                    first.RouteType,
                    first.AgencyId,
                    first.ServiceId,
                    TripId = (string?)"", // grouped: no single trip
                    first.TripHeadsign,
                    first.DirectionId,
                    first.ShapeId,
                    first.FirstStopId,
                    first.FirstStopName,
                    first.StartTime,
                    first.LastStopId,
                    first.LastStopName,
                    first.EndTime,
                    first.StopCount,
                    first.ServiceDate,
                    first.DayOfWeekName,
                    TripCount = g.Count()
                };
            })
            .OrderBy(x => ((dynamic)x).ServiceDate?.ToString() ?? "")
            .ToList();
    }

    private static object AddTripCount(object row, int tripCount)
    {
        var d = (dynamic)row;
        return new
        {
            d.RouteId,
            d.RouteShortName,
            d.RouteLongName,
            d.RouteType,
            d.AgencyId,
            d.ServiceId,
            d.TripId,
            d.TripHeadsign,
            d.DirectionId,
            d.ShapeId,
            d.FirstStopId,
            d.FirstStopName,
            d.StartTime,
            d.LastStopId,
            d.LastStopName,
            d.EndTime,
            d.StopCount,
            d.ServiceDate,
            d.DayOfWeekName,
            TripCount = tripCount
        };
    }

    /// <summary>Returns the first service date (YYYYMMDD) for this service that matches the selected day-type filter, or null.</summary>
    private static string? GetFirstMatchingServiceDate(string serviceId, IReadOnlyDictionary<string, List<string>> serviceDatesByServiceId, IReadOnlyDictionary<int, string> dateKeyToDayName, bool includeWeekdays, bool includeSaturday, bool includeSundaysAndHolidays)
    {
        var dates = GetServiceDatesForRow(serviceId, serviceDatesByServiceId);
        foreach (var d in dates)
        {
            if (d.Length != 8 || !int.TryParse(d, out var key)) continue;
            var dayName = dateKeyToDayName.TryGetValue(key, out var name) ? name : "";
            if (string.IsNullOrEmpty(dayName) && DateTime.TryParseExact(d, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
                dayName = dt.ToString("dddd");
            var isWeekday = IsWeekday(dayName);
            var isSaturday = string.Equals(dayName, "Saturday", StringComparison.OrdinalIgnoreCase);
            var isSunday = string.Equals(dayName, "Sunday", StringComparison.OrdinalIgnoreCase);
            if ((includeWeekdays && isWeekday) || (includeSaturday && isSaturday) || (includeSundaysAndHolidays && isSunday))
                return d;
        }
        return null;
    }

    private static bool IsWeekday(string dayName)
    {
        return string.Equals(dayName, "Monday", StringComparison.OrdinalIgnoreCase)
            || string.Equals(dayName, "Tuesday", StringComparison.OrdinalIgnoreCase)
            || string.Equals(dayName, "Wednesday", StringComparison.OrdinalIgnoreCase)
            || string.Equals(dayName, "Thursday", StringComparison.OrdinalIgnoreCase)
            || string.Equals(dayName, "Friday", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Get list of YYYYMMDD dates when the trip runs (exception_type 1 = service added).</summary>
    private static List<string> GetServiceDatesForRow(string serviceId, IReadOnlyDictionary<string, List<string>> serviceDatesByServiceId)
    {
        if (serviceDatesByServiceId.TryGetValue(serviceId, out var dates) && dates != null && dates.Count > 0)
            return dates.ToList();
        return new List<string>();
    }

    private static object MakeOutputRow(TripLevelRow row, string serviceDate, IReadOnlyDictionary<int, string> dateKeyToDayName)
    {
        var dayOfWeekName = "";
        if (serviceDate.Length == 8 && int.TryParse(serviceDate, out var dateKey))
        {
            if (dateKeyToDayName.TryGetValue(dateKey, out var name))
                dayOfWeekName = name;
            else if (DateTime.TryParseExact(serviceDate, "yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
                dayOfWeekName = dt.ToString("dddd");
        }
        return new
        {
            row.RouteId,
            row.RouteShortName,
            row.RouteLongName,
            row.RouteType,
            row.AgencyId,
            row.ServiceId,
            row.TripId,
            row.TripHeadsign,
            row.DirectionId,
            row.ShapeId,
            row.FirstStopId,
            row.FirstStopName,
            row.StartTime,
            row.LastStopId,
            row.LastStopName,
            row.EndTime,
            row.StopCount,
            ServiceDate = serviceDate,
            DayOfWeekName = dayOfWeekName
        };
    }

    /// <summary>Parse an uploaded Excel file and return headers + rows for Data Validator.</summary>
    [HttpPost]
    public async Task<IActionResult> ParseExcelForValidator(IFormFile excelFile, CancellationToken cancellationToken)
    {
        if (excelFile == null || excelFile.Length == 0)
            return Json(new { success = false, message = "Please select an Excel file." });
        try
        {
            using var stream = excelFile.OpenReadStream();
            using var workbook = new XLWorkbook(stream);
            var ws = workbook.Worksheet(1);
            var usedRange = ws.RangeUsed();
            if (usedRange == null)
                return Json(new { success = true, headers = Array.Empty<string>(), rows = new List<List<string?>>() });
            var firstRow = usedRange.FirstRow();
            var lastRow = usedRange.LastRow();
            var lastCol = usedRange.LastColumn().ColumnNumber();
            var headers = new List<string>();
            for (var c = 1; c <= lastCol; c++)
            {
                var val = firstRow.Cell(c).GetString();
                headers.Add(string.IsNullOrWhiteSpace(val) ? $"Column{c}" : val);
            }
            var rows = new List<List<string?>>();
            for (var r = firstRow.RowNumber() + 1; r <= lastRow.RowNumber(); r++)
            {
                var row = ws.Row(r);
                var list = new List<string?>();
                for (var c = 1; c <= lastCol; c++)
                {
                    var s = row.Cell(c).GetString();
                    list.Add(string.IsNullOrWhiteSpace(s) ? null : s.Trim());
                }
                rows.Add(list);
            }
            return Json(new { success = true, headers, rows });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing Excel for validator");
            return Json(new { success = false, message = ex.Message });
        }
    }
}

/// <summary>Trip-level row from BuildFromJoined (one row per trip, with first/last stop and times).</summary>
internal class TripLevelRow
{
    public string RouteId { get; set; } = string.Empty;
    public string? RouteShortName { get; set; }
    public string? RouteLongName { get; set; }
    public int? RouteType { get; set; }
    public string? AgencyId { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public string TripId { get; set; } = string.Empty;
    public string? TripHeadsign { get; set; }
    public int? DirectionId { get; set; }
    public string? ShapeId { get; set; }
    public string FirstStopId { get; set; } = string.Empty;
    public string? FirstStopName { get; set; }
    public string? StartTime { get; set; }
    public string LastStopId { get; set; } = string.Empty;
    public string? LastStopName { get; set; }
    public string? EndTime { get; set; }
    public int StopCount { get; set; }
}

// DTOs for updates
public class TripUpdateDto
{
    public int Id { get; set; }
    public string? TripHeadsign { get; set; }
    public string? TripShortName { get; set; }
    public int? DirectionId { get; set; }
    public string? BlockId { get; set; }
    public string? ShapeId { get; set; }
    public int? WheelchairAccessible { get; set; }
    public int? BikesAllowed { get; set; }
}

public class StopUpdateDto
{
    public int Id { get; set; }
    public string? StopCode { get; set; }
    public string? StopName { get; set; }
    public string? StopDesc { get; set; }
    public decimal? StopLat { get; set; }
    public decimal? StopLon { get; set; }
    public int? LocationType { get; set; }
    public int? WheelchairBoarding { get; set; }
}

public class StopTimingUpdateDto
{
    public int Id { get; set; }
    public string? ArrivalTime { get; set; }
    public string? DepartureTime { get; set; }
    public int? StopSequence { get; set; }
    public string? StopHeadsign { get; set; }
    public int? PickupType { get; set; }
    public int? DropOffType { get; set; }
    public decimal? ShapeDistTraveled { get; set; }
    public int? Timepoint { get; set; }
}

public class CalendarDateUpdateDto
{
    public int Id { get; set; }
    public string ServiceId { get; set; } = string.Empty;
    public string Date { get; set; } = string.Empty;
    public int ExceptionType { get; set; }
}
