namespace TodoApi.Infrastructure;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "TodoApi";
    public string Audience { get; init; } = "TodoApi";
    public string SigningKey { get; init; } = string.Empty;
}
