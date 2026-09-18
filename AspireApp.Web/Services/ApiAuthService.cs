using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Text.Json;

namespace AspireApp.Web.Services;

public class ApiAuthService
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly NavigationManager _nav;
    private readonly IJSRuntime _jsRuntime;

    public string? JwtToken { get; private set; }
    public string? RefreshToken { get; private set; }
    private readonly System.Threading.SemaphoreSlim _refreshLock = new(1, 1);

    public ApiAuthService(IHttpClientFactory httpFactory, NavigationManager nav, IJSRuntime jsRuntime)
    {
        _httpFactory = httpFactory;
        _nav = nav;
        _jsRuntime = jsRuntime;
    }

    public async Task InitializeAsync()
    {
        try
        {
            var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "aspire_auth");
            if (!string.IsNullOrEmpty(json))
            {
                var stored = JsonSerializer.Deserialize<AuthStorage>(json);
                if (stored != null)
                {
                    JwtToken = stored.Token;
                    RefreshToken = stored.RefreshToken;
                }
            }
        }
        catch
        {
            // ignore storage errors
        }
    }

    private HttpClient CreateClient()
    {
        return _httpFactory.CreateClient("ApiClient");
    }

    public async Task<bool> LoginAsync(string email, string password)
    {
        var client = _httpFactory.CreateClient("ApiClientNoAuth");
        var resp = await client.PostAsJsonAsync("api/auth/login", new { Email = email, Password = password });
        if (!resp.IsSuccessStatusCode) return false;
        var data = await resp.Content.ReadFromJsonAsync<AuthResponse>();
        JwtToken = data?.Token;
        RefreshToken = data?.RefreshToken;
        // persist tokens in protected session storage
        var storage = System.Text.Json.JsonSerializer.Serialize(new AuthStorage { Token = JwtToken, RefreshToken = RefreshToken });
        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "aspire_auth", storage);
        return !string.IsNullOrEmpty(JwtToken);
    }

    public async Task<bool> RegisterAsync(string email, string password, string? displayName)
    {
        var client = _httpFactory.CreateClient("ApiClientNoAuth");
        var resp = await client.PostAsJsonAsync("api/auth/register", new { Email = email, Password = password, DisplayName = displayName });
        return resp.IsSuccessStatusCode;
    }

    public async Task LogoutAsync()
    {
        JwtToken = null;
        RefreshToken = null;
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", "aspire_auth");
        _nav.NavigateTo("/login");
    }

    public async Task<ProjectDto[]?> GetProjectsAsync()
    {
        var client = CreateClient();
        var resp = await client.GetAsync("api/projects");
        if (!resp.IsSuccessStatusCode) return null;
        var result = await resp.Content.ReadFromJsonAsync<ProjectDto[]>();
        return result;
    }

    public async Task<ProjectMemberDto[]?> GetProjectMembersAsync(Guid projectId)
    {
        var client = CreateClient();
        var resp = await client.GetAsync($"api/projects/{projectId}/members");
        if (!resp.IsSuccessStatusCode) return null;
        return await resp.Content.ReadFromJsonAsync<ProjectMemberDto[]>();
    }

    public async Task<bool> AddProjectMemberAsync(Guid projectId, string email, string? role)
    {
        var client = CreateClient();
        var resp = await client.PostAsJsonAsync($"api/projects/{projectId}/members", new { Email = email, Role = role });
        return resp.IsSuccessStatusCode;
    }

    public async Task<bool> RemoveProjectMemberAsync(Guid projectId, Guid memberId)
    {
        var client = CreateClient();
        var resp = await client.DeleteAsync($"api/projects/{projectId}/members/{memberId}");
        return resp.IsSuccessStatusCode;
    }

    public record ProjectMemberDto(Guid Id, Guid ProjectId, string? UserId, string? Email, string Role);

    public async Task<bool> TryRefreshTokenAsync()
    {
        if (string.IsNullOrEmpty(RefreshToken)) return false;

        await _refreshLock.WaitAsync();
        try
        {
            // If another request already refreshed the token, skip calling API again
            if (!string.IsNullOrEmpty(JwtToken))
                return true;

            var client = _httpFactory.CreateClient("ApiClientNoAuth");
            var resp = await client.PostAsJsonAsync("api/auth/refresh", new { RefreshToken = RefreshToken });
            if (!resp.IsSuccessStatusCode) return false;
            var data = await resp.Content.ReadFromJsonAsync<AuthResponse>();
            if (data == null || string.IsNullOrEmpty(data.Token)) return false;
            JwtToken = data.Token;
            RefreshToken = data.RefreshToken;
            var json = System.Text.Json.JsonSerializer.Serialize(new AuthStorage { Token = JwtToken, RefreshToken = RefreshToken });
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", "aspire_auth", json);
            return true;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    // DTOs used by the client
    public record AuthResponse(string Token, string? RefreshToken, string? UserId, string? Email);
    public record ProjectDto(Guid Id, string Name, string? Description);
}
