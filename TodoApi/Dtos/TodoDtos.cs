using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos;

public sealed class CreateTodoRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsComplete { get; set; }
}

public sealed class UpdateTodoRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public bool IsComplete { get; set; }
}

public sealed class TodoResponse
{
    public long Id { get; init; }
    public string? Name { get; init; }
    public bool IsComplete { get; init; }
}
