using System.ComponentModel.DataAnnotations;

namespace BankingApi.Models;

public sealed class Transaction
{
    public Guid Id { get; set; }

    public Guid BankId { get; set; }
    public Guid AccountId { get; set; }

    public Account? Account { get; set; }

    public decimal Amount { get; set; }
    public TransactionType Type { get; set; }

    [MaxLength(400)]
    public string? Description { get; set; }

    public DateTimeOffset Timestamp { get; set; }
}
