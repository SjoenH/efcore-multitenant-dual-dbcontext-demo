using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

public sealed class TodoList
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    // Personal list owner (nullable when GroupId is set).
    public Guid? OwnerUserId { get; set; }
    public User? OwnerUser { get; set; }

    // Group-owned list (nullable when OwnerUserId is set).
    public Guid? GroupId { get; set; }
    public Group? Group { get; set; }

    // Optional assignment (useful for group-owned lists).
    public Guid? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public List<Todo> Todos { get; set; } = new();
}
