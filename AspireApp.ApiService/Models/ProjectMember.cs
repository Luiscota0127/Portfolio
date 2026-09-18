using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AspireApp.ApiService.Models;

public class ProjectMember
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [ForeignKey("Project")]
    public Guid ProjectId { get; set; }
    public Project? Project { get; set; }

    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }

    public string Role { get; set; } = "Member";
}
