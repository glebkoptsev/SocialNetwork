using System.ComponentModel.DataAnnotations;

namespace UserService.API.DTOs
{
    public class UserRegisterRequest
    {
        [Required, StringLength(50, MinimumLength = 3)]
        public string Login { get; set; } = null!;
        [Required, StringLength(30)]
        public string First_name { get; set; } = null!;
        [Required, StringLength(30)]
        public string Second_name { get; set; } = null!;
        [Required, StringLength(11)]
        public string Birthdate { get; set; } = null!;
        [StringLength(1000)]
        public string Biography { get; set; } = null!;
        [StringLength(255)]
        public string City { get; set; } = null!;
        [Required, StringLength(255, MinimumLength = 6)]
        public string Password { get; set; } = null!;
    }
}
