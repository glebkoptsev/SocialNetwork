using System.ComponentModel.DataAnnotations;

namespace UserService.API.DTOs
{
    public class AddPostRequest
    {
        [Required, StringLength(2000, MinimumLength = 1)]
        public string Text { get; set; } = null!;
    }
}
