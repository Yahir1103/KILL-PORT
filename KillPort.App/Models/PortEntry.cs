namespace KillPort.App.Models;

public sealed record PortEntry(
    int Port,
    string Address,
    int Pid,
    string ProcessName,
    string ExecutablePath,
    bool IsClosable,
    string BlockReason
);
