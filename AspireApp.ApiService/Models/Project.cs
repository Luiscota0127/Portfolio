using System.ComponentModel.DataAnnotations;

namespace AspireApp.ApiService.Models;

public class Project
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? OwnerId { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
}
