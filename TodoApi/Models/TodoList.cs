using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

public sealed class TodoList
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public Guid OwnerUserId { get; set; }
    public User OwnerUser { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; }

    public List<Todo> Todos { get; set; } = new();
}
