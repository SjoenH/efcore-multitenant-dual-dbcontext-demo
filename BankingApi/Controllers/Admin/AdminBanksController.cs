using BankingApi.Dtos;
using BankingApi.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Controllers.Admin;

[Route("api/admin/banks")]
[ApiController]
[Authorize(Policy = "IsAdmin")]
public sealed class AdminBanksController : ControllerBase
{
    private readonly IAdminBanksService _banks;

    public AdminBanksController(IAdminBanksService banks)
    {
        _banks = banks;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<BankResponse>>> GetAll()
    {
        return Ok(await _banks.GetAll());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BankResponse>> GetById(Guid id)
    {
        var bank = await _banks.GetById(id);
        return bank is null ? NotFound() : Ok(bank);
    }

    [HttpPost]
    public async Task<ActionResult<BankResponse>> Create(CreateBankRequest request)
    {
        var created = await _banks.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateBankRequest request)
    {
        var ok = await _banks.Update(id, request);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _banks.Delete(id);
        return ok ? NoContent() : NotFound();
    }
}
