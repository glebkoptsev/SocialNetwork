using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class FeedHub : Hub
{
    private static readonly string SystemUserId = "00000000-0000-0000-0000-000000000000";

    public async Task Send(string message, string userId)
    {
        if (Context.UserIdentifier == SystemUserId)
            await Clients.User(userId).SendAsync("Receive", message);
    }
}
