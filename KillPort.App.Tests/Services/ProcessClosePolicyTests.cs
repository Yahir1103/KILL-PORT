using KillPort.App.Services;

namespace KillPort.App.Tests.Services;

public sealed class ProcessClosePolicyTests
{
    private const string WindowsDirectory = @"C:\Windows";

    [Fact]
    public void Evaluate_BlocksUnresolvableProcesses()
    {
        var decision = ProcessClosePolicy.Evaluate(
            processName: string.Empty,
            executablePath: string.Empty,
            processResolved: false,
            windowsDirectory: WindowsDirectory);

        Assert.False(decision.IsClosable);
        Assert.Equal(ProcessBlockReason.Unresolvable, decision.BlockReason);
    }

    [Fact]
    public void Evaluate_BlocksSvchostEvenOutsideWindowsDirectory()
    {
        var decision = ProcessClosePolicy.Evaluate(
            processName: "svchost",
            executablePath: @"D:\Apps\svchost.exe",
            processResolved: true,
            windowsDirectory: WindowsDirectory);

        Assert.False(decision.IsClosable);
        Assert.Equal(ProcessBlockReason.WindowsSvchost, decision.BlockReason);
    }

    [Fact]
    public void Evaluate_BlocksProcessesWithoutExecutablePath()
    {
        var decision = ProcessClosePolicy.Evaluate(
            processName: "python",
            executablePath: string.Empty,
            processResolved: true,
            windowsDirectory: WindowsDirectory);

        Assert.False(decision.IsClosable);
        Assert.Equal(ProcessBlockReason.NoExecutable, decision.BlockReason);
    }

    [Fact]
    public void Evaluate_BlocksProcessesInsideWindowsDirectory()
    {
        var decision = ProcessClosePolicy.Evaluate(
            processName: "edgebroker",
            executablePath: @"C:\WINDOWS\System32\edgeBroker.exe",
            processResolved: true,
            windowsDirectory: WindowsDirectory);

        Assert.False(decision.IsClosable);
        Assert.Equal(ProcessBlockReason.SystemProcess, decision.BlockReason);
    }

    [Fact]
    public void Evaluate_DoesNotTreatSimilarPathPrefixAsWindowsDirectory()
    {
        var decision = ProcessClosePolicy.Evaluate(
            processName: "customapp",
            executablePath: @"C:\WindowsApps\customapp.exe",
            processResolved: true,
            windowsDirectory: WindowsDirectory);

        Assert.True(decision.IsClosable);
        Assert.Equal(ProcessBlockReason.None, decision.BlockReason);
    }

    [Fact]
    public void Evaluate_AllowsResolvedUserProcessesOutsideWindowsDirectory()
    {
        var decision = ProcessClosePolicy.Evaluate(
            processName: "node",
            executablePath: @"D:\Tools\node.exe",
            processResolved: true,
            windowsDirectory: WindowsDirectory);

        Assert.True(decision.IsClosable);
        Assert.Equal(ProcessBlockReason.None, decision.BlockReason);
    }
}
