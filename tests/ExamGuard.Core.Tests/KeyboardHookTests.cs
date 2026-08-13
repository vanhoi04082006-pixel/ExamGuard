using ExamGuard.Core.Hooks;
using ExamGuard.Core.Interop;
using Xunit;

namespace ExamGuard.Core.Tests;

public class KeyboardHookTests
{
    [Theory]
    [InlineData(NativeMethods.VK_C, true, false)]
    [InlineData(NativeMethods.VK_X, true, false)]
    [InlineData(NativeMethods.VK_V, true, false)]
    [InlineData(NativeMethods.VK_INSERT, true, false)]
    [InlineData(NativeMethods.VK_INSERT, false, true)]
    public void BlockedCombos_ReturnTrue(uint vk, bool ctrl, bool shift)
    {
        Assert.True(KeyboardHook.IsBlockedCombo(vk, ctrl, shift));
    }

    [Theory]
    [InlineData(NativeMethods.VK_C, false, false)] // plain C, no modifier
    [InlineData(NativeMethods.VK_C, false, true)]  // Shift+C (uppercase C)
    [InlineData(0x5A, true, false)]                // Ctrl+Z (undo) - not blocked
    [InlineData(0x41, true, false)]                // Ctrl+A (select all) - not blocked
    public void NonBlockedCombos_ReturnFalse(uint vk, bool ctrl, bool shift)
    {
        Assert.False(KeyboardHook.IsBlockedCombo(vk, ctrl, shift));
    }
}
