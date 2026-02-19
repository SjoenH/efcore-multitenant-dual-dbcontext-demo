using Microsoft.EntityFrameworkCore;

namespace TodoApi.Data;

public sealed class AdminDbContext : AppDbContextBase
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options)
        : base(options)
    {
    }
}
