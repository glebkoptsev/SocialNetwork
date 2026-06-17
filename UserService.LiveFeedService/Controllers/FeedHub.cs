using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace UserService.LiveFeedService.Controllers
{
    [Authorize]
    public class FeedHub : Hub
    {
        public async Task Send(string message, string userId)
        {
            var callerId = Context.UserIdentifier;
            if (callerId == userId)
            {
                await Clients.User(userId).SendAsync("Receive", message);
            }
        }
    }
}
