using System.Text;
using ExamGuard.Core.Interop;

namespace ExamGuard.Core;

public static class ForegroundWindow
{
    private const int MaxNameLength = 256;

    public static string GetForegroundClassName()
    {
        IntPtr hWnd = NativeMethods.GetForegroundWindow();
        return GetClassName(hWnd);
    }

    public static string GetClassName(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return string.Empty;
        char[] buffer = new char[MaxNameLength];
        int length = NativeMethods.GetClassName(hWnd, buffer, MaxNameLength);
        return length > 0 ? new string(buffer, 0, length) : string.Empty;
    }

    public static string GetWindowTitle(IntPtr hWnd)
    {
        if (hWnd == IntPtr.Zero)
            return string.Empty;
        char[] buffer = new char[MaxNameLength];
        int length = NativeMethods.GetWindowText(hWnd, buffer, MaxNameLength);
        return length > 0 ? new string(buffer, 0, length) : string.Empty;
    }

    public static IntPtr GetForegroundHandle()
        => NativeMethods.GetForegroundWindow();

    public static bool IsExplorerWindow(string className)
    {
        if (string.IsNullOrEmpty(className))
            return false;
        return className.Equals("CabinetWClass", StringComparison.Ordinal)
            || className.Equals("Progman", StringComparison.Ordinal)
            || className.Equals("WorkerW", StringComparison.Ordinal);
    }
}
