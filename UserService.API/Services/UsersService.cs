using Microsoft.EntityFrameworkCore;
using UserService.API.DTOs;
using UserService.Database;
using UserService.Database.Entities;

namespace UserService.API.Services
{
    public class UsersService(UserDbContext context)
    {
        public async Task<Guid> ResolveUserIdAsync(string id)
        {
            if (Guid.TryParse(id, out var guid))
                return guid;
            var user = await GetUserByLoginAsync(id);
            return user?.User_id ?? Guid.Empty;
        }

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

        public async Task<UserResponse?> GetUserResponseAsync(Guid id)
        {
            var user = await GetUserAsync(id);
            return user?.ToResponse();
        }

        public async Task<UserResponse?> GetUserByLoginResponseAsync(string login)
        {
            var user = await GetUserByLoginAsync(login);
            return user?.ToResponse();
        }

        public async Task<List<UserResponse>> SearchUserResponseAsync(string first_name, string second_name)
        {
            var users = await SearchUserAsync(first_name, second_name);
            return users.Select(u => u.ToResponse()).ToList();
        }

        public async Task<List<UserResponse>> GetSubscriptionsResponseAsync(Guid userId)
        {
            var ids = await context.Friends
                .Where(f => f.User_id == userId)
                .Select(f => f.Friend_id)
                .ToListAsync();

            return await context.Users
                .Where(u => ids.Contains(u.User_id))
                .Select(u => u.ToResponse())
                .ToListAsync();
        }

        public async Task<List<UserResponse>> GetFollowersResponseAsync(Guid userId)
        {
            var ids = await context.Friends
                .Where(f => f.Friend_id == userId)
                .Select(f => f.User_id)
                .ToListAsync();

            return await context.Users
                .Where(u => ids.Contains(u.User_id))
                .Select(u => u.ToResponse())
                .ToListAsync();
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
