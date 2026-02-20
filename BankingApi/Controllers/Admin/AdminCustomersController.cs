using BankingApi.Dtos;
using BankingApi.Infrastructure;
using BankingApi.Services.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Controllers.Admin;

[Route("api/admin/customers")]
[ApiController]
[Authorize(Policy = AuthPolicies.IsAdmin)]
public sealed class AdminCustomersController : ControllerBase
{
    private readonly IAdminCustomersService _customers;

    public AdminCustomersController(IAdminCustomersService customers)
    {
        _customers = customers;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetAll()
    {
        return Ok(await _customers.GetAll());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CustomerResponse>> GetById(Guid id)
    {
        var customer = await _customers.GetById(id);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(CreateAdminCustomerRequest request)
    {
        var created = await _customers.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateCustomerRequest request)
    {
        var ok = await _customers.Update(id, request);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _customers.Delete(id);
        return ok ? NoContent() : NotFound();
    }
}
