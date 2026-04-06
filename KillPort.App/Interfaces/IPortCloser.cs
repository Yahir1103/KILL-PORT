using KillPort.App.Models;

namespace KillPort.App.Interfaces;

public interface IPortCloser
{
    Task<ClosePortResult> CloseAsync(PortEntry entry, CancellationToken cancellationToken = default);
}
