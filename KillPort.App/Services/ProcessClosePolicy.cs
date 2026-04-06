namespace KillPort.App.Services;

internal enum ProcessBlockReason
{
    None = 0,
    Unresolvable,
    WindowsSvchost,
    NoExecutable,
    SystemProcess,
}

internal readonly record struct ProcessCloseDecision(bool IsClosable, ProcessBlockReason BlockReason);

internal static class ProcessClosePolicy
{
    private static readonly char[] DirectorySeparators =
        [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar];

    public static ProcessCloseDecision Evaluate(
        string processName,
        string executablePath,
        bool processResolved,
        string windowsDirectory)
    {
        if (!processResolved)
            return Block(ProcessBlockReason.Unresolvable);

        if (string.Equals(processName, "svchost", StringComparison.OrdinalIgnoreCase))
            return Block(ProcessBlockReason.WindowsSvchost);

        if (string.IsNullOrWhiteSpace(executablePath))
            return Block(ProcessBlockReason.NoExecutable);

        if (IsUnderDirectory(executablePath, windowsDirectory))
            return Block(ProcessBlockReason.SystemProcess);

        return new ProcessCloseDecision(true, ProcessBlockReason.None);
    }

    private static ProcessCloseDecision Block(ProcessBlockReason reason) =>
        new(false, reason);

    private static bool IsUnderDirectory(string path, string directory)
    {
        if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(directory))
            return false;

        string normalizedDirectory = NormalizePath(directory);
        string normalizedPath = NormalizePath(path);

        return normalizedPath.Equals(normalizedDirectory, StringComparison.OrdinalIgnoreCase) ||
            normalizedPath.StartsWith(
                normalizedDirectory + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizePath(string path) =>
        Path.GetFullPath(path).TrimEnd(DirectorySeparators);
}
