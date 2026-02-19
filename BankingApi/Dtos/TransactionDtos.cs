using System.ComponentModel.DataAnnotations;

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
}

public class CreateTransactionRequest
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

public sealed class CreateAdminTransactionRequest : CreateTransactionRequest
{
    [Required]
    public Guid BankId { get; set; }
}
