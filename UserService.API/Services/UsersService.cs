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
            return await context.Users.FirstOrDefaultAsync(u => EF.Functions.ILike(u.Login, login));
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

        public async Task<List<UserResponse>> SearchUserResponseAsync(string query, int offset, int limit)
        {
            var users = await SearchUserAsync(query, offset, limit);
            return users.Select(u => u.ToResponse()).ToList();
        }

        public async Task<List<User>> SearchUserAsync(string query, int offset, int limit)
        {
            var q = "%" + query + "%";
            var users = await context.Users
                .Where(u => EF.Functions.ILike(
                    u.First_name + " " + u.Second_name + " " + u.Login, q))
                .OrderBy(u => u.User_id)
                .Skip(offset)
                .Take(limit)
                .ToListAsync();
            return users;
        }

        public async Task<List<UserResponse>> GetSubscriptionsResponseAsync(Guid userId, int offset = 0, int limit = 20)
        {
            var ids = await context.Friends
                .Where(f => f.User_id == userId)
                .OrderBy(f => f.Friend_id)
                .Skip(offset)
                .Take(limit)
                .Select(f => f.Friend_id)
                .ToListAsync();

            return await context.Users
                .Where(u => ids.Contains(u.User_id))
                .Select(u => u.ToResponse())
                .ToListAsync();
        }

        public async Task<List<UserResponse>> GetFollowersResponseAsync(Guid userId, int offset = 0, int limit = 20)
        {
            var ids = await context.Friends
                .Where(f => f.Friend_id == userId)
                .OrderBy(f => f.User_id)
                .Skip(offset)
                .Take(limit)
                .Select(f => f.User_id)
                .ToListAsync();

            return await context.Users
                .Where(u => ids.Contains(u.User_id))
                .Select(u => u.ToResponse())
                .ToListAsync();
        }

        public async Task UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            var user = await context.Users.FirstOrDefaultAsync(u => u.User_id == userId)
                ?? throw new KeyNotFoundException($"User {userId} not found");

            user.First_name = request.First_name;
            user.Second_name = request.Second_name;
            user.Birthdate = request.Birthdate;
            user.Biography = request.Biography;
            user.City = request.City;
            user.Who_can_message = request.Who_can_message;
            await context.SaveChangesAsync();
        }
    }
}
