using BankingApi.Data;
using BankingApi.Dtos;
using BankingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Services.Admin;

public interface IAdminAccountsService
{
    Task<IReadOnlyList<AccountResponse>> GetAll();
    Task<AccountResponse?> GetById(Guid id);
    Task<AccountResponse> Create(CreateAdminAccountRequest request);
    Task<bool> Delete(Guid id);
}

public sealed class AdminAccountsService : IAdminAccountsService
{
    private readonly AdminDbContext _db;

    public AdminAccountsService(AdminDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAll()
    {
        return await _db.Accounts.AsNoTracking()
            .OrderBy(x => x.BankId)
            .ThenBy(x => x.AccountNumber)
            .Select(x => new AccountResponse
            {
                Id = x.Id,
                BankId = x.BankId,
                CustomerId = x.CustomerId,
                AccountNumber = x.AccountNumber,
                Balance = x.Balance,
                Currency = x.Currency,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<AccountResponse?> GetById(Guid id)
    {
        return await _db.Accounts.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new AccountResponse
            {
                Id = x.Id,
                BankId = x.BankId,
                CustomerId = x.CustomerId,
                AccountNumber = x.AccountNumber,
                Balance = x.Balance,
                Currency = x.Currency,
                CreatedAt = x.CreatedAt
            })
            .SingleOrDefaultAsync();
    }

    public async Task<AccountResponse> Create(CreateAdminAccountRequest request)
    {
        if (request.BankId == Guid.Empty)
        {
            throw new InvalidOperationException("BankId is required");
        }

        var customer = await _db.Customers.SingleOrDefaultAsync(x => x.Id == request.CustomerId);
        if (customer is null || customer.BankId != request.BankId)
        {
            throw new InvalidOperationException("Customer not found in bank");
        }

        var bank = await _db.Banks.SingleAsync(x => x.Id == request.BankId);

        var entity = new Account
        {
            Id = Guid.NewGuid(),
            BankId = request.BankId,
            CustomerId = request.CustomerId,
            AccountNumber = $"{bank.Code}-{Guid.NewGuid():N}".ToUpperInvariant(),
            Balance = 0m,
            Currency = "NOK",
            CreatedAt = DateTimeOffset.UtcNow
        };

        _db.Accounts.Add(entity);
        await _db.SaveChangesAsync();

        return new AccountResponse
        {
            Id = entity.Id,
            BankId = entity.BankId,
            CustomerId = entity.CustomerId,
            AccountNumber = entity.AccountNumber,
            Balance = entity.Balance,
            Currency = entity.Currency,
            CreatedAt = entity.CreatedAt
        };
    }

    public async Task<bool> Delete(Guid id)
    {
        var entity = await _db.Accounts.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return false;
        }

        _db.Accounts.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
