using ExamGuard.Core.Configuration;
using Xunit;

namespace ExamGuard.Core.Tests;

public class AppConfigTests
{
    [Fact]
    public void SetPassword_ReplacesOldPassword()
    {
        var config = AppConfig.CreateWithPassword("old");
        config.SetPassword("new");
        Assert.False(config.VerifyPassword("old"));
        Assert.True(config.VerifyPassword("new"));
    }

    [Fact]
    public void CreateWithPassword_HasPassword()
    {
        var config = AppConfig.CreateWithPassword("secret");
        Assert.True(config.HasPassword);
    }

    [Fact]
    public void VerifyPassword_WithoutPassword_ReturnsFalse()
    {
        var config = new AppConfig();
        Assert.False(config.VerifyPassword("anything"));
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(15, 15)]
    public void UnlockMinutes_StoredAsIs(int stored, int expected)
    {
        var config = new AppConfig { UnlockMinutes = stored };
        Assert.Equal(expected, Math.Max(1, config.UnlockMinutes));
    }

    [Fact]
    public void Unkillable_Defaults_ToTrue()
    {
        Assert.True(new AppConfig().Unkillable);
    }
}
