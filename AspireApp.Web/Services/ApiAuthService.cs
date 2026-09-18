using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AspireApp.Web.Services;

public class ApiAuthService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly NavigationManager _nav;

    public string? JwtToken { get; private set; }

    public ApiAuthService(IHttpClientFactory httpFactory, NavigationManager nav)
    {
        _httpFactory = httpFactory;
        _nav = nav;
    }

    private HttpClient CreateClient()
    {
        var client = _httpFactory.CreateClient("ApiClient");
        if (!string.IsNullOrEmpty(JwtToken))
        {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", JwtToken);
        }
        return client;
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("api/auth/login", new { Email = email, Password = password });
        if (!resp.IsSuccessStatusCode) return false;
        var data = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        JwtToken = data?.Token;
        return !string.IsNullOrEmpty(JwtToken);
    }

    public async Task<bool> RegisterAsync(string email, string password, string? displayName)
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync("api/auth/register", new { Email = email, Password = password, DisplayName = displayName });
        return resp.IsSuccessStatusCode;
    }

    public async Task LogoutAsync()
    {
        JwtToken = null;
        _nav.NavigateTo("/login");
    }

    public async Task<ProjectDto[]?> GetProjectsAsync()
    {
        var client = CreateClient();
        var resp = await client.GetAsync("api/projects");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<ProjectDto[]>();
    }

    // DTOs used by the client
    public record AuthResponse(string Token, string? RefreshToken, string? UserId, string? Email);
    public record ProjectDto(Guid Id, string Name, string? Description);
}
