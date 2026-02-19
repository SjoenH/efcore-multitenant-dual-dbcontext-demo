using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

public sealed class Group
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTimeOffset CreatedAt { get; set; }

    public List<GroupMember> Members { get; set; } = new();
}
