using System.Text.Json;

namespace KillPort.App.Settings;

public sealed class SettingsService
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "KillPort",
        "settings.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public AppSettings Current { get; private set; } = new();

    public SettingsService() => Load();

    private void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                Current = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            Current = new AppSettings();
        }

        Current.PinnedProcesses ??= [];
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Current, JsonOptions));
        }
        catch
        {
            // Ignore write errors in the user profile.
        }
    }

    public bool IsPinned(string processName) =>
        Current.PinnedProcesses.Any(
            item => item.Equals(processName, StringComparison.OrdinalIgnoreCase));
}
