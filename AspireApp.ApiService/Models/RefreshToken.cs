using System.ComponentModel.DataAnnotations;

namespace AspireApp.ApiService.Models;

public class RefreshToken
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Token { get; set; } = null!;

    public DateTime Expires { get; set; }

    public bool Revoked { get; set; }

    public string? UserId { get; set; }

    public ApplicationUser? User { get; set; }
}
