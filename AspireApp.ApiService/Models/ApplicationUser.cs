using Microsoft.AspNetCore.Identity;

namespace AspireApp.ApiService.Models;

public class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
