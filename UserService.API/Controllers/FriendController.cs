using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserService.API.DTOs;
using UserService.API.Services;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/friend")]
    public class FriendController(FriendService friendService, UsersService usersService) : ControllerBase
    {
        [HttpGet, Route("subscriptions"), Authorize]
        public async Task<ActionResult<List<UserResponse>>> GetSubscriptions(string? user_id)
        {
            Guid targetUserId;
            if (user_id is null)
                targetUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            else
            {
                targetUserId = await usersService.ResolveUserIdAsync(user_id);
                if (targetUserId == Guid.Empty) return NotFound();
            }
            return Ok(await usersService.GetSubscriptionsResponseAsync(targetUserId));
        }

        [HttpGet, Route("followers"), Authorize]
        public async Task<ActionResult<List<UserResponse>>> GetFollowers(string? user_id)
        {
            Guid targetUserId;
            if (user_id is null)
                targetUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            else
            {
                targetUserId = await usersService.ResolveUserIdAsync(user_id);
                if (targetUserId == Guid.Empty) return NotFound();
            }
            return Ok(await usersService.GetFollowersResponseAsync(targetUserId));
        }

        [HttpGet, Route("status/{friend_id}"), Authorize]
        public async Task<ActionResult<bool>> GetSubscriptionStatus(string friend_id)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            var friendGuid = await usersService.ResolveUserIdAsync(friend_id);
            if (friendGuid == Guid.Empty) return NotFound();
            return Ok(await friendService.IsFriendAsync(currentUserId, friendGuid));
        }

        [HttpPut, Route("set/{friend_id}"), Authorize]
        public async Task<IActionResult> Subscribe(string friend_id)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            var friendGuid = await usersService.ResolveUserIdAsync(friend_id);
            if (friendGuid == Guid.Empty) return NotFound();
            if (currentUserId == friendGuid) return BadRequest();
            await friendService.AddFriendAsync(currentUserId, friendGuid);
            return Ok();
        }

        [HttpPut, Route("delete/{friend_id}"), Authorize]
        public async Task<IActionResult> Unsubscribe(string friend_id)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            var friendGuid = await usersService.ResolveUserIdAsync(friend_id);
            if (friendGuid == Guid.Empty) return NotFound();
            if (currentUserId == friendGuid) return BadRequest();
            await friendService.DeleteFriendAsync(currentUserId, friendGuid);
            return Ok();
        }

        // backward compat
        [HttpGet, Route("list"), Authorize]
        public async Task<ActionResult<List<UserResponse>>> GetList(string? user_id) => await GetSubscriptions(user_id);
    }
}