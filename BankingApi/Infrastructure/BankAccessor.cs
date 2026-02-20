namespace BankingApi.Infrastructure;

public interface IBankAccessor
{
    Guid? TryGetBankId();
    Guid GetRequiredBankId();
}

public sealed class BankAccessor : IBankAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public BankAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TryGetBankId()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.TryGetBankIdClaim();
    }

    public Guid GetRequiredBankId()
    {
        var bankId = TryGetBankId();
        if (bankId is null)
        {
            throw new UnauthorizedAccessException("Missing BankId claim in token.");
        }

        return bankId.Value;
    }
}
