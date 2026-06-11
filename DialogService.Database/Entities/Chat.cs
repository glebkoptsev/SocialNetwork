namespace DialogService.Database.Entities
{
    public class Chat
    {
        public Guid Chat_id { get; set; }
        public Guid Creator_id { get; set; }
        public string Chat_name { get; set; } = null!;
        public DateTime Creation_datetime { get; set; }
        public DateTime Last_update_datetime { get; set; }
    }
}
