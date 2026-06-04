using Libraries.NpgsqlService;
using Libraries.NpgsqlService.Security;
using Moq;
using Npgsql;
using UserService.API.DTOs;
using UserService.API.Services;

namespace UserService.Tests;

public class UsersServiceTests
{
    private readonly Mock<INpgsqlService> _npgsqlMock;
    private readonly UsersService _service;

    public UsersServiceTests()
    {
        _npgsqlMock = new Mock<INpgsqlService>();
        _service = new UsersService(_npgsqlMock.Object);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsUser_WhenFound()
    {
        var userId = Guid.NewGuid();
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                ["first_name"] = "John",
                ["second_name"] = "Doe",
                ["birthdate"] = "1990-01-01",
                ["biography"] = "Bio",
                ["city"] = "NYC",
                ["password"] = PasswordHasher.Hash("pass"),
                ["can_publish_messages"] = true,
                ["login"] = "john_doe"
            }
        };

        _npgsqlMock
            .Setup(x => x.GetQueryResultAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>(),
                It.Is<string[]>(c => c.Contains("first_name")),
                It.IsAny<TargetSessionAttributes>()))
            .ReturnsAsync(data);

        var user = await _service.GetUserAsync(userId);

        Assert.NotNull(user);
        Assert.Equal("John", user!.First_name);
        Assert.Equal("Doe", user.Second_name);
        Assert.Equal("john_doe", user.Login);
    }

    [Fact]
    public async Task GetUserAsync_ReturnsNull_WhenNotFound()
    {
        _npgsqlMock
            .Setup(x => x.GetQueryResultAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>(),
                It.IsAny<string[]>(),
                It.IsAny<TargetSessionAttributes>()))
            .ReturnsAsync(new List<Dictionary<string, object>>());

        var user = await _service.GetUserAsync(Guid.NewGuid());
        Assert.Null(user);
    }

    [Fact]
    public async Task RegisterUserAsync_ReturnsUserId()
    {
        var request = new UserRegisterRequest
        {
            Login = "jane_smith",
            First_name = "Jane",
            Second_name = "Smith",
            Birthdate = "1995-05-15",
            Biography = "Developer",
            City = "LA",
            Password = "securePass"
        };

        _npgsqlMock
            .Setup(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        var response = await _service.RegisterUserAsync(request);

        Assert.NotEqual(Guid.Empty, response.User_id);
        _npgsqlMock.Verify(x => x.ExecuteNonQueryAsync(
            It.Is<string>(s => s.Contains("INSERT")),
            It.Is<NpgsqlParameter[]>(p => p.Length == 8)),
            Times.Once);
    }

    [Fact]
    public async Task SearchUserAsync_ReturnsUsers_WhenFound()
    {
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                ["first_name"] = "John",
                ["second_name"] = "Doe",
                ["birthdate"] = "1990-01-01",
                ["biography"] = "Bio",
                ["city"] = "NYC",
                ["user_id"] = Guid.NewGuid().ToString()
            }
        };

        _npgsqlMock
            .Setup(x => x.GetQueryResultAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>(),
                It.Is<string[]>(c => c.Contains("first_name")),
                It.IsAny<TargetSessionAttributes>()))
            .ReturnsAsync(data);

        var users = await _service.SearchUserAsync("John", "Doe");

        Assert.NotNull(users);
        var user = Assert.Single(users!);
        Assert.Equal("John", user.First_name);
    }

    [Fact]
    public async Task SearchUserAsync_ReturnsNull_WhenNotFound()
    {
        _npgsqlMock
            .Setup(x => x.GetQueryResultAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>(),
                It.IsAny<string[]>(),
                It.IsAny<TargetSessionAttributes>()))
            .ReturnsAsync(new List<Dictionary<string, object>>());

        var users = await _service.SearchUserAsync("Non", "Existent");
        Assert.Null(users);
    }
}
