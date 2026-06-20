using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace UserService.Database.Entities
{
    public class Post
    {
        public Guid Post_id { get; set; }
        public Guid User_id { get; set; }

        [Column("post")]
        [JsonPropertyName("post")]
        public string Text { get; set; } = null!;

        [JsonPropertyName("creation_datetime")]
        public DateTime Creation_datetime { get; set; }

        [JsonPropertyName("authorFirstName")]
        public string? AuthorFirstName { get; set; }

        [JsonPropertyName("authorSecondName")]
        public string? AuthorSecondName { get; set; }

        [JsonPropertyName("like_count")]
        public int LikeCount { get; set; }
    }
}
