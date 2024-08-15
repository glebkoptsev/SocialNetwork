using Libraries.NpgsqlService.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UserService.API.DTOs;
using UserService.API.Services;
using UserService.API.Settings;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/security")]
    public class SecurityController(UsersService userService, IOptions<JwtSettings> options) : ControllerBase
    {
        private readonly UsersService userService = userService;
        private readonly IOptions<JwtSettings> options = options;

        [HttpPost, Route("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var user = await userService.GetUserAsync(request.Id);

            if (user is null)
                return BadRequest("Bad credentials");

            var verified = PasswordHasher.Check(user.Password, request.Password);
            if (!verified)
            {
                return BadRequest("Bad credentials");
            }

            var claims = new ClaimsIdentity();
            claims.AddClaim(new(ClaimTypes.NameIdentifier, user.User_id.ToString()));
            claims.AddClaim(new(ClaimTypes.Name, user.First_name));

            var expire = options.Value.TokenExpireSeconds;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Expires = DateTime.UtcNow.AddSeconds(options.Value.TokenExpireSeconds),
                SigningCredentials = options.Value.GetSigningCredentials(),
                Audience = options.Value.Audience,
                Issuer = options.Value.Issuer
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            var access_token = tokenHandler.WriteToken(token);

            return Ok(new LoginResponse { Access_token = access_token, ExpiresIn = expire });
        }
    }
}
