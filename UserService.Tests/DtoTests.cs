using System.Text.Json;
using UserService.API;
using UserService.API.DTOs;

namespace UserService.Tests;

public class DtoTests
{
    [Fact]
    public void UserRegisterRequest_Roundtrip()
    {
        var request = new UserRegisterRequest
        {
            Login = "john_doe",
            First_name = "John",
            Second_name = "Doe",
            Birthdate = "1990-01-01",
            Biography = "Bio",
            City = "NYC",
            Password = "secret"
        };
        var json = JsonSerializer.Serialize(request, Consts.JsonSerializerOptions);
        var deserialized = JsonSerializer.Deserialize<UserRegisterRequest>(json, Consts.JsonSerializerOptions);
        Assert.NotNull(deserialized);
        Assert.Equal("john_doe", deserialized!.Login);
        Assert.Equal("John", deserialized.First_name);
        Assert.Equal("Doe", deserialized.Second_name);
        Assert.Equal("secret", deserialized.Password);
    }

    [Fact]
    public void UserRegisterResponse_Roundtrip()
    {
        var response = new UserRegisterResponse { User_id = Guid.NewGuid() };
        var json = JsonSerializer.Serialize(response, Consts.JsonSerializerOptions);
        var deserialized = JsonSerializer.Deserialize<UserRegisterResponse>(json, Consts.JsonSerializerOptions);
        Assert.NotNull(deserialized);
        Assert.Equal(response.User_id, deserialized!.User_id);
    }

    [Fact]
    public void LoginRequest_Roundtrip()
    {
        var request = new LoginRequest { Login = "john_doe", Password = "mypass" };
        var json = JsonSerializer.Serialize(request, Consts.JsonSerializerOptions);
        var deserialized = JsonSerializer.Deserialize<LoginRequest>(json, Consts.JsonSerializerOptions);
        Assert.NotNull(deserialized);
        Assert.Equal("john_doe", deserialized!.Login);
        Assert.Equal("mypass", deserialized.Password);
    }

    [Fact]
    public void LoginResponse_Roundtrip()
    {
        var response = new LoginResponse { Access_token = "token123", ExpiresIn = 3600 };
        var json = JsonSerializer.Serialize(response, Consts.JsonSerializerOptions);
        var deserialized = JsonSerializer.Deserialize<LoginResponse>(json, Consts.JsonSerializerOptions);
        Assert.NotNull(deserialized);
        Assert.Equal("token123", deserialized!.Access_token);
        Assert.Equal(3600, deserialized.ExpiresIn);
    }

    [Fact]
    public void AddPostRequest_Roundtrip()
    {
        var request = new AddPostRequest { Text = "My new post" };
        var json = JsonSerializer.Serialize(request, Consts.JsonSerializerOptions);
        var deserialized = JsonSerializer.Deserialize<AddPostRequest>(json, Consts.JsonSerializerOptions);
        Assert.NotNull(deserialized);
        Assert.Equal("My new post", deserialized!.Text);
    }

    [Fact]
    public void Consts_JsonSerializerOptions_IsWebDefaults()
    {
        var options = Consts.JsonSerializerOptions;
        Assert.True(options.PropertyNameCaseInsensitive);
    }
}
