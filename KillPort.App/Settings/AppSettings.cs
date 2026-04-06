namespace KillPort.App.Settings;

public sealed class AppSettings
{
    public List<string> PinnedProcesses { get; set; } = [];
    public string? Language { get; set; }
}
