using BankingApi.Dtos;
using BankingApi.Infrastructure;
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
