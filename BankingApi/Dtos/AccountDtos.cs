using System.ComponentModel.DataAnnotations;

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
}

public class CreateAccountRequest
{
    [Required]
    public Guid CustomerId { get; set; }
}

public sealed class CreateAdminAccountRequest : CreateAccountRequest
{
    [Required]
    public Guid BankId { get; set; }
}
