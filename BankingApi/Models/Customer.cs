using System.ComponentModel.DataAnnotations;

namespace BankingApi.Models;

public sealed class Customer
{
    public Guid Id { get; set; }

    public Guid BankId { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(320)]
    public string? Email { get; set; }

    [MaxLength(40)]
    public string? Phone { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<Account> Accounts { get; set; } = new();
}
