using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using TodoApi.Dtos;
using TodoApi.Infrastructure;
using TodoApi.Services;

namespace TodoApi.Controllers;

[Route("api/lists/{listId:guid}/todos")]
[ApiController]
[Authorize]
public class TodosController : ControllerBase
{
    private readonly ITodoService _todoService;
    private readonly ICurrentUserAccessor _currentUser;

    public TodosController(ITodoService todoService, ICurrentUserAccessor currentUser)
    {
        _todoService = todoService;
        _currentUser = currentUser;
    }

    // GET: api/lists/{listId}/todos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoResponse>>> GetTodos(Guid listId, [FromQuery] bool? isComplete = null)
    {
        var userId = _currentUser.GetRequiredUserId();
        return Ok(await _todoService.GetTodos(userId, listId, isComplete));
    }

    // GET: api/lists/{listId}/todos/5
    [HttpGet("{id:long}")]
    public async Task<ActionResult<TodoResponse>> GetTodo(Guid listId, long id)
    {
        var userId = _currentUser.GetRequiredUserId();
        var todo = await _todoService.GetTodo(userId, listId, id);

        if (todo == null)
        {
            return NotFound();
        }

        return todo;
    }

    // PUT: api/lists/{listId}/todos/5
    [HttpPut("{id:long}")]
    public async Task<IActionResult> PutTodo(Guid listId, long id, UpdateTodoRequest request)
    {
        var userId = _currentUser.GetRequiredUserId();
        var result = await _todoService.UpdateTodo(userId, listId, id, request);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    // POST: api/lists/{listId}/todos
    [HttpPost]
    public async Task<ActionResult<TodoResponse>> PostTodo(Guid listId, CreateTodoRequest request)
    {
        var userId = _currentUser.GetRequiredUserId();
        var createdTodo = await _todoService.CreateTodo(userId, listId, request);
        return CreatedAtAction(nameof(GetTodo), new { listId, id = createdTodo.Id }, createdTodo);
    }

    // DELETE: api/lists/{listId}/todos/5
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteTodo(Guid listId, long id)
    {
        var userId = _currentUser.GetRequiredUserId();
        var result = await _todoService.DeleteTodo(userId, listId, id);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
