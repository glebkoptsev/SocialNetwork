using DialogService.API.DTOs;

namespace DialogService.API.Services
{
    public interface IChatService
    {
        Task<MessageDto[]> GetChatAsync(Guid chat_id, int limit, int offset, Guid user_id);
        Task<List<ChatDto>> GetUserChatListAsync(Guid user_id, int limit, int offset);
        Task<Guid> SendMessageToChatAsync(Guid chat_id, SendMessageRequest request, Guid creator_id);
        Task<Guid> CreateChatAsync(CreateChatRequest request, Guid creator_id);
        Task<Guid> CreateOrGetPersonalChatAsync(Guid currentUserId, Guid targetUserId);
        Task MarkChatAsReadAsync(Guid chat_id, Guid user_id);
    }
}
