using ExamGuard.Core.Interop;

namespace ExamGuard.Core.Hooks;

/// <summary>
/// Validates clipboard content: file copies (CF_HDROP) are preserved, while
/// text formats (CF_UNICODETEXT / CF_TEXT) are removed by emptying the
/// clipboard. Catches right-click Copy/Paste and programmatic copies that the
/// keyboard hook cannot see.
/// </summary>
public static class ClipboardGuard
{
    /// <summary>
    /// Returns true if the clipboard currently contains a file drop (copy/cut
    /// of files or folders) which must be preserved.
    /// </summary>
    public static bool ClipboardIsFileDrop()
    {
        bool opened = NativeMethods.OpenClipboard(IntPtr.Zero);
        if (!opened)
            return false;
        try
        {
            return NativeMethods.IsClipboardFormatAvailable(NativeMethods.CF_HDROP);
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }

    /// <summary>
    /// Removes text from the clipboard. If the clipboard is a file drop it is
    /// left untouched; otherwise any text content is cleared.
    /// </summary>
    public static void ClearTextIfPresent()
    {
        bool opened = NativeMethods.OpenClipboard(IntPtr.Zero);
        if (!opened)
            return;
        try
        {
            if (NativeMethods.IsClipboardFormatAvailable(NativeMethods.CF_HDROP))
                return; // File operation: keep it.

            bool hasText = NativeMethods.IsClipboardFormatAvailable(NativeMethods.CF_UNICODETEXT)
                || NativeMethods.IsClipboardFormatAvailable(NativeMethods.CF_TEXT);

            if (hasText)
                NativeMethods.EmptyClipboard();
        }
        finally
        {
            NativeMethods.CloseClipboard();
        }
    }
}
