using System.ComponentModel.DataAnnotations;

namespace UserService.API.DTOs
{
    public class LoginRequest
    {
        [Required]
        public string Login { get; set; } = null!;
        [Required]
        public string Password { get; set; } = null!;
    }
}
