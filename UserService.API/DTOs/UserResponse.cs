namespace UserService.API.DTOs
{
    public class UserResponse
    {
        public Guid User_id { get; set; }
        public string First_name { get; set; } = null!;
        public string Second_name { get; set; } = null!;
        public string Birthdate { get; set; } = null!;
        public string Biography { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Login { get; set; } = null!;
    }
}