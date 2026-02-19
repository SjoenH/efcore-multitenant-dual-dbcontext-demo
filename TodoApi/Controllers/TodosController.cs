using Microsoft.AspNetCore.Mvc;
using TodoApi.Dtos;
using TodoApi.Models;
using TodoApi.Services;

namespace TodoApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class TodosController : ControllerBase
{
    private readonly ITodoService _todoService;

    public TodosController(ITodoService todoService)
    {
        _todoService = todoService;
    }

    // GET: api/Todos
    [HttpGet]
    public async Task<ActionResult<IEnumerable<TodoResponse>>> GetTodos([FromQuery] bool? isComplete = null)
    {
        return Ok(await _todoService.GetTodos(isComplete));
    }

    // GET: api/Todos/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoResponse>> GetTodo(long id)
    {
        var todo = await _todoService.GetTodo(id);

        if (todo == null)
        {
            return NotFound();
        }

        return todo;
    }

    // PUT: api/Todos/5
    [HttpPut("{id}")]
    public async Task<IActionResult> PutTodo(long id, UpdateTodoRequest request)
    {
        var result = await _todoService.UpdateTodo(id, request);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }

    // POST: api/Todos
    [HttpPost]
    public async Task<ActionResult<TodoResponse>> PostTodo(CreateTodoRequest request)
    {
        var createdTodo = await _todoService.CreateTodo(request);
        return CreatedAtAction(nameof(GetTodo), new { id = createdTodo.Id }, createdTodo);
    }

    // DELETE: api/Todos/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteTodo(long id)
    {
        var result = await _todoService.DeleteTodo(id);

        if (!result)
        {
            return NotFound();
        }

        return NoContent();
    }
}
