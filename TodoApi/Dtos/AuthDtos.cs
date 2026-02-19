using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos;

public sealed class LoginRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(320)]
    public string Email { get; set; } = string.Empty;
}

public sealed class LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public DateTimeOffset ExpiresAt { get; init; }
    public Guid UserId { get; init; }
}
