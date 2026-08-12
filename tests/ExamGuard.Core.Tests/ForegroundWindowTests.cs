using Xunit;

namespace ExamGuard.Core.Tests;

public class ForegroundWindowTests
{
    [Theory]
    [InlineData("CabinetWClass", true)]
    [InlineData("Progman", true)]
    [InlineData("WorkerW", true)]
    [InlineData("Notepad", false)]
    [InlineData("Chrome_WidgetWin_1", false)]
    [InlineData("ConsoleWindowClass", false)]
    [InlineData("", false)]
    public void IsExplorerWindow_Classifies(string className, bool expected)
    {
        Assert.Equal(expected, ForegroundWindow.IsExplorerWindow(className));
    }
}
