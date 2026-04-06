using KillPort.App.Localization;
using KillPort.App.Services;
using KillPort.App.Settings;
using KillPort.App.UI;

namespace KillPort.App;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var settings = new SettingsService();
        var localization = new LocalizationService(settings);
        var scanner = new PortScanner(localization);
        var closer = new PortCloser(localization);

        Application.Run(new TrayApplicationContext(scanner, closer, settings, localization));
    }
}
