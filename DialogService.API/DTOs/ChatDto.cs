namespace DialogService.API.DTOs
{
    public class ChatDto
    {
        public Guid Chat_id { get; set; }
        public Guid Creator_id { get; set; }
        public string Chat_name { get; set; } = null!;
        public DateTime Creation_datetime { get; set; }
    }
}
