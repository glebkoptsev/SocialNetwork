using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserService.API.Services;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/friend")]
    public class FriendController(FriendService friendService, UsersService usersService) : ControllerBase
    {
        private readonly FriendService friendService = friendService;
        private readonly UsersService usersService = usersService;

        [HttpGet, Route("status/{friend_id}"), Authorize]
        public async Task<ActionResult<bool>> GetFriendStatus(string friend_id)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Guid friendGuid;
            if (!Guid.TryParse(friend_id, out friendGuid))
            {
                var friendUser = await usersService.GetUserByLoginAsync(friend_id);
                if (friendUser is null) return NotFound();
                friendGuid = friendUser.User_id;
            }
            var result = await friendService.IsFriendAsync(currentUserId, friendGuid);
            return Ok(result);
        }

        [HttpPut, Route("set/{friend_id}"), Authorize]
        public async Task<IActionResult> AddFriend(string friend_id)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Guid friendGuid;
            if (!Guid.TryParse(friend_id, out friendGuid))
            {
                var friendUser = await usersService.GetUserByLoginAsync(friend_id);
                if (friendUser is null) return NotFound();
                friendGuid = friendUser.User_id;
            }
            if (currentUserId == friendGuid)
            {
                return BadRequest();
            }
            await friendService.AddFriendAsync(currentUserId, friendGuid);
            return Ok();
        }

        [HttpPut, Route("delete/{friend_id}"), Authorize]
        public async Task<IActionResult> DeleteFriend(string friend_id)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            Guid friendGuid;
            if (!Guid.TryParse(friend_id, out friendGuid))
            {
                var friendUser = await usersService.GetUserByLoginAsync(friend_id);
                if (friendUser is null) return NotFound();
                friendGuid = friendUser.User_id;
            }
            if (currentUserId == friendGuid)
            {
                return BadRequest();
            }
            await friendService.DeleteFriendAsync(currentUserId, friendGuid);
            return Ok();
        }
    }
}
