using System.ComponentModel.DataAnnotations;

namespace BankingApi.Models;

public sealed class User
{
    public Guid Id { get; set; }

    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    public Role Role { get; set; }

    public Guid? BankId { get; set; }
    public Guid? CustomerId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
