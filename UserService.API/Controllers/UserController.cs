using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using UserService.API.DTOs;
using UserService.API.Services;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController(UsersService userService) : ControllerBase
    {
        [HttpPost, Route("register")]
        public async Task<ActionResult<UserRegisterResponse>> Register(UserRegisterRequest request)
        {
            return Ok(await userService.RegisterUserAsync(request));
        }

        [HttpGet, Route("get/{id}"), Authorize]
        public async Task<ActionResult<UserResponse>> GetUser(string id)
        {
            var guid = await userService.ResolveUserIdAsync(id);
            if (guid == Guid.Empty) return NotFound();
            var user = await userService.GetUserResponseAsync(guid);
            if (user is null) return NotFound();
            return Ok(user);
        }

        [HttpGet, Route("search"), Authorize]
        public async Task<ActionResult<List<UserResponse>>> SearchUser(
            [Required] string query, int offset = 0, int limit = 20)
        {
            return Ok(await userService.SearchUserResponseAsync(query, offset, limit));
        }

        [HttpPut, Route("profile"), Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var currentUserId = Guid.Parse(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);
            await userService.UpdateProfileAsync(currentUserId, request);
            return Ok();
        }
    }
}