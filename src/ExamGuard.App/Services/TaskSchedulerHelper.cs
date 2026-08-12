using System.Diagnostics;

namespace ExamGuard.App.Services;

/// <summary>
/// Task Scheduler safety net: registers a watchdog that runs every minute, so
/// the service is recovered even if a student kills BOTH processes in Task
/// Manager at once.
/// </summary>
public static class TaskSchedulerHelper
{
    public const string TaskName = "ExamGuardWatchdog";

    public static void Register(string exePath)
    {
        if (string.IsNullOrEmpty(exePath))
            return;
        var psi = new ProcessStartInfo("schtasks.exe")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            ArgumentList =
            {
                "/Create", "/F",
                "/SC", "MINUTE", "/MO", "1",
                "/TN", TaskName,
                "/TR", $"\"{exePath}\" --watchdog"
            }
        };
        try { Process.Start(psi)?.WaitForExit(3000); } catch { }
    }

    public static void Remove()
    {
        var psi = new ProcessStartInfo("schtasks.exe")
        {
            CreateNoWindow = true,
            UseShellExecute = false,
            WindowStyle = ProcessWindowStyle.Hidden,
            ArgumentList = { "/Delete", "/F", "/TN", TaskName }
        };
        try { Process.Start(psi)?.WaitForExit(3000); } catch { }
    }
}
