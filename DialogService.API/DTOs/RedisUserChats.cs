using DialogService.Database.Entities;

namespace DialogService.API.DTOs
{
    public class RedisUserChats
    {
        public Guid User_id { get; set; }
        public List<Chat> Chats { get; set; } = [];
    }
}
