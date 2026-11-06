using System.ComponentModel.DataAnnotations;
using HelpDesk.Api.Data;
using HelpDesk.Api.Models;
using HelpDesk.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(AppDbContext db, PasswordService passwords, JwtTokenService tokens) : ControllerBase
{
    public record RegisterRequest([Required, MinLength(2)] string CompanyName,
        [Required] string FullName, [Required, EmailAddress] string Email,
        [Required, MinLength(8)] string Password);
    public record LoginRequest([Required, EmailAddress] string Email, [Required] string Password);

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var companyName = request.CompanyName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.Tenants.AnyAsync(t => t.Name == companyName))
            return Conflict(new { message = "A company with this name already exists." });

        var tenant = new Tenant { Name = companyName };
        var user = new User { TenantId = tenant.Id, FullName = request.FullName.Trim(),
            Email = email, PasswordHash = passwords.Hash(request.Password), Role = "Admin" };
        db.Tenants.Add(tenant);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return Ok(new { token = tokens.Create(user, tenant.Name), user = new { user.FullName, user.Email, user.Role }, tenant = tenant.Name });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        if (user is null || !passwords.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Invalid email or password." });
        var tenant = await db.Tenants.FindAsync(user.TenantId);
        if (tenant is null) return Unauthorized();
        return Ok(new { token = tokens.Create(user, tenant.Name), user = new { user.FullName, user.Email, user.Role }, tenant = tenant.Name });
    }
}

