namespace KillPort.App.Models;

public sealed record ClosePortResult(
    bool Success,
    string Message,
    bool RequiresElevation
);
