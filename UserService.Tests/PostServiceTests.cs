using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Moq;
using UserService.API.Services;
using UserService.Database;
using UserService.Database.Entities;

namespace UserService.Tests;

public class PostServiceTests
{
    private readonly Mock<IPostRepository> _repoMock;
    private readonly Mock<IDistributedCache> _cacheMock;
    private readonly PostService _service;

    public PostServiceTests()
    {
        _repoMock = new Mock<IPostRepository>();
        _cacheMock = new Mock<IDistributedCache>();
        _service = new PostService(_repoMock.Object, _cacheMock.Object);
    }

    [Fact]
    public async Task AddPostAsync_ReturnsPostId_AndCreatesOneOutboxEntry()
    {
        var userId = Guid.NewGuid();

        _repoMock
            .Setup(x => x.AddPostAsync(It.IsAny<Guid>(), "Hello", It.IsAny<Guid>(), It.IsAny<OutboxEntry?>()))
            .ReturnsAsync((Guid uid, string _, Guid pid, OutboxEntry? _) => pid);

        var result = await _service.AddPostAsync(userId, "Hello");

        Assert.NotEqual(Guid.Empty, result);
        _repoMock.Verify(x => x.AddPostAsync(userId, "Hello", result,
            It.Is<OutboxEntry?>(o => o != null)), Times.Once);
    }

    [Fact]
    public async Task UpdatePostAsync_UpdatesAndCreatesOneOutboxEntry()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        await _service.UpdatePostAsync(postId, "Updated", userId);

        _repoMock.Verify(x => x.UpdatePostAsync(postId, "Updated", userId,
            It.Is<OutboxEntry?>(o => o != null)), Times.Once);
    }

    [Fact]
    public async Task DeletePostAsync_DeletesAndCreatesOneOutboxEntry()
    {
        var userId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        await _service.DeletePostAsync(postId, userId);

        _repoMock.Verify(x => x.DeletePostAsync(postId, userId,
            It.Is<OutboxEntry?>(o => o != null)), Times.Once);
    }

    [Fact]
    public async Task GetPostAsync_ReturnsPost()
    {
        var postId = Guid.NewGuid();
        var post = new Post { Post_id = postId, Text = "content" };

        _repoMock.Setup(x => x.GetPostAsync(postId)).ReturnsAsync(post);

        var result = await _service.GetPostAsync(postId);

        Assert.NotNull(result);
        Assert.Equal(postId, result!.Post_id);
        Assert.Equal("content", result.Text);
    }

    [Fact]
    public async Task GetPostAsync_ReturnsNull_WhenNotFound()
    {
        _repoMock.Setup(x => x.GetPostAsync(It.IsAny<Guid>())).ReturnsAsync((Post?)null);

        var result = await _service.GetPostAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task GetFeedAsync_FallsBackToDb_WhenCacheMiss()
    {
        var userId = Guid.NewGuid();
        var dbPosts = new List<Post> { new() { Post_id = Guid.NewGuid(), Text = "db post" } };

        _cacheMock.Setup(x => x.GetAsync($"feed-{userId}", It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        _repoMock.Setup(x => x.GetFeedAsync(userId, 0, 10)).ReturnsAsync(dbPosts);

        var result = await _service.GetFeedAsync(userId, 0, 10);

        var list = result.ToList();
        Assert.Single(list);
        Assert.Equal("db post", list[0].Text);
    }

    [Fact]
    public async Task GetFeedAsync_FallsBackToDb_WhenCacheInsufficient()
    {
        var userId = Guid.NewGuid();
        var cachedPosts = new List<Post>
        {
            new() { Post_id = Guid.NewGuid(), Text = "only one", Creation_datetime = DateTime.UtcNow }
        };

        var cachedJson = System.Text.Json.JsonSerializer.Serialize(cachedPosts, API.Consts.JsonSerializerOptions);
        var cachedBytes = Encoding.UTF8.GetBytes(cachedJson);
        _cacheMock.Setup(x => x.GetAsync($"feed-{userId}", It.IsAny<CancellationToken>())).ReturnsAsync(cachedBytes);
        _repoMock.Setup(x => x.GetFeedAsync(userId, 0, 10)).ReturnsAsync(new List<Post>());

        var result = await _service.GetFeedAsync(userId, 0, 10);

        _repoMock.Verify(x => x.GetFeedAsync(userId, 0, 10), Times.Once);
    }
}
