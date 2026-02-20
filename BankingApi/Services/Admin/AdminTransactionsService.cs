using BankingApi.Data;
using BankingApi.Dtos;
using BankingApi.Models;
using BankingApi.Services;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Services.Admin;

public interface IAdminTransactionsService
{
    Task<IReadOnlyList<TransactionResponse>> GetAll();
    Task<TransactionResponse?> GetById(Guid id);
    Task<TransactionResponse> Create(CreateAdminTransactionRequest request);
    Task<bool> Delete(Guid id);
}

public sealed class AdminTransactionsService : IAdminTransactionsService
{
    private readonly AdminDbContext _db;

    public AdminTransactionsService(AdminDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<TransactionResponse>> GetAll()
    {
        return await _db
            .Transactions.AsNoTracking()
            .OrderByDescending(x => x.Timestamp)
            .Select(TransactionResponse.Projection)
            .ToListAsync();
    }

    public async Task<TransactionResponse?> GetById(Guid id)
    {
        return await _db
            .Transactions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(TransactionResponse.Projection)
            .SingleOrDefaultAsync();
    }

    public async Task<TransactionResponse> Create(CreateAdminTransactionRequest request)
    {
        var account = await _db.Accounts.SingleOrDefaultAsync(x => x.Id == request.AccountId);
        if (account is null || account.BankId != request.BankId)
        {
            throw new InvalidOperationException("Account not found in bank");
        }

        var entity = TransactionFactory.Build(request.BankId, account, request);

        _db.Transactions.Add(entity);
        await _db.SaveChangesAsync();

        return entity.ToResponse();
    }

    public async Task<bool> Delete(Guid id)
    {
        return await _db.DeleteByIdAsync(_db.Transactions, id);
    }
}
