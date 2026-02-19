namespace BankingApi.Infrastructure;

public interface IBankAccessor
{
    Guid? TryGetBankId();
    Guid GetRequiredBankId();
}

public sealed class BankAccessor : IBankAccessor
{
    public const string BankIdHeader = "X-Bank-Id";

    private readonly IHttpContextAccessor _httpContextAccessor;

    public BankAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TryGetBankId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (!httpContext.Request.Headers.TryGetValue(BankIdHeader, out var raw))
        {
            return null;
        }

        return Guid.TryParse(raw.ToString(), out var bankId) ? bankId : null;
    }

    public Guid GetRequiredBankId()
    {
        var bankId = TryGetBankId();
        if (bankId is null)
        {
            throw new UnauthorizedAccessException($"Missing bank id. Provide header '{BankIdHeader}: <guid>'.");
        }

        return bankId.Value;
    }
}
