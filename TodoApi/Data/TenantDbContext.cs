using Microsoft.EntityFrameworkCore;
using TodoApi.Infrastructure;
using TodoApi.Models;

namespace TodoApi.Data;

public sealed class TenantDbContext : AppDbContextBase
{
    public Guid TenantId { get; }

    public TenantDbContext(DbContextOptions<TenantDbContext> options, ITenantAccessor tenantAccessor)
        : base(options)
    {
        TenantId = tenantAccessor.GetRequiredTenantId();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Tenant-scoped data
        modelBuilder.Entity<TodoList>()
            .HasQueryFilter(l => l.TenantId == TenantId);
    }
}
