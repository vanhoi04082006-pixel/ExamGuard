using ExamGuard.Core.Interop;

namespace ExamGuard.Core.Hooks;

/// <summary>
/// Global low-level keyboard hook (WH_KEYBOARD_LL) that swallows text-editing
/// shortcuts (Ctrl+C / Ctrl+X / Ctrl+V / Ctrl+Insert / Shift+Insert) whenever
/// the foreground window is NOT a file manager, so file copy/paste in Explorer
/// keeps working. Must be installed on a thread that runs a message pump.
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private IntPtr _hookHandle = IntPtr.Zero;
    private readonly NativeMethods.LowLevelKeyboardProc _proc;

    /// <summary>When non-null, decides whether the current shortcut should be blocked.</summary>
    public Func<bool>? ShouldBlock { get; set; }

    /// <summary>Master switch; when false no key is swallowed.</summary>
    public bool Enabled { get; set; } = true;

    public KeyboardHook()
    {
        _proc = HookCallback;
    }

    public void Install()
    {
        if (_hookHandle != IntPtr.Zero)
            return;
        using var process = System.Diagnostics.Process.GetCurrentProcess();
        using var module = process.MainModule!;
        _hookHandle = NativeMethods.SetWindowsHookEx(
            NativeMethods.WH_KEYBOARD_LL,
            _proc,
            NativeMethods.GetModuleHandle(module.ModuleName),
            0);
    }

    private IntPtr HookCallback(int nCode, nuint wParam, nint lParam)
    {
        if (nCode < 0 || !Enabled || ShouldBlock == null)
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        bool isDown = wParam == NativeMethods.WM_KEYDOWN || wParam == NativeMethods.WM_SYSKEYDOWN;
        if (!isDown)
            return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);

        var data = System.Runtime.InteropServices.Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

        bool isCtrl = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_CONTROL) & 0x8000) != 0;
        bool isShift = (NativeMethods.GetAsyncKeyState(NativeMethods.VK_SHIFT) & 0x8000) != 0;

        bool isBlockedCombo = IsBlockedCombo(data.vkCode, isCtrl, isShift);
        if (isBlockedCombo && ShouldBlock())
        {
            // Swallow the key press so the focused application never sees it.
            return new IntPtr(1);
        }

        return NativeMethods.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private static bool IsBlockedCombo(uint vkCode, bool isCtrl, bool isShift)
    {
        bool isCVX = vkCode is NativeMethods.VK_C or NativeMethods.VK_X or NativeMethods.VK_V;

        if (isCtrl && isCVX)
            return true;
        if (isCtrl && vkCode == NativeMethods.VK_INSERT)
            return true;
        if (isShift && vkCode == NativeMethods.VK_INSERT)
            return true;
        return false;
    }

    public void Dispose()
    {
        if (_hookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = IntPtr.Zero;
        }
    }
}
