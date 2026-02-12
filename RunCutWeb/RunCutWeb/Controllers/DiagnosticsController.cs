using Microsoft.AspNetCore.Mvc;
using RunCutWeb.Infrastructure.Data;
using RunCutWeb.Diagnostics;

namespace RunCutWeb.Controllers;

public class DiagnosticsController : Controller
{
    private readonly ApplicationDbContext _context;
    
    public DiagnosticsController(ApplicationDbContext context)
    {
        _context = context;
    }
    
    [HttpGet]
    public async Task<IActionResult> CheckDatabase()
    {
        var checker = new DatabaseChecker(_context);
        var result = await checker.CheckDatabaseContentsAsync();
        
        return Json(new { success = true, data = result });
    }
}
