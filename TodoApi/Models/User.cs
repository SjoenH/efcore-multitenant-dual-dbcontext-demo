using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

public sealed class User
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? DisplayName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
