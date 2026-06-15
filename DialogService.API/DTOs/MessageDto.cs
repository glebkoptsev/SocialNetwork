namespace DialogService.API.DTOs
{
    public class MessageDto
    {
        public Guid Message_id { get; set; }
        public Guid Chat_id { get; set; }
        public Guid User_id { get; set; }
        public string User_name { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime Creation_datetime { get; set; }
    }
}