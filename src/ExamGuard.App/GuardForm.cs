using System.Diagnostics;
using ExamGuard.App.Forms;
using ExamGuard.App.Services;
using ExamGuard.Core;
using ExamGuard.Core.Configuration;
using ExamGuard.Core.Hooks;
using ExamGuard.Core.Interop;
using ExamGuard.Core.Misc;

namespace ExamGuard.App;

/// <summary>
/// Hidden application host. Owns the keyboard hook, the clipboard guard, the
/// teacher hotkey and the unlock/relock state. Never shown on screen.
/// </summary>
public sealed class GuardForm : Form
{
    private const int HotKeyId = 1;

    private static readonly Mutex ServiceMutex = new(initiallyOwned: false, ProcessGuard.ServiceMutexName);

    private readonly ConfigStore _store;
    private readonly AppConfig _config;
    private readonly LockoutGuard _lockout = new();
    private readonly KeyboardHook _keyboardHook = new();
    private readonly System.Windows.Forms.Timer _relockTimer;
    private bool _unlocked;
    private bool _dialogOpen;
    private bool _exiting;

    public GuardForm()
    {
        _store = new ConfigStore();
        _config = _store.Load();

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        Opacity = 0;
        Visible = false;

        _relockTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _relockTimer.Tick += (_, _) =>
        {
            _unlocked = false;
            _relockTimer.Stop();
        };
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        if (!_config.HasPassword)
            RunSetupOrExit();
        RegisterHotKey();
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Hide();
        ProcessGuard.ClearStopFlag();

        // Register auto-start at logon (idempotent).
        string? exePath = Environment.ProcessPath;
        if (!string.IsNullOrEmpty(exePath))
            AutoStart.Enable(exePath);

        // Hold the run mutex for this process's lifetime; the watchdog uses it
        // to detect whether the service is alive.
        try { ServiceMutex.WaitOne(); } catch (AbandonedMutexException) { }

        _keyboardHook.ShouldBlock = ShouldBlockKeys;
        _keyboardHook.Install();

        if (!NativeMethods.AddClipboardFormatListener(Handle))
        {
            // Extremely old Windows; fall back to polling.
            var poller = new System.Windows.Forms.Timer { Interval = 500 };
            poller.Tick += (_, _) => { if (!_unlocked) ClipboardGuard.ClearTextIfPresent(); };
            poller.Start();
        }

        // Remove any text that was on the clipboard before the guard started.
        ClipboardGuard.ClearTextIfPresent();

        Watchdog.EnsureRunning();
    }

    protected override void WndProc(ref Message m)
    {
        switch (m.Msg)
        {
            case NativeMethods.WM_HOTKEY:
                ShowPasswordDialog();
                break;
            case NativeMethods.WM_CLIPBOARDUPDATE:
                if (!_unlocked)
                    ClipboardGuard.ClearTextIfPresent();
                break;
            default:
                base.WndProc(ref m);
                break;
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        base.OnFormClosing(e);
        if (!_exiting)
        {
            // Block accidental termination (Alt+F4, Close, Task Manager "End task"
            // sends WM_CLOSE too). Only the password-gated Exit path may close us.
            e.Cancel = true;
            Hide();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            NativeMethods.UnregisterHotKey(Handle, HotKeyId);
            _keyboardHook.Dispose();
            NativeMethods.RemoveClipboardFormatListener(Handle);
            _relockTimer.Dispose();
            try { ServiceMutex.ReleaseMutex(); } catch { }
        }
        base.Dispose(disposing);
    }

    private bool ShouldBlockKeys()
        => !_unlocked
           && !_dialogOpen
           && !ForegroundWindow.IsExplorerWindow(ForegroundWindow.GetForegroundClassName());

    private void ShowPasswordDialog()
    {
        if (_dialogOpen)
            return;
        _dialogOpen = true;
        try
        {
            using var dlg = new PasswordDialog(_config, _store, _lockout);
            dlg.Shown += (_, _) => FlashSelf(dlg);
            var result = dlg.ShowDialog(this);
            if (result != DialogResult.OK)
                return;

            switch (dlg.Result)
            {
                case PasswordAction.Unlock:
                    _unlocked = true;
                    _relockTimer.Interval = Math.Max(1, _config.UnlockMinutes) * 60_000;
                    _relockTimer.Start();
                    break;
                case PasswordAction.Exit:
                    ProcessGuard.WriteStopFlag();
                    _exiting = true;
                    Close();
                    break;
            }
        }
        finally
        {
            _dialogOpen = false;
        }
    }

    private void RunSetupOrExit()
    {
        var setup = new SetupPasswordForm();
        if (setup.ShowDialog() == DialogResult.OK)
        {
            _config.SetPassword(setup.NewPassword);
            _store.Save(_config);
        }
        else
        {
            ProcessGuard.WriteStopFlag();
            _exiting = true;
            Close();
        }
    }

    private void RegisterHotKey()
    {
        uint[] modifierSets =
        {
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT | NativeMethods.MOD_SHIFT,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
            NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT,
        };
        const uint vkG = 0x47;
        foreach (uint mods in modifierSets)
        {
            if (NativeMethods.RegisterHotKey(Handle, HotKeyId, mods | NativeMethods.MOD_NOREPEAT, vkG))
                return;
        }
    }

    private static void FlashSelf(Form form)
    {
        try
        {
            var info = new NativeMethods.FLASHWINFO
            {
                cbSize = (uint)System.Runtime.InteropServices.Marshal.SizeOf<NativeMethods.FLASHWINFO>(),
                hwnd = form.Handle,
                dwFlags = NativeMethods.FLASHW_ALL | NativeMethods.FLASHW_TIMERNOFG,
                uCount = 3
            };
            NativeMethods.SetForegroundWindow(form.Handle);
            NativeMethods.FlashWindowEx(ref info);
        }
        catch { }
    }
}
