using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using HelpDesk.Api.Data;
using HelpDesk.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class TicketsController(AppDbContext db) : ControllerBase
{
    public record TicketRequest([Required, MinLength(3), MaxLength(160)] string Title,
        string Description, string Priority, string Status, string? AssignedTo);
    private Guid TenantId => Guid.Parse(User.FindFirstValue("tenant_id")!);
    private Guid UserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private static readonly string[] Priorities = ["Low", "Medium", "High", "Critical"];
    private static readonly string[] Statuses = ["Open", "In Progress", "Resolved", "Closed"];

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search, [FromQuery] string? status)
    {
        var query = db.Tickets.AsNoTracking().Where(t => t.TenantId == TenantId);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(t => t.Title.Contains(search) || t.Description.Contains(search));
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(t => t.Status == status);
        var items = await query.OrderByDescending(t => t.CreatedAt).ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id)
    {
        var ticket = await db.Tickets.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id && t.TenantId == TenantId);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost]
    public async Task<IActionResult> Create(TicketRequest request)
    {
        if (!Priorities.Contains(request.Priority) || !Statuses.Contains(request.Status))
            return BadRequest(new { message = "Invalid priority or status." });
        var ticket = new Ticket { TenantId = TenantId, CreatedBy = UserId,
            Title = request.Title.Trim(), Description = request.Description ?? "",
            Priority = request.Priority, Status = request.Status, AssignedTo = request.AssignedTo };
        db.Tickets.Add(ticket);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = ticket.Id }, ticket);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, TicketRequest request)
    {
        if (!Priorities.Contains(request.Priority) || !Statuses.Contains(request.Status))
            return BadRequest(new { message = "Invalid priority or status." });
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == TenantId);
        if (ticket is null) return NotFound();
        ticket.Title = request.Title.Trim();
        ticket.Description = request.Description ?? "";
        ticket.Priority = request.Priority;
        ticket.Status = request.Status;
        ticket.AssignedTo = request.AssignedTo;
        ticket.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Ok(ticket);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ticket = await db.Tickets.FirstOrDefaultAsync(t => t.Id == id && t.TenantId == TenantId);
        if (ticket is null) return NotFound();
        db.Tickets.Remove(ticket);
        await db.SaveChangesAsync();
        return NoContent();
    }
}

