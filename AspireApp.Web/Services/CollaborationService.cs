using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;

namespace AspireApp.Web.Services;

public class CollaborationService : IAsyncDisposable
{
    private readonly HubConnection _hub;
    private readonly ILogger<CollaborationService> _logger;

    public event Func<ApiAuthService.ProjectDto, Task>? OnProjectCreated;
    public event Func<ApiAuthService.ProjectDto, Task>? OnProjectUpdated;
    public event Func<Guid, Task>? OnProjectDeleted;

    public event Func<object, Task>? OnTaskCreated;
    public event Func<object, Task>? OnTaskUpdated;
    public event Func<Guid, Task>? OnTaskDeleted;

    private readonly ApiAuthService _authService;

    public CollaborationService(NavigationManager nav, ILogger<CollaborationService> logger, ApiAuthService authService)
    {
        _logger = logger;
        _authService = authService;
        var hubUrl = new Uri("https+http://apiservice/hubs/collab");
        _hub = new HubConnectionBuilder()
            .WithUrl(hubUrl.ToString(), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult(_authService.JwtToken);
            })
            .WithAutomaticReconnect()
            .Build();

        _hub.On<Guid>("ProjectDeleted", async id => await (OnProjectDeleted?.Invoke(id) ?? Task.CompletedTask));
        _hub.On<ApiAuthService.ProjectDto>("ProjectCreated", async p => await (OnProjectCreated?.Invoke(p) ?? Task.CompletedTask));
        _hub.On<ApiAuthService.ProjectDto>("ProjectUpdated", async p => await (OnProjectUpdated?.Invoke(p) ?? Task.CompletedTask));

        _hub.On<object>("TaskCreated", async obj => await (OnTaskCreated?.Invoke(obj) ?? Task.CompletedTask));
        _hub.On<object>("TaskUpdated", async obj => await (OnTaskUpdated?.Invoke(obj) ?? Task.CompletedTask));
        _hub.On<Guid>("TaskDeleted", async id => await (OnTaskDeleted?.Invoke(id) ?? Task.CompletedTask));
    }

    public async Task StartAsync()
    {
        try
        {
            await _hub.StartAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting hub connection");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hub.DisposeAsync();
    }
}
