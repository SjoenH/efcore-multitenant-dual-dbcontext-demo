using System.ComponentModel.DataAnnotations;

namespace BankingApi.Dtos;

public sealed class CustomerResponse
{
    public Guid Id { get; init; }
    public Guid BankId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public class CreateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(320)]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }
}

public sealed class CreateAdminCustomerRequest : CreateCustomerRequest
{
    [Required]
    public Guid BankId { get; set; }
}

public sealed class UpdateCustomerRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [EmailAddress]
    [MaxLength(320)]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }
}
