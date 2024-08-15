using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace UserService.API.Settings
{
    public class JwtSettings
    {
        public string Secret { get; set; } = null!;
        public int TokenExpireSeconds { get; set; }
        public string Issuer { get; set; } = null!;
        public string Audience { get; set; } = null!;
        private SigningCredentials? _signingCredentials;
        public SigningCredentials GetSigningCredentials()
        {
            return _signingCredentials ??= new(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(Secret)
                ),
                SecurityAlgorithms.HmacSha256
            );
        }
    }
}