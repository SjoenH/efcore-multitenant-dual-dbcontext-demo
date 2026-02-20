using BankingApi.Data;
using BankingApi.Dtos;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Services;

public interface ITransactionsService
{
    Task<IReadOnlyList<TransactionResponse>> GetAll();
    Task<TransactionResponse?> GetById(Guid id);
    Task<TransactionResponse> Create(TransactionRequest request);
    Task<bool> Delete(Guid id);
}

public sealed class TransactionsService : ITransactionsService
{
    private readonly TenantDbContext _db;

    public TransactionsService(TenantDbContext db)
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

    public async Task<TransactionResponse> Create(TransactionRequest request)
    {
        var account = await _db.Accounts.SingleOrDefaultAsync(x => x.Id == request.AccountId);
        if (account is null)
        {
            throw new InvalidOperationException("Account not found");
        }

        var entity = TransactionFactory.Build(_db.BankId, account, request);

        _db.Transactions.Add(entity);
        await _db.SaveChangesAsync();

        return entity.ToResponse();
    }

    public async Task<bool> Delete(Guid id)
    {
        return await _db.DeleteByIdAsync(_db.Transactions, id);
    }
}
