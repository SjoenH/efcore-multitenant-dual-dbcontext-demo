namespace BankingApi.Infrastructure;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; init; } = "BankingApi";
    public string Audience { get; init; } = "BankingApi";
    public string SigningKey { get; init; } = string.Empty;
}
