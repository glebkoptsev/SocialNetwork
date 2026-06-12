using UserService.Database.Entities;

namespace UserService.Database
{
    public interface IPostRepository
    {
        Task<Guid> AddPostAsync(Guid user_id, string post, Guid postId, OutboxEntry[]? outboxEntries = null);
        Task UpdatePostAsync(Guid post_id, string post, Guid user_id, OutboxEntry[]? outboxEntries = null);
        Task DeletePostAsync(Guid post_id, Guid user_id, OutboxEntry[]? outboxEntries = null);
        Task<Post?> GetPostAsync(Guid post_id);
        Task<List<Post>> GetFeedAsync(Guid user_id, int offset, int limit);
        Task<List<Post>> GetUserPostsAsync(Guid author_id, int offset, int limit);
    }
}
