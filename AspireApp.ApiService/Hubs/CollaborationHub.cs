using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;

namespace AspireApp.ApiService.Hubs;

[Authorize]
public class CollaborationHub : Hub
{
    // Hub methods can be extended as needed. Server will broadcast updates from controllers via IHubContext.

    // Allow clients to join/leave project-specific groups to receive targeted notifications
    public async Task JoinProjectGroup(Guid projectId)
    {
        var groupName = GetProjectGroupName(projectId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveProjectGroup(Guid projectId)
    {
        var groupName = GetProjectGroupName(projectId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    private static string GetProjectGroupName(Guid projectId) => $"project-{projectId}";
}
