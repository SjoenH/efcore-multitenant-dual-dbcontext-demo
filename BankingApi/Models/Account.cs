using System.ComponentModel.DataAnnotations;

namespace BankingApi.Models;

public sealed class Account
{
    public Guid Id { get; set; }

    public Guid BankId { get; set; }
    public Guid CustomerId { get; set; }

    public Customer? Customer { get; set; }

    [MaxLength(100)]
    public string AccountNumber { get; set; } = string.Empty;

    public decimal Balance { get; set; }

    [MaxLength(3)]
    public string Currency { get; set; } = "NOK";

    public DateTimeOffset CreatedAt { get; set; }

    public List<Transaction> Transactions { get; set; } = new();
}
