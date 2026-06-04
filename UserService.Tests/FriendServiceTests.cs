using Confluent.Kafka;
using Libraries.Kafka;
using Libraries.NpgsqlService;
using Moq;
using Npgsql;
using UserService.API.Services;

namespace UserService.Tests;

public class FriendServiceTests
{
    private readonly Mock<INpgsqlService> _npgsqlMock;
    private readonly Mock<IKafkaProducer> _kafkaMock;
    private readonly FriendService _service;

    public FriendServiceTests()
    {
        _npgsqlMock = new Mock<INpgsqlService>();
        _kafkaMock = new Mock<IKafkaProducer>();
        _service = new FriendService(_npgsqlMock.Object, _kafkaMock.Object);
    }

    [Fact]
    public async Task AddFriendAsync_InsertsAndProducesMessage()
    {
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();

        _npgsqlMock
            .Setup(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        await _service.AddFriendAsync(userId, friendId);

        _npgsqlMock.Verify(x => x.ExecuteNonQueryAsync(
            It.Is<string>(s => s.Contains("INSERT")),
            It.Is<NpgsqlParameter[]>(p => p.Length == 2)),
            Times.Once);

        _kafkaMock.Verify(x => x.ProduceAsync(
            "feed-posts",
            It.Is<Message<string, string>>(m => m.Key == userId.ToString())),
            Times.Once);
    }

    [Fact]
    public async Task DeleteFriendAsync_DeletesAndProducesMessage()
    {
        var userId = Guid.NewGuid();
        var friendId = Guid.NewGuid();

        _npgsqlMock
            .Setup(x => x.ExecuteNonQueryAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        await _service.DeleteFriendAsync(userId, friendId);

        _npgsqlMock.Verify(x => x.ExecuteNonQueryAsync(
            It.Is<string>(s => s.Contains("DELETE")),
            It.Is<NpgsqlParameter[]>(p => p.Length == 2)),
            Times.Once);

        _kafkaMock.Verify(x => x.ProduceAsync(
            "feed-posts",
            It.Is<Message<string, string>>(m => m.Key == userId.ToString())),
            Times.Once);
    }

    [Fact]
    public async Task GetFriendsAsync_ReturnsFriendIds()
    {
        var userId = Guid.NewGuid();
        var friendId1 = Guid.NewGuid();
        var friendId2 = Guid.NewGuid();
        var data = new List<Dictionary<string, object>>
        {
            new() { ["user_id"] = friendId1.ToString() },
            new() { ["user_id"] = friendId2.ToString() }
        };

        _npgsqlMock
            .Setup(x => x.GetQueryResultAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>(),
                It.Is<string[]>(c => c.Contains("user_id")),
                It.IsAny<TargetSessionAttributes>()))
            .ReturnsAsync(data);

        var friends = await _service.GetFriendsAsync(userId);

        Assert.Equal(2, friends.Count);
        Assert.Contains(friendId1, friends);
        Assert.Contains(friendId2, friends);
    }

    [Fact]
    public async Task GetFriendsAsync_ReturnsEmpty_WhenNoFriends()
    {
        _npgsqlMock
            .Setup(x => x.GetQueryResultAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>(),
                It.IsAny<string[]>(),
                It.IsAny<TargetSessionAttributes>()))
            .ReturnsAsync(new List<Dictionary<string, object>>());

        var friends = await _service.GetFriendsAsync(Guid.NewGuid());
        Assert.Empty(friends);
    }
}
