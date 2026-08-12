namespace ExamGuard.Core.Misc;

public static class FileLog
{
    private static readonly object Lock = new();

    public static void Write(string message)
    {
        try
        {
            lock (Lock)
            {
                File.AppendAllText(
                    Path.Combine(AppContext.BaseDirectory, "examguard.log"),
                    $"{DateTime.Now:HH:mm:ss.fff} [{Environment.ProcessId}] {message}{Environment.NewLine}");
            }
        }
        catch { }
    }
}
