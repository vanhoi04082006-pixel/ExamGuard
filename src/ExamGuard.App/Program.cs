using ExamGuard.App.Forms;
using ExamGuard.App.Services;

namespace ExamGuard.App;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        bool watchdogMode = args.Contains("--watchdog", StringComparer.OrdinalIgnoreCase);
        bool initMode = args.Contains("--init", StringComparer.OrdinalIgnoreCase);

        if (watchdogMode)
        {
            Watchdog.Run();
            return;
        }

        if (initMode)
        {
            Initializer.InitializePassword();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new GuardForm());
    }
}
