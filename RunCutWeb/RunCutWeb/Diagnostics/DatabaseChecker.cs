using Microsoft.EntityFrameworkCore;
using RunCutWeb.Infrastructure.Data;
using RunCutWeb.Domain.Entities;

namespace RunCutWeb.Diagnostics
{
    public class DatabaseChecker
    {
        private readonly ApplicationDbContext _context;
        
        public DatabaseChecker(ApplicationDbContext context)
        {
            _context = context;
        }
        
        public async Task<string> CheckDatabaseContentsAsync()
        {
            var result = new System.Text.StringBuilder();
            
            try
            {
                // Check if we can connect
                await _context.Database.CanConnectAsync();
                result.AppendLine("✅ Database connection successful");
                
                // Check table counts
                var dataSetCount = await _context.DataSets.CountAsync();
                var routeCount = await _context.Routes.CountAsync();
                var stopCount = await _context.Stops.CountAsync();
                var tripCount = await _context.Trips.CountAsync();
                var stopTimingCount = await _context.StopTimings.CountAsync();
                var calendarDateCount = await _context.CalendarDates.CountAsync();
                
                result.AppendLine($"📊 Database Contents:");
                result.AppendLine($"   DataSets: {dataSetCount}");
                result.AppendLine($"   Routes: {routeCount}");
                result.AppendLine($"   Stops: {stopCount}");
                result.AppendLine($"   Trips: {tripCount}");
                result.AppendLine($"   StopTimings: {stopTimingCount}");
                result.AppendLine($"   CalendarDates: {calendarDateCount}");
                
                // If we have datasets, show details
                if (dataSetCount > 0)
                {
                    var dataSets = await _context.DataSets
                        .Select(d => new { d.Id, d.Name, d.CreatedDate })
                        .ToListAsync();
                    
                    result.AppendLine("\n📁 Datasets:");
                    foreach (var ds in dataSets)
                    {
                        result.AppendLine($"   ID: {ds.Id}, Name: '{ds.Name}', Created: {ds.CreatedDate:yyyy-MM-dd HH:mm:ss}");
                        
                        // Count related records for this dataset
                        var routes = await _context.Routes.CountAsync(r => r.DataSetId == ds.Id);
                        var stops = await _context.Stops.CountAsync(s => s.DataSetId == ds.Id);
                        var trips = await _context.Trips.CountAsync(t => t.DataSetId == ds.Id);
                        var stopTimings = await _context.StopTimings.CountAsync(st => st.DataSetId == ds.Id);
                        var calendarDates = await _context.CalendarDates.CountAsync(cd => cd.DataSetId == ds.Id);
                        
                        result.AppendLine($"      └─ Routes: {routes}, Stops: {stops}, Trips: {trips}, StopTimings: {stopTimings}, CalendarDates: {calendarDates}");
                    }
                }
                
                // Check for any recent errors in logs (if we had access)
                result.AppendLine("\n🔍 Diagnosis:");
                if (dataSetCount > 0 && routeCount == 0)
                {
                    result.AppendLine("   ⚠️  Datasets exist but no routes found - routes file may not have been imported");
                }
                if (dataSetCount > 0 && stopCount == 0)
                {
                    result.AppendLine("   ⚠️  Datasets exist but no stops found - stops file may not have been imported");
                }
                if (dataSetCount > 0 && tripCount == 0)
                {
                    result.AppendLine("   ⚠️  Datasets exist but no trips found - trips file may not have been imported");
                }
                if (dataSetCount > 0 && calendarDateCount == 0)
                {
                    result.AppendLine("   ⚠️  Datasets exist but no calendar dates found - calendar dates file may not have been imported");
                }
            }
            catch (Exception ex)
            {
                result.AppendLine($"❌ Error checking database: {ex.Message}");
                if (ex.InnerException != null)
                {
                    result.AppendLine($"   Inner error: {ex.InnerException.Message}");
                }
            }
            
            return result.ToString();
        }
    }
}
