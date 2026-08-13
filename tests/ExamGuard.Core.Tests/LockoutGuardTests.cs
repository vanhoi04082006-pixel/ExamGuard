using ExamGuard.Core.Security;
using Xunit;

namespace ExamGuard.Core.Tests;

public class LockoutGuardTests
{
    [Fact]
    public void NotLockedInitially()
    {
        var guard = new LockoutGuard(maxAttempts: 3, cooldownSeconds: 30);
        Assert.False(guard.IsLocked);
        Assert.Equal(0, guard.RemainingSeconds);
    }

    [Fact]
    public void FewerThanMaxFailures_DoesNotLock()
    {
        var guard = new LockoutGuard(maxAttempts: 3, cooldownSeconds: 30);
        guard.RegisterFailure();
        guard.RegisterFailure();
        Assert.False(guard.IsLocked);
    }

    [Fact]
    public void MaxFailures_Locks()
    {
        var guard = new LockoutGuard(maxAttempts: 3, cooldownSeconds: 30);
        guard.RegisterFailure();
        guard.RegisterFailure();
        guard.RegisterFailure();
        Assert.True(guard.IsLocked);
        Assert.InRange(guard.RemainingSeconds, 1, 31);
    }

    [Fact]
    public void CooldownExpiry_Unlocks()
    {
        var guard = new LockoutGuard(maxAttempts: 3, cooldownSeconds: 1);
        for (int i = 0; i < 3; i++)
            guard.RegisterFailure();
        Assert.True(guard.IsLocked);
        Thread.Sleep(1100);
        Assert.False(guard.IsLocked);
    }

    [Fact]
    public void Reset_ClearsLock()
    {
        var guard = new LockoutGuard(maxAttempts: 3, cooldownSeconds: 30);
        for (int i = 0; i < 3; i++)
            guard.RegisterFailure();
        Assert.True(guard.IsLocked);
        guard.Reset();
        Assert.False(guard.IsLocked);
        Assert.Equal(0, guard.RemainingSeconds);
    }
}
