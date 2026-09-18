using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using AspireApp.ApiService.Models;

namespace AspireApp.ApiService.Data;

public static class SeedData
{
    public static async Task EnsureSeedDataAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var adminEmail = "admin@aspireapp.local";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser { UserName = adminEmail, Email = adminEmail, DisplayName = "Administrator" };
            await userManager.CreateAsync(admin, "Admin123!");
        }
    }
}
