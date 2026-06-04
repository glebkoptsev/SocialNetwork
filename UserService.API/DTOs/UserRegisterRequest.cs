namespace UserService.API.DTOs
{
    public class UserRegisterRequest
    {
        public string Login { get; set; } = null!;
        public string First_name { get; set; } = null!;
        public string Second_name { get; set; } = null!;
        public string Birthdate { get; set; } = null!;
        public string Biography { get; set; } = null!;
        public string City { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
