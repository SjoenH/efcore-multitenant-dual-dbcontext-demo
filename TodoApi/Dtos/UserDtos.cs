using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos;

public sealed class CreateUserRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? DisplayName { get; set; }
}

public sealed class UserResponse
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
