using Microsoft.EntityFrameworkCore;

namespace BankingApi.Data;

public sealed class AdminDbContext : AppDbContextBase
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options)
        : base(options)
    {
    }
}
