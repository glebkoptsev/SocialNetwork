namespace Libraries.Clients.Common
{
    public class UserAuthServiceToken
    {
        public string Access_token { get; set; } = null!;
        public int ExpiresIn { get; set; }
        private readonly DateTime _createdAt = DateTime.Now;
        public bool IsExpired => (DateTime.Now - _createdAt).TotalSeconds > ExpiresIn - 120;
    }
}
