using BankingApi.Data;
using BankingApi.Dtos;
using BankingApi.Models;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Services;

public interface IAccountsService
{
    Task<IReadOnlyList<AccountResponse>> GetAll();
    Task<AccountResponse?> GetById(Guid id);
    Task<AccountResponse> Create(CreateAccountRequest request);
    Task<bool> Delete(Guid id);
}

public sealed class AccountsService : IAccountsService
{
    private readonly TenantDbContext _db;

    public AccountsService(TenantDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<AccountResponse>> GetAll()
    {
        return await _db.Accounts.AsNoTracking()
            .OrderBy(x => x.AccountNumber)
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

    public async Task<AccountResponse> Create(CreateAccountRequest request)
    {
        // Global filters guarantee Customer exists only if it's in this bank.
        var customerExists = await _db.Customers.AnyAsync(x => x.Id == request.CustomerId);
        if (!customerExists)
        {
            throw new InvalidOperationException("Customer not found");
        }

        var entity = new Account
        {
            Id = Guid.NewGuid(),
            BankId = _db.BankId,
            CustomerId = request.CustomerId,
            AccountNumber = $"{_db.BankId:N}-{Guid.NewGuid():N}".ToUpperInvariant(),
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
