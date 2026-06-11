namespace UserService.API.Services
{
    public interface IFriendService
    {
        Task<List<Guid>> GetFriendsAsync(Guid user_id);
        Task AddFriendAsync(Guid user_id, Guid friend_id);
        Task DeleteFriendAsync(Guid user_id, Guid friend_id);
        Task<bool> IsFriendAsync(Guid user_id, Guid friend_id);
    }
}
