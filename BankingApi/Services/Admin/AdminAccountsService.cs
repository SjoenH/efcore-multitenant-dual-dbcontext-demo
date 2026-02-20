using BankingApi.Data;
using BankingApi.Dtos;
using BankingApi.Services;
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
        return await _db
            .Accounts.AsNoTracking()
            .OrderBy(x => x.BankId)
            .ThenBy(x => x.AccountNumber)
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

    public async Task<AccountResponse> Create(CreateAdminAccountRequest request)
    {
        var customer = await _db.Customers.SingleOrDefaultAsync(x => x.Id == request.CustomerId);
        if (customer is null || customer.BankId != request.BankId)
        {
            throw new InvalidOperationException("Customer not found in bank");
        }

        var bank = await _db.Banks.SingleAsync(x => x.Id == request.BankId);
        var entity = AccountFactory.Build(request.BankId, request.CustomerId, bank.Code);

        _db.Accounts.Add(entity);
        await _db.SaveChangesAsync();

        return entity.ToResponse();
    }

    public async Task<bool> Delete(Guid id)
    {
        return await _db.DeleteByIdAsync(_db.Accounts, id);
    }
}
