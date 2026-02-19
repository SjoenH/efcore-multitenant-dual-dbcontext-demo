using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TodoApi.Dtos;
using TodoApi.Infrastructure;
using TodoApi.Services;

namespace TodoApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class ListsController : ControllerBase
{
    private readonly IListService _lists;
    private readonly ICurrentUserAccessor _currentUser;

    public ListsController(IListService lists, ICurrentUserAccessor currentUser)
    {
        _lists = lists;
        _currentUser = currentUser;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TodoListResponse>>> GetMyLists()
    {
        var userId = _currentUser.GetRequiredUserId();
        return Ok(await _lists.GetAccessibleLists(userId));
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TodoListResponse>> GetMyList(Guid id)
    {
        var userId = _currentUser.GetRequiredUserId();
        var list = await _lists.GetAccessibleList(userId, id);
        return list is null ? NotFound() : Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<TodoListResponse>> CreateList(CreateListRequest request)
    {
        var userId = _currentUser.GetRequiredUserId();
        var list = await _lists.CreatePersonalList(userId, request);
        return CreatedAtAction(nameof(GetMyList), new { id = list.Id }, list);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteList(Guid id)
    {
        var userId = _currentUser.GetRequiredUserId();
        var ok = await _lists.DeletePersonalList(userId, id);
        return ok ? NoContent() : NotFound();
    }
}
