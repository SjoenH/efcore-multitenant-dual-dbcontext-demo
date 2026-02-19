using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Dtos;
using TodoApi.Services;

namespace TodoApi.Controllers;

[Route("api/admin/lists")]
[ApiController]
[Authorize(Policy = "IsAdmin")]
public sealed class AdminListsController : ControllerBase
{
    private readonly IAdminListService _lists;

    public AdminListsController(IAdminListService lists)
    {
        _lists = lists;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TodoListResponse>>> GetAll()
    {
        return Ok(await _lists.GetAll());
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoListResponse>> GetById(Guid id)
    {
        var list = await _lists.GetById(id);
        return list is null ? NotFound() : Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<TodoListResponse>> Create(CreateAdminListRequest request)
    {
        var created = await _lists.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateListRequest request)
    {
        var ok = await _lists.Update(id, request);
        return ok ? NoContent() : NotFound();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var ok = await _lists.Delete(id);
        return ok ? NoContent() : NotFound();
    }
}
