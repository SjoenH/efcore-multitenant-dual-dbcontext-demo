using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos;

public sealed class CreateListRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}

public sealed class TodoListResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public Guid? OwnerUserId { get; init; }
    public Guid? GroupId { get; init; }
    public Guid? AssignedUserId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateGroupListRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public Guid? AssignedUserId { get; set; }
}

public sealed class AssignListRequest
{
    public Guid? AssignedUserId { get; set; }
}
