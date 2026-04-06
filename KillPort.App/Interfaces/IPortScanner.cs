using KillPort.App.Models;

namespace KillPort.App.Interfaces;

public interface IPortScanner
{
    Task<IReadOnlyList<PortEntry>> GetListeningPortsAsync(CancellationToken cancellationToken = default);
}
