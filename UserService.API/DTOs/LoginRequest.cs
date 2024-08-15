namespace UserService.API.DTOs
{
    public class LoginRequest
    {
        public Guid Id { get; set; }
        public string Password { get; set; } = null!;
    }
}
