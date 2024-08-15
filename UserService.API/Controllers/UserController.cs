using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using UserService.API.DTOs;
using UserService.API.Services;
using UserService.Database.Entities;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/user")]
    public class UserController(UsersService userService) : ControllerBase
    {
        private readonly UsersService userService = userService;

        [HttpPost, Route("register")]
        public async Task<ActionResult<UserRegisterResponse>> Register(UserRegisterRequest request)
        {
            return Ok(await userService.RegisterUserAsync(request));
        }

        [HttpGet, Route("get/{id}")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            var user = await userService.GetUserAsync(id);
            if (user is null) return NotFound();
            return Ok(user);
        }

        [HttpGet, Route("search")]
        public async Task<ActionResult<List<User>>> SearchUser([Required] string first_name, [Required] string second_name)
        {
            var users = await userService.SearchUserAsync(first_name, second_name);
            if (users is null) return NotFound();
            return Ok(users);
        }
    }
}
