using HelpDesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Ticket> Tickets => Set<Ticket>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tenant>().HasIndex(x => x.Name).IsUnique();
        modelBuilder.Entity<User>().HasIndex(x => x.Email).IsUnique();
        modelBuilder.Entity<Ticket>().HasIndex(x => new { x.TenantId, x.CreatedAt });
        modelBuilder.Entity<Ticket>().Property(x => x.Title).HasMaxLength(160).IsRequired();
        modelBuilder.Entity<Ticket>().Property(x => x.Priority).HasMaxLength(20);
        modelBuilder.Entity<Ticket>().Property(x => x.Status).HasMaxLength(20);
    }
}

