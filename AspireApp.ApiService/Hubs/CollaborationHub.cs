using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace AspireApp.ApiService.Hubs;

[Authorize]
public class CollaborationHub : Hub
{
    // Hub methods can be extended as needed. Server will broadcast updates from controllers via IHubContext.
}
