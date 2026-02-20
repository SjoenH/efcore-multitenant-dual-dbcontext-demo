namespace BankingApi.Infrastructure;

/// <summary>Custom JWT claim type names used when issuing and reading tokens.</summary>
public static class AppClaimTypes
{
    public const string BankId = "BankId";
    public const string CustomerId = "CustomerId";
    public const string IsAdmin = "IsAdmin";
}

/// <summary>Authorization policy names registered in Program.cs.</summary>
public static class AuthPolicies
{
    public const string IsAdmin = "IsAdmin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";
}
