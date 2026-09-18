using System.ComponentModel.DataAnnotations;

namespace AspireApp.ApiService.Models;

public enum TaskStatus
{
    Todo,
    InProgress,
    Done
}

public class TaskItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public TaskStatus Status { get; set; } = TaskStatus.Todo;

    public DateTime? DueDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Guid ProjectId { get; set; }

    public Project? Project { get; set; }

    public string? AssignedToId { get; set; }
}
