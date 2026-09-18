using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using AspireApp.ApiService.Data;
using AspireApp.ApiService.Models;
using Microsoft.AspNetCore.Identity;
using Xunit;

namespace AspireApp.ApiService.Tests;

public class ProjectMembersIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ProjectMembersIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // remove existing DbContext registrations to avoid multiple providers (SQLite + InMemory)
                var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) || d.ServiceType == typeof(ApplicationDbContext)).ToList();
                foreach (var d in descriptors) services.Remove(d);

                // register in-memory provider for tests
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("TestDb" + Guid.NewGuid()));
            });
        });
    }

    [Fact]
    public async Task AddAndRemoveMember_Workflow()
    {
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        // create owner and member users
        var owner = new ApplicationUser { UserName = "owner@test.local", Email = "owner@test.local" };
        await userManager.CreateAsync(owner, "Owner123!");
        var member = new ApplicationUser { UserName = "member@test.local", Email = "member@test.local" };
        await userManager.CreateAsync(member, "Member123!");

        var client = _factory.CreateClient();

        // login owner
        var loginResp = await client.PostAsJsonAsync("api/auth/login", new { Email = "owner@test.local", Password = "Owner123!" });
        loginResp.EnsureSuccessStatusCode();
        var auth = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth?.Token);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth!.Token);

        // create project
        var createResp = await client.PostAsJsonAsync("api/projects", new { Name = "IntegrationProject", Description = "desc" });
        createResp.EnsureSuccessStatusCode();
        var project = await createResp.Content.ReadFromJsonAsync<IntegrationProject>();
        Assert.NotNull(project);

        // add member by email
        var addResp = await client.PostAsJsonAsync($"api/projects/{project.Id}/members", new { Email = "member@test.local", Role = "Member" });
        addResp.EnsureSuccessStatusCode();

        // get members
        var membersResp = await client.GetAsync($"api/projects/{project.Id}/members");
        membersResp.EnsureSuccessStatusCode();
        var members = await membersResp.Content.ReadFromJsonAsync<ProjectMemberDto[]>();
        Assert.Contains(members, m => m.Email == "member@test.local");

        var mem = members!.First(m => m.Email == "member@test.local");

        // remove member
        var delResp = await client.DeleteAsync($"api/projects/{project.Id}/members/{mem.Id}");
        delResp.EnsureSuccessStatusCode();

        var membersAfter = await (await client.GetAsync($"api/projects/{project.Id}/members")).Content.ReadFromJsonAsync<ProjectMemberDto[]>();
        Assert.DoesNotContain(membersAfter, m => m.Email == "member@test.local");
    }

    // helper DTOs for tests
    private record AuthResponse(string Token, string? RefreshToken, string? UserId, string? Email);
    private record IntegrationProject(Guid Id, string Name, string? Description);
    private record ProjectMemberDto(Guid Id, Guid ProjectId, string? UserId, string? Email, string Role);
}
