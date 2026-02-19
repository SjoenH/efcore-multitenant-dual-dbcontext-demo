using System.ComponentModel.DataAnnotations;

namespace BankingApi.Models;

public sealed class Bank
{
    public Guid Id { get; set; }

    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }
}
