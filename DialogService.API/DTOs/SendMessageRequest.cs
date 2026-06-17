using System.ComponentModel.DataAnnotations;

namespace DialogService.API.DTOs
{
    public class SendMessageRequest
    {
        [Required, StringLength(5000, MinimumLength = 1)]
        public string Message { get; set; } = null!;
    }
}
