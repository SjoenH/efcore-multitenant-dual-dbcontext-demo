using BankingApi.Data;
using BankingApi.Dtos;
using BankingApi.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankingApi.Controllers;

[Route("api/my")]
[ApiController]
[Authorize(Policy = AuthPolicies.Customer)]
public sealed class MyAccountsController : ControllerBase
{
    private readonly TenantDbContext _db;

    public MyAccountsController(TenantDbContext db)
    {
        _db = db;
    }

    [HttpGet("accounts")]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> GetMyAccounts()
    {
        var customerId = HttpContext.User.TryGetCustomerIdClaim();
        if (customerId is null)
        {
            return Forbid();
        }

        var accounts = await _db
            .Accounts.AsNoTracking()
            .Where(x => x.CustomerId == customerId)
            .OrderBy(x => x.AccountNumber)
            .Select(AccountResponse.Projection)
            .ToListAsync();

        return Ok(accounts);
    }

    [HttpGet("accounts/{accountId:guid}/transactions")]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetMyTransactions(Guid accountId)
    {
        var customerId = HttpContext.User.TryGetCustomerIdClaim();
        if (customerId is null)
        {
            return Forbid();
        }

        var ownsAccount = await _db
            .Accounts.AsNoTracking()
            .AnyAsync(x => x.Id == accountId && x.CustomerId == customerId);

        if (!ownsAccount)
        {
            return NotFound();
        }

        var txs = await _db
            .Transactions.AsNoTracking()
            .Where(x => x.AccountId == accountId)
            .OrderByDescending(x => x.Timestamp)
            .Select(TransactionResponse.Projection)
            .ToListAsync();

        return Ok(txs);
    }
}
