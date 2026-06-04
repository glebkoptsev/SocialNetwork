namespace UserService.Database.Entities
{
    public class Post
    {
        public Post() { }
        public Post(Guid id, Dictionary<string, object> data)
        {
            Post_id = id;
            User_id = Guid.Parse(data["user_id"].ToString()!);
            Text = data["post"].ToString()!;
            Creation_datetime = DateTime.Parse(data["creation_datetime"].ToString()!);
            AuthorFirstName = data.TryGetValue("first_name", out var fn) ? fn.ToString() : null;
            AuthorSecondName = data.TryGetValue("second_name", out var sn) ? sn.ToString() : null;
        }

        public Post(Dictionary<string, object> data)
        {
            Post_id = Guid.Parse(data["post_id"].ToString()!);
            User_id = Guid.Parse(data["user_id"].ToString()!);
            Text = data["post"].ToString()!;
            Creation_datetime = Convert.ToDateTime(data["creation_datetime"]);
            AuthorFirstName = data.TryGetValue("first_name", out var fn) ? fn.ToString() : null;
            AuthorSecondName = data.TryGetValue("second_name", out var sn) ? sn.ToString() : null;
        }

        public Guid Post_id { get; set; }
        public Guid User_id { get; set; }
        public string Text { get; set; } = null!;
        public DateTime Creation_datetime { get; set; }
        public string? AuthorFirstName { get; set; }
        public string? AuthorSecondName { get; set; }
    }
}
