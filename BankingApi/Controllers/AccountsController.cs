using BankingApi.Dtos;
using BankingApi.Infrastructure;
using BankingApi.Models;
using BankingApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Controllers;

[Route("api/accounts")]
[ApiController]
[Authorize(Policy = AuthPolicies.Staff)]
public sealed class AccountsController : ControllerBase
{
    private readonly IAccountsService _accounts;

    public AccountsController(IAccountsService accounts)
    {
        _accounts = accounts;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> GetAll()
    {
        return Ok(await _accounts.GetAll());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<AccountResponse>> GetById(Guid id)
    {
        var account = await _accounts.GetById(id);
        return account is null ? NotFound() : Ok(account);
    }

    [HttpGet("me")]
    [Authorize(Policy = AuthPolicies.Customer)]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> GetMyAccounts()
    {
        var customerId = HttpContext.User.TryGetCustomerIdClaim();
        if (customerId is null)
        {
            return Forbid();
        }

        return Ok(await _accounts.GetByCustomerId(customerId.Value));
    }

    [HttpGet("{id:guid}/transactions")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetTransactions(Guid id)
    {
        // Customers may only see transactions for accounts they own.
        // Staff may see any account's transactions within the bank.
        var isCustomer = HttpContext.User.GetRequiredRole() == nameof(Role.Customer);
        var ownerCustomerId = isCustomer ? HttpContext.User.TryGetCustomerIdClaim() : null;

        var txs = await _accounts.GetTransactionsByAccountId(id, ownerCustomerId);
        return txs is null ? NotFound() : Ok(txs);
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Create(AccountRequest request)
    {
        var created = await _accounts.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _accounts.Delete(id);
        return ok ? NoContent() : NotFound();
    }
}
