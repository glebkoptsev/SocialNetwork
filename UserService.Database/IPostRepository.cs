using UserService.Database.Entities;

namespace UserService.Database
{
    public interface IPostRepository
    {
        Task<Guid> AddPostAsync(Guid user_id, string post);
        Task UpdatePostAsync(Guid post_id, string post, Guid user_id);
        Task DeletePostAsync(Guid post_id, Guid user_id);
        Task<Post?> GetPostAsync(Guid post_id);
        Task<List<Post>> GetFeedAsync(Guid user_id, int offset, int limit);
    }
}
