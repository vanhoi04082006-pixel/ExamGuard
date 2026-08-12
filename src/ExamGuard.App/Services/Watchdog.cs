using System.Diagnostics;
using ExamGuard.Core.Misc;

namespace ExamGuard.App.Services;

/// <summary>
/// Sibling hidden process that restarts the service if a student kills it.
/// Detects liveness via the shared named mutex held by the main process.
/// </summary>
public static class Watchdog
{
    private static readonly Mutex ServiceMutex = new(initiallyOwned: true, ProcessGuard.ServiceMutexName);
    private static readonly Mutex WatchdogMutex = new(initiallyOwned: true, ProcessGuard.WatchdogMutexName);

    public static void Run()
    {
        // Holding WatchdogMutex marks this process as "a watchdog is present" so
        // that a freshly started service does not spawn a duplicate.
        try { WatchdogMutex.WaitOne(0); } catch (AbandonedMutexException) { }

        string exePath = Environment.ProcessPath ?? string.Empty;
        while (true)
        {
            bool acquired = false;
            try
            {
                acquired = ServiceMutex.WaitOne(0);
            }
            catch (AbandonedMutexException)
            {
                acquired = true;
            }

            if (acquired)
            {
                try
                {
                    if (File.Exists(ProcessGuard.StoppedFlagPath))
                        return; // Intentional teacher exit: stand down.

                    ProcessGuard.ClearStopFlag();
                    StartService(exePath);
                }
                finally
                {
                    ServiceMutex.ReleaseMutex();
                }
            }

            Thread.Sleep(2000);
        }
    }

    /// <summary>Ensures a watchdog sibling is present for the running service.</summary>
    public static void EnsureRunning()
    {
        try
        {
            bool exists = Mutex.TryOpenExisting(ProcessGuard.WatchdogMutexName, out Mutex? existing);
            if (exists)
            {
                existing?.Dispose();
                return;
            }
        }
        catch { }

        try
        {
            StartService(Environment.ProcessPath ?? string.Empty, "--watchdog");
        }
        catch { }
    }

    private static void StartService(string exePath, string args = "--service")
    {
        if (string.IsNullOrEmpty(exePath))
            return;
        var psi = new ProcessStartInfo(exePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = args
        };
        Process.Start(psi);
    }
}
