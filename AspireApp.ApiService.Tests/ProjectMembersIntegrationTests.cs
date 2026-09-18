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
            // ensure the app uses the Testing environment so Program.cs selects InMemory DB
            builder.ConfigureServices(services =>
            {
                // remove existing DbContext registrations to avoid multiple providers (SQLite + InMemory)
                var descriptors = services.Where(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>) || d.ServiceType == typeof(ApplicationDbContext)).ToList();
                foreach (var d in descriptors) services.Remove(d);

                // register in-memory provider for tests (use fixed name to share DB across app and test scopes)
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase("AspireApp_Tests"));
            });
            // set environment via the TestServer's configuration
            builder.UseSetting("environment", "Testing");
        });
    }

    [Fact]
    public async Task AddAndRemoveMember_Workflow()
    {
        var client = _factory.CreateClient();

        // create owner and member via API register endpoint to ensure same pipeline
        var regOwner = await client.PostAsJsonAsync("api/auth/register", new { Email = "owner@test.local", Password = "Owner123!", DisplayName = "Owner" });
        if (!regOwner.IsSuccessStatusCode)
        {
            var t = await regOwner.Content.ReadAsStringAsync();
            throw new System.Exception($"Register owner failed: {regOwner.StatusCode} - {t}");
        }
        var regMember = await client.PostAsJsonAsync("api/auth/register", new { Email = "member@test.local", Password = "Member123!", DisplayName = "Member" });
        if (!regMember.IsSuccessStatusCode)
        {
            var t = await regMember.Content.ReadAsStringAsync();
            throw new System.Exception($"Register member failed: {regMember.StatusCode} - {t}");
        }

        // use token returned by registration for owner (avoids SignInManager issues in test host)
        var ownerAuth = await regOwner.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(ownerAuth?.Token);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerAuth!.Token);

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
