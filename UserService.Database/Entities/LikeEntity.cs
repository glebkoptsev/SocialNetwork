namespace UserService.Database.Entities
{
    public class LikeEntity
    {
        public Guid Post_id { get; set; }
        public Guid User_id { get; set; }
        public DateTime Created_at { get; set; }
    }
}
