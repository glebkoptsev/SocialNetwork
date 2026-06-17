namespace Libraries.Clients.Common
{
    public class UserAuthServiceToken
    {
        public string Access_token { get; set; } = null!;
        public int ExpiresIn { get; set; }
        private readonly DateTime _createdAt = DateTime.UtcNow;
        public bool IsExpired => (DateTime.UtcNow - _createdAt).TotalSeconds > ExpiresIn - 120;
    }
}
