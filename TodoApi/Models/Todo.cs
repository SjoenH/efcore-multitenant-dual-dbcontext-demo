using System.ComponentModel.DataAnnotations;

namespace TodoApi.Models;

public class Todo
{
    public long Id { get; set; }

    [MaxLength(200)]
    public string? Name { get; set; }
    public bool IsComplete { get; set; }
}
