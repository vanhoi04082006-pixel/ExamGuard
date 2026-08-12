namespace ExamGuard.Core.Misc;

public static class ProcessGuard
{
    public const string ServiceMutexName = "ExamGuard_Service_RunMutex_7F3A";
    public const string WatchdogMutexName = "ExamGuard_Watchdog_RunMutex_7F3A";

    public const string StoppedFlagName = "examguard.stopped";

    public static string StoppedFlagPath => Path.Combine(AppContext.BaseDirectory, StoppedFlagName);

    public static void WriteStopFlag()
    {
        try { File.WriteAllText(StoppedFlagPath, DateTime.UtcNow.ToString("O")); } catch { }
    }

    public static void ClearStopFlag()
    {
        try { if (File.Exists(StoppedFlagPath)) File.Delete(StoppedFlagPath); } catch { }
    }
}
