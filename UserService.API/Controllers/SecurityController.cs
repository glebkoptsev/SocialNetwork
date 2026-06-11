using Libraries.Web.Common.Security;
using Libraries.Web.Common.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using UserService.API.DTOs;
using UserService.API.Services;

namespace UserService.API.Controllers
{
    [ApiController]
    [Route("api/security")]
    [EnableRateLimiting("LoginPolicy")]
    public class SecurityController(UsersService userService, IOptions<JwtSettings> options) : ControllerBase
    {
        [HttpPost, Route("login")]
        public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await userService.GetUserByLoginAsync(request.Login);
            if (user is null)
                return BadRequest("Bad credentials");

            if (!PasswordHasher.Check(user.Password, request.Password))
                return BadRequest("Bad credentials");

            var claims = new ClaimsIdentity();
            claims.AddClaim(new(ClaimTypes.NameIdentifier, user.User_id.ToString()));
            claims.AddClaim(new(ClaimTypes.Name, user.First_name));
            claims.AddClaim(new("can_publish_messages", user.CanPublishMessages.ToString()));

            var expire = options.Value.TokenExpireSeconds;
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = claims,
                Expires = DateTime.UtcNow.AddSeconds(expire),
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
