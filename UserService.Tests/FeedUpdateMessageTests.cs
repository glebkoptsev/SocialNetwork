using System.Text.Json;
using Libraries.Kafka.DTOs;

namespace UserService.Tests;

public class FeedUpdateMessageTests
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var postId = Guid.NewGuid();
        var authorId = Guid.NewGuid();
        var message = new FeedUpdateMessage(ActionTypeEnum.Create, postId, authorId, "Hello");
        Assert.Equal(ActionTypeEnum.Create, message.ActionType);
        Assert.Equal(postId, message.Post_id);
        Assert.Equal(authorId, message.Author_id);
        Assert.Equal("Hello", message.Text);
    }

    [Fact]
    public void Constructor_NullPostIdAndText()
    {
        var authorId = Guid.NewGuid();
        var message = new FeedUpdateMessage(ActionTypeEnum.Delete, null, authorId, null);
        Assert.Equal(ActionTypeEnum.Delete, message.ActionType);
        Assert.Null(message.Post_id);
        Assert.Null(message.Text);
        Assert.Equal(authorId, message.Author_id);
    }

    [Fact]
    public void Serialization_Roundtrip()
    {
        var message = new FeedUpdateMessage(ActionTypeEnum.Update, Guid.NewGuid(), Guid.NewGuid(), "Updated text");
        var json = JsonSerializer.Serialize(message);
        var deserialized = JsonSerializer.Deserialize<FeedUpdateMessage>(json);
        Assert.NotNull(deserialized);
        Assert.Equal(message.ActionType, deserialized!.ActionType);
        Assert.Equal(message.Post_id, deserialized.Post_id);
        Assert.Equal(message.Author_id, deserialized.Author_id);
        Assert.Equal(message.Text, deserialized.Text);
    }

    [Fact]
    public void ActionTypeEnum_Values()
    {
        Assert.Equal(0, (int)ActionTypeEnum.Create);
        Assert.Equal(1, (int)ActionTypeEnum.Update);
        Assert.Equal(2, (int)ActionTypeEnum.Delete);
        Assert.Equal(3, (int)ActionTypeEnum.FullReload);
    }

    [Fact]
    public void DefaultConstructor_InitializesDefaults()
    {
        var message = new FeedUpdateMessage();
        Assert.Equal(default, message.ActionType);
        Assert.Null(message.Post_id);
        Assert.Equal(Guid.Empty, message.Author_id);
        Assert.Null(message.Text);
    }
}
