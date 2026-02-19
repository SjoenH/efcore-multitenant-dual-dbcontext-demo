using BankingApi.Dtos;
using BankingApi.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Controllers.Admin;

[Route("api/admin/accounts")]
[ApiController]
[Authorize(Policy = "IsAdmin")]
public sealed class AdminAccountsController : ControllerBase
{
    private readonly IAdminAccountsService _accounts;

    public AdminAccountsController(IAdminAccountsService accounts)
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
    public async Task<ActionResult<AccountResponse>> Create(CreateAdminAccountRequest request)
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
