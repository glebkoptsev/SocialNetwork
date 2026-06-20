namespace UserService.API.DTOs
{
    public class PostResponse
    {
        public Guid Post_id { get; set; }
        public Guid User_id { get; set; }
        public string Text { get; set; } = null!;
        public DateTime Creation_datetime { get; set; }
        public string? AuthorFirstName { get; set; }
        public string? AuthorSecondName { get; set; }
        public int Like_count { get; set; }
    }
}