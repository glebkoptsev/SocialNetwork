using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
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
        public async Task<ActionResult<List<UserResponse>>> SearchUser([Required] string first_name, string? second_name)
        {
            return Ok(await userService.SearchUserResponseAsync(first_name, second_name ?? ""));
        }
    }
}