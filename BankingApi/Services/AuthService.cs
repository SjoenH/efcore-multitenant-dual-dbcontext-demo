using BankingApi.Data;
using BankingApi.Dtos;
using BankingApi.Infrastructure;
using BankingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Services;

public interface IAuthService
{
    Task<User?> FindUserByEmailAsync(string email);
    Task<SeededLoginsResponse> GetSeededLoginsAsync();
}

public sealed class AuthService : IAuthService
{
    private readonly AdminDbContext _db;

    public AuthService(AdminDbContext db)
    {
        _db = db;
    }

    public async Task<User?> FindUserByEmailAsync(string email)
    {
        return await _db.Users.SingleOrDefaultAsync(u => u.Email == email);
    }

    public async Task<SeededLoginsResponse> GetSeededLoginsAsync()
    {
        var bankAId = await _db
            .Banks.AsNoTracking()
            .Where(x => x.Code == "NO-001")
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync();

        var bankBId = await _db
            .Banks.AsNoTracking()
            .Where(x => x.Code == "SE-001")
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync();

        var customerAId = await _db
            .Customers.AsNoTracking()
            .Where(x => x.Email == SeedData.CustomerAEmail)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync();

        var customerBId = await _db
            .Customers.AsNoTracking()
            .Where(x => x.Email == SeedData.CustomerBEmail)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync();

        return new SeededLoginsResponse
        {
            AdminEmail = SeedData.AdminEmail,
            StaffBankAEmail = SeedData.StaffBankAEmail,
            StaffBankBEmail = SeedData.StaffBankBEmail,
            CustomerAEmail = SeedData.CustomerAEmail,
            CustomerBEmail = SeedData.CustomerBEmail,
            BankAId = bankAId,
            BankBId = bankBId,
            CustomerAId = customerAId,
            CustomerBId = customerBId,
        };
    }
}
