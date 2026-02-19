using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

public sealed class TodoList
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public Guid TenantId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
