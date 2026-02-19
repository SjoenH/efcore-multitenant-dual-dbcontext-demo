using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

public class Todo
{
    public long Id { get; set; }

    public Guid ListId { get; set; }
    public TodoList List { get; set; } = null!;

    [MaxLength(200)]
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
}
