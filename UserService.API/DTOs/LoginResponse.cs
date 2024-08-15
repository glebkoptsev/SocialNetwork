namespace UserService.API.DTOs
{
    public class LoginResponse
    {
        public string Access_token { get; set; } = null!;
        public int ExpiresIn { get; set; }
    }
}
