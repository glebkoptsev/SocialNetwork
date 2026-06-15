namespace DialogService.API.DTOs
{
    public class RedisChatMessage
    {
        public string Text { get; set; } = null!;
        public Guid User_id { get; set; }
        public string User_name { get; set; } = null!;
        public DateTime Created_at { get; set; }
        public Guid Message_id { get; set; }
        public int Status { get; set; }
    }
}
