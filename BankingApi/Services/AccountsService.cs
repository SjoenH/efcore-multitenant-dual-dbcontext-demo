using BankingApi.Data;
using BankingApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Services;

public interface IAccountsService
{
    Task<IReadOnlyList<AccountResponse>> GetAll();
    Task<AccountResponse?> GetById(Guid id);
    Task<IReadOnlyList<AccountResponse>> GetByCustomerId(Guid customerId);
    Task<IReadOnlyList<TransactionResponse>> GetTransactionsByAccountId(Guid accountId, Guid? ownerCustomerId);
    Task<AccountResponse> Create(AccountRequest request);
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
        return await _db
            .Accounts.AsNoTracking()
            .OrderBy(x => x.AccountNumber)
            .Select(AccountResponse.Projection)
            .ToListAsync();
    }

    public async Task<AccountResponse?> GetById(Guid id)
    {
        return await _db
            .Accounts.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(AccountResponse.Projection)
            .SingleOrDefaultAsync();
    }

    public async Task<IReadOnlyList<AccountResponse>> GetByCustomerId(Guid customerId)
    {
        return await _db
            .Accounts.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.AccountNumber)
            .Select(AccountResponse.Projection)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetTransactionsByAccountId(
        Guid accountId,
        Guid? ownerCustomerId
    )
    {
        // If ownerCustomerId is provided, verify the account belongs to that customer.
        if (ownerCustomerId is not null)
        {
            var ownsAccount = await _db
                .Accounts.AsNoTracking()
                .AnyAsync(x => x.Id == accountId && x.CustomerId == ownerCustomerId);

            if (!ownsAccount)
            {
                return null!; // caller maps null → 404
            }
        }

        return await _db
            .Transactions.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.Timestamp)
            .Select(TransactionResponse.Projection)
            .ToListAsync();
    }

    public async Task<AccountResponse> Create(AccountRequest request)
    {
        // Global filters guarantee Customer exists only if it's in this bank.
        var customerExists = await _db.Customers.AnyAsync(x => x.Id == request.CustomerId);
        if (!customerExists)
        {
            throw new InvalidOperationException("Customer not found");
        }

        var bank = await _db.Banks.SingleAsync(x => x.Id == _db.BankId);
        var entity = AccountFactory.Build(_db.BankId, request.CustomerId, bank.Code);

        _db.Accounts.Add(entity);
        await _db.SaveChangesAsync();

        return entity.ToResponse();
    }

    public async Task<bool> Delete(Guid id)
    {
        return await _db.DeleteByIdAsync(_db.Accounts, id);
    }
}
