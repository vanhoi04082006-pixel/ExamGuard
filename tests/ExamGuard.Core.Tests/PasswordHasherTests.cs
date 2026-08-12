using ExamGuard.Core.Security;
using Xunit;

namespace ExamGuard.Core.Tests;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_And_Verify_RoundTrips()
    {
        var (salt, hash) = PasswordHasher.Hash("giaoVien#2026");
        Assert.True(PasswordHasher.Verify("giaoVien#2026", salt, hash));
    }

    [Fact]
    public void Verify_Rejects_WrongPassword()
    {
        var (salt, hash) = PasswordHasher.Hash("dung");
        Assert.False(PasswordHasher.Verify("sai", salt, hash));
    }

    [Fact]
    public void Verify_Rejects_EmptyExpectedHash()
    {
        Assert.False(PasswordHasher.Verify("x", new byte[16], string.Empty));
    }

    [Fact]
    public void Salt_Makes_Hashes_Different_For_SamePassword()
    {
        var (_, h1) = PasswordHasher.Hash("abc");
        var (_, h2) = PasswordHasher.Hash("abc");
        Assert.NotEqual(h1, h2);
    }
}
