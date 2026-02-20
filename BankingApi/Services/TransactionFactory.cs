using BankingApi.Dtos;
using BankingApi.Models;

namespace BankingApi.Services;

internal static class TransactionFactory
{
    /// <summary>
    /// Validates the request, mutates <paramref name="account"/>'s balance, and returns a
    /// ready-to-add <see cref="Transaction"/> entity.  The caller is responsible for adding
    /// the entity to the DbSet and saving changes.
    /// </summary>
    internal static Transaction Build(Guid bankId, Account account, CreateTransactionRequest request)
    {
        var type = request.Type.Equals("Debit", StringComparison.OrdinalIgnoreCase)
            ? TransactionType.Debit
            : TransactionType.Credit;

        if (type == TransactionType.Debit && account.Balance < request.Amount)
        {
            throw new InvalidOperationException("Insufficient funds");
        }

        if (type == TransactionType.Credit)
        {
            account.Balance += request.Amount;
        }
        else
        {
            account.Balance -= request.Amount;
        }

        return new Transaction
        {
            Id = Guid.NewGuid(),
            BankId = bankId,
            AccountId = account.Id,
            Amount = request.Amount,
            Type = type,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Timestamp = DateTimeOffset.UtcNow,
        };
    }
}
