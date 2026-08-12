using ExamGuard.Core.Configuration;
using Xunit;

namespace ExamGuard.Core.Tests;

public class ConfigStoreTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"examguard-test-{Guid.NewGuid():N}.json");

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }

    [Fact]
    public void Load_When_FileMissing_ReturnsDefault()
    {
        var store = new ConfigStore(_path);
        var config = store.Load();
        Assert.False(config.HasPassword);
    }

    [Fact]
    public void Save_Then_Load_RoundTrips()
    {
        var store = new ConfigStore(_path);
        var config = AppConfig.CreateWithPassword("secret");
        config.UnlockMinutes = 45;
        store.Save(config);

        var loaded = new ConfigStore(_path).Load();
        Assert.Equal(config.PasswordHash, loaded.PasswordHash);
        Assert.Equal(config.SaltBase64, loaded.SaltBase64);
        Assert.Equal(45, loaded.UnlockMinutes);
        Assert.True(loaded.VerifyPassword("secret"));
    }

    [Fact]
    public void CorruptFile_FallsBack_ToDefault()
    {
        File.WriteAllText(_path, "{not-json");
        var config = new ConfigStore(_path).Load();
        Assert.False(config.HasPassword);
    }
}
