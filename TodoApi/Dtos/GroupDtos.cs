using System.ComponentModel.DataAnnotations;

namespace TodoApi.Dtos;

public sealed class CreateGroupRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}

public sealed class AddGroupMemberRequest
{
    [Required]
    public Guid UserId { get; set; }
}

public sealed class GroupResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public sealed class CreateGroupResponse
{
    public GroupResponse Group { get; init; } = new();
    public GroupMemberResponse Me { get; init; } = new();
}

public sealed class GroupMemberResponse
{
    public Guid GroupId { get; init; }
    public Guid UserId { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
}
