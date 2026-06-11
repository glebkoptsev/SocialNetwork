namespace UserService.Database.Entities
{
    public class FriendEntity
    {
        public Guid User_id { get; set; }
        public Guid Friend_id { get; set; }

        public User User { get; set; } = null!;
        public User Friend { get; set; } = null!;
    }
}
