namespace UserService.API.DTOs
{
    public class UpdateProfileRequest
    {
        public string First_name { get; set; } = null!;
        public string Second_name { get; set; } = null!;
        public string Birthdate { get; set; } = null!;
        public string Biography { get; set; } = null!;
        public string City { get; set; } = null!;
        public int Who_can_message { get; set; } = 0;
    }
}
