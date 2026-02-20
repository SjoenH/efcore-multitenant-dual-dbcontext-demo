using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using BankingApi.Models;

namespace BankingApi.Dtos;

public sealed class TransactionResponse
{
    public Guid Id { get; init; }
    public Guid BankId { get; init; }
    public Guid AccountId { get; init; }
    public decimal Amount { get; init; }
    public string Type { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTimeOffset Timestamp { get; init; }

    public static Expression<Func<Transaction, TransactionResponse>> Projection =>
        x => new TransactionResponse
        {
            Id = x.Id,
            BankId = x.BankId,
            AccountId = x.AccountId,
            Amount = x.Amount,
            Type = x.Type.ToString(),
            Description = x.Description,
            Timestamp = x.Timestamp,
        };
}

public static class TransactionExtensions
{
    public static TransactionResponse ToResponse(this Transaction x) =>
        new()
        {
            Id = x.Id,
            BankId = x.BankId,
            AccountId = x.AccountId,
            Amount = x.Amount,
            Type = x.Type.ToString(),
            Description = x.Description,
            Timestamp = x.Timestamp,
        };
}

public class TransactionRequest
{
    [Required]
    public Guid AccountId { get; set; }

    [Required]
    [Range(typeof(decimal), "0.01", "79228162514264337593543950335")]
    public decimal Amount { get; set; }

    [Required]
    [RegularExpression("^(Credit|Debit)$")]
    public string Type { get; set; } = "Credit";

    [MaxLength(400)]
    public string? Description { get; set; }
}

public sealed class AdminTransactionRequest : TransactionRequest
{
    [Required]
    public Guid BankId { get; set; }
}
