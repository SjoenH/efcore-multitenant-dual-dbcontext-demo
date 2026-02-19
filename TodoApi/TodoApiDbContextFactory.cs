using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using TodoApi.Data;

namespace TodoApi;

public sealed class TodoApiDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
{
    public AdminDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
        optionsBuilder.UseSqlite("Data Source=TodoApi.mt.db");
        return new AdminDbContext(optionsBuilder.Options);
    }
}
