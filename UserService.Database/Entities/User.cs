using System.Text.Json.Serialization;

namespace UserService.Database.Entities
{
    public class User
    {
        public Guid User_id { get; set; }
        public string First_name { get; set; } = null!;
        public string Second_name { get; set; } = null!;
        public string Birthdate { get; set; } = null!;
        public string Biography { get; set; } = null!;
        public string City { get; set; } = null!;

        [JsonIgnore]
        public string Password { get; set; } = null!;
        public bool CanPublishMessages { get; set; }
        public string Login { get; set; } = null!;
    }
}
