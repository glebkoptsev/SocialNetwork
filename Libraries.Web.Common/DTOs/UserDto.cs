namespace Libraries.Web.Common.DTOs
{
    public class UserDto
    {
        public Guid User_id { get; set; }
        public string First_name { get; set; } = null!;
        public string Second_name { get; set; } = null!;
        public string Birthdate { get; set; } = null!;
        public string Biography { get; set; } = null!;
        public string City { get; set; } = null!;
        public bool? CanPublishMessages { get; set; }
    }
}
