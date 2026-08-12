using System.Diagnostics;
using System.Text;
using ExamGuard.Core.Configuration;
using ExamGuard.Core.Misc;

namespace ExamGuard.App.Services;

/// <summary>
/// Removes ExamGuard from the machine permanently: writes the stop flag so the
/// watchdog stands down, removes the Task Scheduler task and the Run autostart
/// entry, then deletes the install folder (exe, config, logs) via a detached
/// helper that outlives the current process (a running exe cannot delete itself).
/// </summary>
public static class SelfDelete
{
    public static void Run()
    {
        try { ProcessGuard.WriteStopFlag(); } catch { }
        try { TaskSchedulerHelper.Remove(); } catch { }
        try { AutoStart.Disable(); } catch { }

        try
        {
            string folder = AppContext.BaseDirectory.TrimEnd('\\', '/');
            string exe = Environment.ProcessPath ?? string.Empty;
            string helper = Path.Combine(Path.GetTempPath(), "examguard_uninstall_" + Guid.NewGuid().ToString("N") + ".cmd");

            var sb = new StringBuilder();
            sb.AppendLine("@echo off");
            // Give the current process and the watchdog sibling time to exit.
            sb.AppendLine("ping -n 6 127.0.0.1 >nul");
            if (!string.IsNullOrEmpty(exe))
                sb.AppendLine($"del /f /q \"{exe}\" >nul 2>&1");
            sb.AppendLine($"rd /s /q \"{folder}\" >nul 2>&1");
            sb.AppendLine($"del /f /q \"%~f0\" >nul 2>&1");
            File.WriteAllText(helper, sb.ToString());

            var psi = new ProcessStartInfo("cmd.exe", $"/c \"{helper}\"")
            {
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            };
            Process.Start(psi);
        }
        catch { }
    }
}
