using UserService.Database.Entities;

namespace UserService.Tests;

public class EntitiesTests
{
    [Fact]
    public void User_DefaultConstructor_SetsProperties()
    {
        var user = new User();
        Assert.Equal(Guid.Empty, user.User_id);
        Assert.Null(user.First_name);
    }

    [Fact]
    public void User_ParameterizedConstructor_SetsProperties()
    {
        var id = Guid.NewGuid();
        var data = new Dictionary<string, object>
        {
            ["first_name"] = "John",
            ["second_name"] = "Doe",
            ["birthdate"] = "1990-01-01",
            ["biography"] = "Bio",
            ["city"] = "NYC",
            ["password"] = "hash123",
            ["can_publish_messages"] = true,
            ["login"] = "john_doe"
        };
        var user = new User(id, data);
        Assert.Equal(id, user.User_id);
        Assert.Equal("John", user.First_name);
        Assert.Equal("Doe", user.Second_name);
        Assert.Equal("1990-01-01", user.Birthdate);
        Assert.Equal("Bio", user.Biography);
        Assert.Equal("NYC", user.City);
        Assert.Equal("hash123", user.Password);
        Assert.True(user.CanPublishMessages);
        Assert.Equal("john_doe", user.Login);
    }

    [Fact]
    public void User_PasswordIsJsonIgnored()
    {
        var prop = typeof(User).GetProperty(nameof(User.Password));
        var attr = prop!.GetCustomAttributes(typeof(System.Text.Json.Serialization.JsonIgnoreAttribute), false);
        Assert.NotEmpty(attr);
    }

    [Fact]
    public void User_CanPublishMessagesNull_WhenMissing()
    {
        var id = Guid.NewGuid();
        var data = new Dictionary<string, object>
        {
            ["first_name"] = "John",
            ["second_name"] = "Doe",
            ["birthdate"] = "1990-01-01",
            ["biography"] = "Bio",
            ["city"] = "NYC",
            ["password"] = "hash123"
        };
        var user = new User(id, data);
        Assert.Null(user.CanPublishMessages);
    }

    [Fact]
    public void Post_DefaultConstructor_SetsProperties()
    {
        var post = new Post();
        Assert.Equal(Guid.Empty, post.Post_id);
        Assert.Null(post.Text);
    }

    [Fact]
    public void Post_ParameterizedConstructorWithId_SetsProperties()
    {
        var id = Guid.NewGuid();
        var data = new Dictionary<string, object>
        {
            ["user_id"] = Guid.NewGuid().ToString(),
            ["post"] = "Hello world",
            ["creation_datetime"] = "2024-01-15T10:30:00",
            ["first_name"] = "John",
            ["second_name"] = "Doe"
        };
        var post = new Post(id, data);
        Assert.Equal(id, post.Post_id);
        Assert.Equal("Hello world", post.Text);
        Assert.Equal(DateTime.Parse("2024-01-15T10:30:00"), post.Creation_datetime);
        Assert.Equal("John", post.AuthorFirstName);
        Assert.Equal("Doe", post.AuthorSecondName);
    }

    [Fact]
    public void Post_ParameterizedConstructorWithoutId_SetsProperties()
    {
        var data = new Dictionary<string, object>
        {
            ["post_id"] = Guid.NewGuid().ToString(),
            ["user_id"] = Guid.NewGuid().ToString(),
            ["post"] = "Test post",
            ["creation_datetime"] = "2024-06-01T12:00:00",
            ["first_name"] = "Jane",
            ["second_name"] = "Smith"
        };
        var post = new Post(data);
        Assert.Equal(Guid.Parse(data["post_id"].ToString()!), post.Post_id);
        Assert.Equal("Test post", post.Text);
        Assert.Equal(Convert.ToDateTime("2024-06-01T12:00:00"), post.Creation_datetime);
        Assert.Equal("Jane", post.AuthorFirstName);
        Assert.Equal("Smith", post.AuthorSecondName);
    }
}
