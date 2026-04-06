using System.ComponentModel;
using System.Diagnostics;
using KillPort.App.Interfaces;
using KillPort.App.Localization;
using KillPort.App.Models;

namespace KillPort.App.Services;

public sealed class PortCloser : IPortCloser
{
    private readonly LocalizationService _localization;

    public PortCloser(LocalizationService localization)
    {
        _localization = localization;
    }

    public async Task<ClosePortResult> CloseAsync(
        PortEntry entry,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var process = Process.GetProcessById(entry.Pid);

            if (process.MainWindowHandle != IntPtr.Zero)
            {
                process.CloseMainWindow();
                await Task.Delay(1500, cancellationToken);
            }

            if (!process.HasExited)
                process.Kill();

            return new ClosePortResult(
                true,
                _localization.Format("ClosePortSuccess", entry.Port),
                false);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
        {
            return new ClosePortResult(
                false,
                _localization.Format("ClosePortAccessDenied", entry.ProcessName, entry.Pid),
                true);
        }
        catch (ArgumentException)
        {
            return new ClosePortResult(
                true,
                _localization.Get("ClosePortAlreadyExited"),
                false);
        }
        catch (Exception ex)
        {
            return new ClosePortResult(
                false,
                _localization.Format("ClosePortUnexpectedError", ex.Message),
                false);
        }
    }
}
