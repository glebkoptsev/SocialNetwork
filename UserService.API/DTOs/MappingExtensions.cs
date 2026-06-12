using UserService.Database.Entities;

namespace UserService.API.DTOs
{
    public static class MappingExtensions
    {
        public static UserResponse ToResponse(this User user)
        {
            return new UserResponse
            {
                User_id = user.User_id,
                First_name = user.First_name,
                Second_name = user.Second_name,
                Birthdate = user.Birthdate,
                Biography = user.Biography,
                City = user.City,
                Login = user.Login
            };
        }

        public static PostResponse ToResponse(this Post post)
        {
            return new PostResponse
            {
                Post_id = post.Post_id,
                User_id = post.User_id,
                Text = post.Text,
                Creation_datetime = post.Creation_datetime,
                AuthorFirstName = post.AuthorFirstName,
                AuthorSecondName = post.AuthorSecondName
            };
        }
    }
}