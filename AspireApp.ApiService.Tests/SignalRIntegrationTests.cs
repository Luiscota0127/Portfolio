using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AspireApp.ApiService.Tests;

public class SignalRIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SignalRIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("environment", "Testing");
        });
    }

    [Fact]
    public async Task HubHandshakeSucceeds()
    {
        var client = _factory.CreateClient();
        var baseUrl = client.BaseAddress!.ToString().TrimEnd('/');

        // register a user to obtain a valid token for authenticated hubs
        var reg = await client.PostAsJsonAsync("api/auth/register", new { Email = "signal@test.local", Password = "Signal123!", DisplayName = "Signal" });
        reg.EnsureSuccessStatusCode();
        var auth = await reg.Content.ReadFromJsonAsync<AuthResponse>();
        var token = auth?.Token ?? string.Empty;

        var hubUrl = new Uri(new Uri(baseUrl), "/hubs/collab");

        var connection = new HubConnectionBuilder()
            .WithUrl(hubUrl.ToString(), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
                options.AccessTokenProvider = () => Task.FromResult(token);
            })
            .Build();

        await connection.StartAsync();
        Assert.Equal(HubConnectionState.Connected, connection.State);

        await connection.StopAsync();
        await connection.DisposeAsync();
    }

    private record AuthResponse(string Token, string? RefreshToken, string? UserId, string? Email);
}
