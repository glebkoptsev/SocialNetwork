using Libraries.NpgsqlService;
using Moq;
using Npgsql;
using UserService.Database;

namespace UserService.Tests;

public class PostRepositoryTests
{
    private readonly Mock<INpgsqlService> _npgsqlMock;
    private readonly PostRepository _repo;

    public PostRepositoryTests()
    {
        _npgsqlMock = new Mock<INpgsqlService>();
        _repo = new PostRepository(_npgsqlMock.Object);
    }

    [Fact]
    public async Task AddPostAsync_ReturnsNewGuid()
    {
        var userId = Guid.NewGuid();
        var expectedId = Guid.NewGuid();
        _npgsqlMock
            .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        var postId = await _repo.AddPostAsync(userId, "test post", expectedId);

        Assert.Equal(expectedId, postId);
        _npgsqlMock.Verify(x => x.ExecuteNonQueryAsync(
            It.Is<string>(s => s.Contains("INSERT")),
            It.Is<NpgsqlParameter[]>(p => p.Length == 3)),
            Times.Once);
    }

    [Fact]
    public async Task UpdatePostAsync_CallsExecuteNonQuery()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _npgsqlMock
            .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        await _repo.UpdatePostAsync(postId, "updated", userId);

        _npgsqlMock.Verify(x => x.ExecuteNonQueryAsync(
            It.Is<string>(s => s.Contains("UPDATE")),
            It.Is<NpgsqlParameter[]>(p => p.Length == 3)),
            Times.Once);
    }

    [Fact]
    public async Task DeletePostAsync_CallsExecuteNonQuery()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        _npgsqlMock
            .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        await _repo.DeletePostAsync(postId, userId);

        _npgsqlMock.Verify(x => x.ExecuteNonQueryAsync(
            It.Is<string>(s => s.Contains("DELETE")),
            It.Is<NpgsqlParameter[]>(p => p.Length == 2)),
            Times.Once);
    }

    [Fact]
    public async Task AddPostAsync_WithOutboxEntries_UsesTransaction()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var entries = new[] { new OutboxEntry("key1", "val1"), new OutboxEntry("key2", "val2") };

        _npgsqlMock
            .Setup(x => x.ExecuteTransactionAsync(It.IsAny<string[]>(), It.IsAny<NpgsqlParameter[][]>()))
            .Returns(Task.CompletedTask);

        var result = await _repo.AddPostAsync(userId, "test", postId, entries);

        Assert.Equal(postId, result);
        _npgsqlMock.Verify(x => x.ExecuteTransactionAsync(
            It.Is<string[]>(q => q.Length == 3 && q[0].Contains("INSERT")),
            It.IsAny<NpgsqlParameter[][]>()),
            Times.Once);
    }

    [Fact]
    public async Task AddPostAsync_WithoutOutboxEntries_UsesSimpleInsert()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        _npgsqlMock
            .Setup(x => x.ExecuteNonQueryAsync(It.IsAny<string>(), It.IsAny<NpgsqlParameter[]>()))
            .ReturnsAsync(1);

        var result = await _repo.AddPostAsync(userId, "test", postId, null);

        Assert.Equal(postId, result);
        _npgsqlMock.Verify(x => x.ExecuteNonQueryAsync(
            It.Is<string>(s => s.Contains("INSERT")),
            It.Is<NpgsqlParameter[]>(p => p.Length == 3)),
            Times.Once);
    }

    [Fact]
    public async Task GetPostAsync_ReturnsPost_WhenFound()
    {
        var postId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                ["user_id"] = userId.ToString(),
                ["post"] = "post content",
                ["creation_datetime"] = DateTime.Now
            }
        };

        _npgsqlMock
            .Setup(x => x.GetQueryResultAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>(),
                It.Is<string[]>(c => c.Contains("user_id")),
                It.IsAny<TargetSessionAttributes>()))
            .ReturnsAsync(data);

        var post = await _repo.GetPostAsync(postId);

        Assert.NotNull(post);
        Assert.Equal(postId, post!.Post_id);
        Assert.Equal("post content", post.Text);
    }

    [Fact]
    public async Task GetPostAsync_ReturnsNull_WhenNotFound()
    {
        _npgsqlMock
            .Setup(x => x.GetQueryResultAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>(),
                It.IsAny<string[]>(),
                It.IsAny<TargetSessionAttributes>()))
            .ReturnsAsync(new List<Dictionary<string, object>>());

        var post = await _repo.GetPostAsync(Guid.NewGuid());
        Assert.Null(post);
    }

    [Fact]
    public async Task GetFeedAsync_ReturnsPosts()
    {
        var userId = Guid.NewGuid();
        var data = new List<Dictionary<string, object>>
        {
            new()
            {
                ["post_id"] = Guid.NewGuid().ToString(),
                ["user_id"] = Guid.NewGuid().ToString(),
                ["post"] = "feed post 1",
                ["creation_datetime"] = DateTime.Now.AddHours(-1)
            },
            new()
            {
                ["post_id"] = Guid.NewGuid().ToString(),
                ["user_id"] = Guid.NewGuid().ToString(),
                ["post"] = "feed post 2",
                ["creation_datetime"] = DateTime.Now
            }
        };

        _npgsqlMock
            .Setup(x => x.GetQueryResultAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>(),
                It.Is<string[]>(c => c.Contains("post_id")),
                It.IsAny<TargetSessionAttributes>()))
            .ReturnsAsync(data);

        var posts = await _repo.GetFeedAsync(userId, 0, 10);

        Assert.Equal(2, posts.Count);
    }

    [Fact]
    public async Task GetFeedAsync_ReturnsEmpty_WhenNoPosts()
    {
        _npgsqlMock
            .Setup(x => x.GetQueryResultAsync(
                It.IsAny<string>(),
                It.IsAny<NpgsqlParameter[]>(),
                It.IsAny<string[]>(),
                It.IsAny<TargetSessionAttributes>()))
            .ReturnsAsync(new List<Dictionary<string, object>>());

        var posts = await _repo.GetFeedAsync(Guid.NewGuid(), 0, 10);
        Assert.Empty(posts);
    }
}
