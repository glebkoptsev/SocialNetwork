using Libraries.Web.Common.Settings;

namespace UserService.Tests;

public class JwtSettingsTests
{
    [Fact]
    public void GetSigningCredentials_ReturnsCachedInstance()
    {
        var settings = new JwtSettings
        {
            Secret = "my-super-secret-key-that-is-long-enough!",
            TokenExpireSeconds = 3600,
            Issuer = "iss",
            Audience = "aud"
        };
        var creds1 = settings.GetSigningCredentials();
        var creds2 = settings.GetSigningCredentials();
        Assert.Same(creds1, creds2);
    }

    [Fact]
    public void GetSigningCredentials_ReturnsNonNull()
    {
        var settings = new JwtSettings
        {
            Secret = "test-secret-key-for-jwt-token-signing!",
            TokenExpireSeconds = 3600,
            Issuer = "test-issuer",
            Audience = "test-audience"
        };
        var creds = settings.GetSigningCredentials();
        Assert.NotNull(creds);
        Assert.NotNull(creds.Key);
    }

    [Fact]
    public void GetSigningCredentials_UsesHmacSha256()
    {
        var settings = new JwtSettings
        {
            Secret = "another-secret-key-that-is-long-enough!",
            TokenExpireSeconds = 7200,
            Issuer = "iss",
            Audience = "aud"
        };
        var creds = settings.GetSigningCredentials();
            Assert.Equal("HS256", creds.Algorithm);
    }

    [Fact]
    public void DifferentSecrets_ProduceDifferentKeys()
    {
        var settings1 = new JwtSettings { Secret = "secret-key-one-that-is-long-enough!!", TokenExpireSeconds = 3600, Issuer = "iss", Audience = "aud" };
        var settings2 = new JwtSettings { Secret = "secret-key-two-that-is-long-enough!!", TokenExpireSeconds = 3600, Issuer = "iss", Audience = "aud" };
        var keyBytes1 = Convert.FromBase64String(System.Convert.ToBase64String(((Microsoft.IdentityModel.Tokens.SymmetricSecurityKey)settings1.GetSigningCredentials().Key).Key));
        var keyBytes2 = Convert.FromBase64String(System.Convert.ToBase64String(((Microsoft.IdentityModel.Tokens.SymmetricSecurityKey)settings2.GetSigningCredentials().Key).Key));
        Assert.NotEqual(keyBytes1, keyBytes2);
    }
}
