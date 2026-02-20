using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using BankingApi.Models;

namespace BankingApi.Dtos;

public sealed class AccountResponse
{
    public Guid Id { get; init; }
    public Guid BankId { get; init; }
    public Guid CustomerId { get; init; }
    public string AccountNumber { get; init; } = string.Empty;
    public decimal Balance { get; init; }
    public string Currency { get; init; } = "NOK";
    public DateTimeOffset CreatedAt { get; init; }

    public static Expression<Func<Account, AccountResponse>> Projection =>
        x => new AccountResponse
        {
            Id = x.Id,
            BankId = x.BankId,
            CustomerId = x.CustomerId,
            AccountNumber = x.AccountNumber,
            Balance = x.Balance,
            Currency = x.Currency,
            CreatedAt = x.CreatedAt,
        };
}

public static class AccountExtensions
{
    public static AccountResponse ToResponse(this Account x) =>
        new()
        {
            Id = x.Id,
            BankId = x.BankId,
            CustomerId = x.CustomerId,
            AccountNumber = x.AccountNumber,
            Balance = x.Balance,
            Currency = x.Currency,
            CreatedAt = x.CreatedAt,
        };
}

public class AccountRequest
{
    [Required]
    public Guid CustomerId { get; set; }
}

public sealed class AdminAccountRequest : AccountRequest
{
    [Required]
    public Guid BankId { get; set; }
}
