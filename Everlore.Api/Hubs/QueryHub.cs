using Everlore.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Everlore.Api.Hubs;

[Authorize]
public class QueryHub(ICurrentUser currentUser) : Hub<IQueryHubClient>
{
    public override async Task OnConnectedAsync()
    {
        var tenantId = currentUser.TenantId;
        if (tenantId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId.Value}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenantId.Value}");
        }

        await base.OnDisconnectedAsync(exception);
    }
}
