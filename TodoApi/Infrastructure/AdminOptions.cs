namespace TodoApi.Infrastructure;

public sealed class AdminOptions
{
    public const string SectionName = "Admin";

    public string[] Emails { get; init; } = Array.Empty<string>();
}
