using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Components;

namespace AspireApp.Web.Services;

public class CollaborationService : IAsyncDisposable
{
        public event Func<ConnectionState, Task>? OnConnectionChanged;

    private readonly HubConnection _hub;
    private readonly ILogger<CollaborationService> _logger;

    public event Func<ApiAuthService.ProjectDto, Task>? OnProjectCreated;
    public event Func<ApiAuthService.ProjectDto, Task>? OnProjectUpdated;
    public event Func<Guid, Task>? OnProjectDeleted;

    public event Func<object, Task>? OnTaskCreated;
    public event Func<object, Task>? OnTaskUpdated;
    public event Func<Guid, Task>? OnTaskDeleted;

        private readonly ApiAuthService _authService;

        public enum ConnectionState { Connected, Reconnecting, Disconnected }

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
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
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
            // attempt refresh before starting connection
            await _authService.TryRefreshTokenAsync();

            // start connection and resubscribe to default groups if necessary
                await _hub.StartAsync();
                await NotifyConnectionChanged(ConnectionState.Connected);
            // TODO: if the app uses group subscriptions, call server methods to rejoin groups here
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting hub connection");
                await NotifyConnectionChanged(ConnectionState.Disconnected);
        }
    }

        private async Task NotifyConnectionChanged(ConnectionState state)
        {
            try
            {
                if (OnConnectionChanged != null) await OnConnectionChanged.Invoke(state);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error delivering connection state");
            }
        }

    public async ValueTask DisposeAsync()
    {
        await _hub.DisposeAsync();
    }
}
