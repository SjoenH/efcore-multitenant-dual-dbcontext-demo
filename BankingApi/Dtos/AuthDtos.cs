using System.ComponentModel.DataAnnotations;

namespace BankingApi.Dtos;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;
}

public sealed class SeededLoginsResponse
{
    public string AdminEmail { get; init; } = string.Empty;
    public string StaffBankAEmail { get; init; } = string.Empty;
    public string StaffBankBEmail { get; init; } = string.Empty;
    public string CustomerAEmail { get; init; } = string.Empty;
    public string CustomerBEmail { get; init; } = string.Empty;

    public Guid? BankAId { get; init; }
    public Guid? BankBId { get; init; }
    public Guid? CustomerAId { get; init; }
    public Guid? CustomerBId { get; init; }
}

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public DateTimeOffset ExpiresAt { get; init; }
    public Guid UserId { get; init; }

    public string Role { get; init; } = string.Empty;
    public Guid? BankId { get; init; }
    public Guid? CustomerId { get; init; }
}
