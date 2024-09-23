namespace Libraries.Clients.Common
{
    public class UserAuthServiceOptions
    {
        public string URL { get; set; } = null!;
        public string URL_Debug { get; set; } = null!;
        public string Password { get; set; } = null!;
        public Guid User_id { get; set; }
    }
}