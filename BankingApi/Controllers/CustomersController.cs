using BankingApi.Dtos;
using BankingApi.Infrastructure;
using BankingApi.Models;
using BankingApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BankingApi.Controllers;

[Route("api/customers")]
[ApiController]
[Authorize(Policy = AuthPolicies.Staff)]
public sealed class CustomersController : ControllerBase
{
    private readonly ICustomersService _customers;
    private readonly IAccountsService _accounts;

    public CustomersController(ICustomersService customers, IAccountsService accounts)
    {
        _customers = customers;
        _accounts = accounts;
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

    [HttpGet("{id:guid}/accounts")]
    [Authorize]
    public async Task<ActionResult<IReadOnlyList<AccountResponse>>> GetAccounts(Guid id)
    {
        // Customers may only fetch their own accounts.
        var isCustomer = HttpContext.User.GetRequiredRole() == nameof(Role.Customer);
        if (isCustomer && HttpContext.User.TryGetCustomerIdClaim() != id)
        {
            return Forbid();
        }

        return Ok(await _accounts.GetByCustomerId(id));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(CustomerRequest request)
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
