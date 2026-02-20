using BankingApi.Dtos;
using BankingApi.Models;

namespace BankingApi.Services;

internal static class AccountFactory
{
    /// <summary>
    /// Builds a new <see cref="Account"/> entity ready to be added to a DbSet.
    /// The caller is responsible for adding the entity and saving changes.
    /// </summary>
    internal static Account Build(Guid bankId, Guid customerId, string bankCode) =>
        new()
        {
            Id = Guid.NewGuid(),
            BankId = bankId,
            CustomerId = customerId,
            AccountNumber = $"{bankCode}-{Guid.NewGuid():N}".ToUpperInvariant(),
            Balance = 0m,
            Currency = Currency.Default,
            CreatedAt = DateTimeOffset.UtcNow,
        };
}
