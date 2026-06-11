using Microsoft.EntityFrameworkCore;
using UserService.API.DTOs;
using UserService.Database;
using UserService.Database.Entities;

namespace UserService.API.Services
{
    public class UsersService(UserDbContext context)
    {
        public async Task<User?> GetUserAsync(Guid id)
        {
            return await context.Users.FirstOrDefaultAsync(u => u.User_id == id);
        }

        public async Task<User?> GetUserByLoginAsync(string login)
        {
            return await context.Users.FirstOrDefaultAsync(u => u.Login.ToLower() == login.ToLower());
        }

        public async Task<UserRegisterResponse> RegisterUserAsync(UserRegisterRequest request)
        {
            var userId = Guid.NewGuid();
            context.Users.Add(new User
            {
                User_id = userId,
                First_name = request.First_name,
                Second_name = request.Second_name,
                Birthdate = request.Birthdate,
                Biography = request.Biography,
                City = request.City,
                Password = Libraries.Web.Common.Security.PasswordHasher.Hash(request.Password),
                Login = request.Login
            });
            await context.SaveChangesAsync();
            return new UserRegisterResponse { User_id = userId };
        }

        public async Task<List<User>> SearchUserAsync(string first_name, string second_name)
        {
            var users = await context.Users
                .Where(u => EF.Functions.Like(u.First_name.ToLower(), first_name.ToLower() + "%") && EF.Functions.Like(u.Second_name.ToLower(), second_name.ToLower() + "%"))
                .OrderBy(u => u.User_id)
                .ToListAsync();
            return users;
        }
    }
}
