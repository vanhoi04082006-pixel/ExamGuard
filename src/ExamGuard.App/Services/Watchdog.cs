using System.Diagnostics;
using ExamGuard.Core.Misc;

namespace ExamGuard.App.Services;

/// <summary>
/// Sibling hidden process that restarts the service if a student kills it.
/// Detects liveness via the shared named mutex held by the main process.
/// </summary>
public static class Watchdog
{
    // IMPORTANT: ServiceMutex must NOT be initiallyOwned. `initiallyOwned: true`
    // would give the watchdog permanent ownership (recursion depth >= 1 forever),
    // locking every newly started service out of the mutex.
    private static readonly Mutex ServiceMutex = new(initiallyOwned: false, ProcessGuard.ServiceMutexName);
    private static readonly Mutex WatchdogMutex = new(initiallyOwned: true, ProcessGuard.WatchdogMutexName);

    public static void Run()
    {
        // Holding WatchdogMutex marks this process as "a watchdog is present" so
        // that a freshly started service does not spawn a duplicate.
        bool watchdogAcquired;
        try { watchdogAcquired = WatchdogMutex.WaitOne(0); }
        catch (AbandonedMutexException) { watchdogAcquired = true; }
        FileLog.Write($"watchdog start, mutexAcquired={watchdogAcquired}");
        if (!watchdogAcquired)
            return; // Another watchdog is alive; this one (e.g. from Task Scheduler) stands down.

        try
        {
            string exePath = Environment.ProcessPath ?? string.Empty;
            DateTime lastRestart = DateTime.MinValue;
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
                    // Grace period: a service we just started may still be on its
                    // way to claiming the mutex. Do not spawn a duplicate.
                    if ((DateTime.UtcNow - lastRestart).TotalSeconds < 6)
                    {
                        try { ServiceMutex.ReleaseMutex(); } catch { }
                        Thread.Sleep(2000);
                        continue;
                    }

                    try
                    {
                        if (File.Exists(ProcessGuard.StoppedFlagPath))
                            return; // Intentional teacher exit: stand down.

                        ProcessGuard.ClearStopFlag();
                        lastRestart = DateTime.UtcNow;
                        FileLog.Write("restarting service");
                        StartService(exePath);
                    }
                    finally
                    {
                        try { ServiceMutex.ReleaseMutex(); } catch { }
                    }
                }

                Thread.Sleep(2000);
            }
        }
        finally
        {
            try { WatchdogMutex.ReleaseMutex(); }
            catch { }
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
