using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AspireApp.ApiService.Data;
using AspireApp.ApiService.Models;
using Xunit;

namespace AspireApp.ApiService.Tests;

public class ProjectMembersAuthorizationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProjectMembersAuthorizationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) || d.ServiceType == typeof(ApplicationDbContext)).ToList();
                foreach (var d in descriptors) services.Remove(d);
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("AspireApp_Tests"));
            });
            builder.UseSetting("environment", "Testing");
        });
    }

    [Fact]
    public async Task NonOwnerCannotAddMember()
    {
        var client = _factory.CreateClient();

        // register owner and non-owner
        var regOwner = await client.PostAsJsonAsync("api/auth/register", new { Email = "owner2@test.local", Password = "Owner123!", DisplayName = "Owner2" });
        regOwner.EnsureSuccessStatusCode();
        var regNon = await client.PostAsJsonAsync("api/auth/register", new { Email = "other@test.local", Password = "Other123!", DisplayName = "Other" });
        regNon.EnsureSuccessStatusCode();

        var ownerAuth = await regOwner.Content.ReadFromJsonAsync<AuthResponse>();
        var nonAuth = await regNon.Content.ReadFromJsonAsync<AuthResponse>();

        // owner creates a project
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAuth!.Token);
        var createResp = await client.PostAsJsonAsync("api/projects", new { Name = "AuthProj", Description = "desc" });
        createResp.EnsureSuccessStatusCode();
        var project = await createResp.Content.ReadFromJsonAsync<IntegrationProject>();

        // non-owner attempts to add a member
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", nonAuth!.Token);
        var addResp = await client.PostAsJsonAsync($"api/projects/{project!.Id}/members", new { Email = "someone@test.local", Role = "Member" });
        Assert.True(addResp.StatusCode == System.Net.HttpStatusCode.Forbidden || addResp.StatusCode == System.Net.HttpStatusCode.Unauthorized);
    }

    // reuse helper DTOs
    private record AuthResponse(string Token, string? RefreshToken, string? UserId, string? Email);
    private record IntegrationProject(Guid Id, string Name, string? Description);
}
