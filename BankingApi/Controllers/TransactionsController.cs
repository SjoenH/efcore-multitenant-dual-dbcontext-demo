using BankingApi.Dtos;
using BankingApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Controllers;

[Route("api/transactions")]
[ApiController]
[Authorize(Policy = "Staff")]
public sealed class TransactionsController : ControllerBase
{
    private readonly ITransactionsService _transactions;

    public TransactionsController(ITransactionsService transactions)
    {
        _transactions = transactions;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TransactionResponse>>> GetAll()
    {
        return Ok(await _transactions.GetAll());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TransactionResponse>> GetById(Guid id)
    {
        var tx = await _transactions.GetById(id);
        return tx is null ? NotFound() : Ok(tx);
    }

    [HttpPost]
    public async Task<ActionResult<TransactionResponse>> Create(CreateTransactionRequest request)
    {
        var created = await _transactions.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _transactions.Delete(id);
        return ok ? NoContent() : NotFound();
    }
}
