using System.Security.Claims;
using HelpDesk.Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Controllers;

[ApiController, Authorize, Route("api/[controller]")]
public class DashboardController(AppDbContext db) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<IActionResult> Summary()
    {
        var tenantId = Guid.Parse(User.FindFirstValue("tenant_id")!);
        var tickets = db.Tickets.AsNoTracking().Where(t => t.TenantId == tenantId);
        return Ok(new {
            total = await tickets.CountAsync(),
            open = await tickets.CountAsync(t => t.Status == "Open"),
            inProgress = await tickets.CountAsync(t => t.Status == "In Progress"),
            resolved = await tickets.CountAsync(t => t.Status == "Resolved" || t.Status == "Closed"),
            critical = await tickets.CountAsync(t => t.Priority == "Critical" && t.Status != "Resolved" && t.Status != "Closed")
        });
    }
}

