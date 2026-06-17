using System.ComponentModel.DataAnnotations;

namespace DialogService.API.DTOs
{
    public class CreateChatRequest
    {
        [Required, StringLength(100)]
        public string Name { get; set; } = null!;
        [Required]
        public List<Guid> Users_ids { get; set; } = [];
    }
}
