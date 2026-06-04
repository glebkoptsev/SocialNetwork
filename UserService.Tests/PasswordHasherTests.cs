using Libraries.NpgsqlService.Security;

namespace UserService.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_ReturnsFormattedString()
    {
        var hash = PasswordHasher.Hash("testpassword");
        var parts = hash.Split('.');
        Assert.Equal(3, parts.Length);
        Assert.Equal("210000", parts[0]);
        Assert.False(string.IsNullOrEmpty(parts[1]));
        Assert.False(string.IsNullOrEmpty(parts[2]));
    }

    [Fact]
    public void Check_ValidPassword_ReturnsTrue()
    {
        var password = "securePass123!";
        var hash = PasswordHasher.Hash(password);
        Assert.True(PasswordHasher.Check(hash, password));
    }

    [Fact]
    public void Check_WrongPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("correctPassword");
        Assert.False(PasswordHasher.Check(hash, "wrongPassword"));
    }

    [Fact]
    public void Check_InvalidFormat_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => PasswordHasher.Check("invalid-format", "pwd"));
    }

    [Fact]
    public void Hash_DifferentPasswords_DifferentHashes()
    {
        var hash1 = PasswordHasher.Hash("password1");
        var hash2 = PasswordHasher.Hash("password2");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Hash_SamePassword_DifferentSalts()
    {
        var hash1 = PasswordHasher.Hash("samepassword");
        var hash2 = PasswordHasher.Hash("samepassword");
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Check_EmptyPassword_ReturnsTrue()
    {
        var hash = PasswordHasher.Hash("");
        Assert.True(PasswordHasher.Check(hash, ""));
    }
}
