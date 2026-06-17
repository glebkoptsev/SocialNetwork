using System.ComponentModel.DataAnnotations;

namespace UserService.API.DTOs
{
    public class UpdateProfileRequest
    {
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
        [Range(0, 1)]
        public int Who_can_message { get; set; } = 0;
    }
}
