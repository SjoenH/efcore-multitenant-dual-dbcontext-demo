using BankingApi.Dtos;
using BankingApi.Infrastructure;
using BankingApi.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Controllers.Admin;

[Route("api/admin/transactions")]
[ApiController]
[Authorize(Policy = AuthPolicies.IsAdmin)]
public sealed class AdminTransactionsController : ControllerBase
{
    private readonly IAdminTransactionsService _tx;

    public AdminTransactionsController(IAdminTransactionsService tx)
    {
        _tx = tx;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetAll()
    {
        return Ok(await _tx.GetAll());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionResponse>> GetById(Guid id)
    {
        var t = await _tx.GetById(id);
        return t is null ? NotFound() : Ok(t);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create(CreateAdminTransactionRequest request)
    {
        var created = await _tx.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _tx.Delete(id);
        return ok ? NoContent() : NotFound();
    }
}
