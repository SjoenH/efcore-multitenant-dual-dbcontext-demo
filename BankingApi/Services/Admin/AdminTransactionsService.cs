using BankingApi.Data;
using BankingApi.Dtos;
using BankingApi.Models;
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
        return await _db.Transactions.AsNoTracking()
            .OrderByDescending(x => x.Timestamp)
            .Select(x => new TransactionResponse
            {
                Id = x.Id,
                BankId = x.BankId,
                AccountId = x.AccountId,
                Amount = x.Amount,
                Type = x.Type.ToString(),
                Description = x.Description,
                Timestamp = x.Timestamp
            })
            .ToListAsync();
    }

    public async Task<TransactionResponse?> GetById(Guid id)
    {
        return await _db.Transactions.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new TransactionResponse
            {
                Id = x.Id,
                BankId = x.BankId,
                AccountId = x.AccountId,
                Amount = x.Amount,
                Type = x.Type.ToString(),
                Description = x.Description,
                Timestamp = x.Timestamp
            })
            .SingleOrDefaultAsync();
    }

    public async Task<TransactionResponse> Create(CreateAdminTransactionRequest request)
    {
        if (request.BankId == Guid.Empty)
        {
            throw new InvalidOperationException("BankId is required");
        }

        var account = await _db.Accounts.SingleOrDefaultAsync(x => x.Id == request.AccountId);
        if (account is null || account.BankId != request.BankId)
        {
            throw new InvalidOperationException("Account not found in bank");
        }

        var type = request.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase)
            ? TransactionType.Debit
            : TransactionType.Credit;

        if (type == TransactionType.Debit && account.Balance < request.Amount)
        {
            throw new InvalidOperationException("Insufficient funds");
        }

        var entity = new Transaction
        {
            Id = Guid.NewGuid(),
            BankId = request.BankId,
            AccountId = account.Id,
            Amount = request.Amount,
            Type = type,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Timestamp = DateTimeOffset.UtcNow
        };

        if (type == TransactionType.Credit)
        {
            account.Balance += request.Amount;
        }
        else
        {
            account.Balance -= request.Amount;
        }

        _db.Transactions.Add(entity);
        await _db.SaveChangesAsync();

        return new TransactionResponse
        {
            Id = entity.Id,
            BankId = entity.BankId,
            AccountId = entity.AccountId,
            Amount = entity.Amount,
            Type = entity.Type.ToString(),
            Description = entity.Description,
            Timestamp = entity.Timestamp
        };
    }

    public async Task<bool> Delete(Guid id)
    {
        var entity = await _db.Transactions.SingleOrDefaultAsync(x => x.Id == id);
        if (entity is null)
        {
            return false;
        }

        _db.Transactions.Remove(entity);
        await _db.SaveChangesAsync();
        return true;
    }
}
