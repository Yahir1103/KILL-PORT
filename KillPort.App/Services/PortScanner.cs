using System.Diagnostics;
using System.Net;
using System.Runtime.InteropServices;
using KillPort.App.Interfaces;
using KillPort.App.Localization;
using KillPort.App.Models;

namespace KillPort.App.Services;

public sealed class PortScanner : IPortScanner
{
    private static readonly string WinDir =
        Environment.GetFolderPath(Environment.SpecialFolder.Windows);

    private readonly LocalizationService _localization;

    public PortScanner(LocalizationService localization)
    {
        _localization = localization;
    }

    public Task<IReadOnlyList<PortEntry>> GetListeningPortsAsync(
        CancellationToken cancellationToken = default) =>
        Task.Run(() => (IReadOnlyList<PortEntry>)ScanPorts(), cancellationToken);

    private List<PortEntry> ScanPorts()
    {
        var rows = ReadTcpTable();
        var result = new List<PortEntry>(rows.Count);

        foreach (var row in rows)
        {
            int pid = (int)row.dwOwningPid;
            if (pid is 0 or 4)
                continue;

            int port = (ushort)IPAddress.NetworkToHostOrder((short)row.dwLocalPort);
            string address = new IPAddress(row.dwLocalAddr).ToString();

            string processName = string.Empty;
            string executablePath = string.Empty;
            bool processResolved = false;

            try
            {
                var process = Process.GetProcessById(pid);
                processResolved = true;
                processName = process.ProcessName;

                try
                {
                    executablePath = process.MainModule?.FileName ?? string.Empty;
                }
                catch
                {
                    // System process or access denied while resolving the module path.
                }
            }
            catch
            {
                // The process exited or could not be resolved while scanning.
            }

            var decision = ProcessClosePolicy.Evaluate(
                processName,
                executablePath,
                processResolved,
                WinDir);

            result.Add(new PortEntry(
                port,
                address,
                pid,
                processName,
                executablePath,
                decision.IsClosable,
                GetBlockReason(decision.BlockReason)));
        }

        return result.OrderBy(entry => entry.Port).ToList();
    }

    private string GetBlockReason(ProcessBlockReason reason) =>
        reason switch
        {
            ProcessBlockReason.None => string.Empty,
            ProcessBlockReason.Unresolvable => _localization.Get("ProcessUnresolvable"),
            ProcessBlockReason.WindowsSvchost => _localization.Get("ProcessWindowsSvchost"),
            ProcessBlockReason.NoExecutable => _localization.Get("ProcessNoExecutable"),
            ProcessBlockReason.SystemProcess => _localization.Get("ProcessSystem"),
            _ => _localization.Get("ProcessUnresolvable"),
        };

    private static List<NativeMethods.MIB_TCPROW_OWNER_PID> ReadTcpTable()
    {
        int bufferSize = 0;
        NativeMethods.GetExtendedTcpTable(
            IntPtr.Zero,
            ref bufferSize,
            true,
            NativeMethods.AF_INET,
            NativeMethods.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_LISTENER,
            0);

        IntPtr buffer = Marshal.AllocHGlobal(bufferSize);
        try
        {
            uint error = NativeMethods.GetExtendedTcpTable(
                buffer,
                ref bufferSize,
                true,
                NativeMethods.AF_INET,
                NativeMethods.TCP_TABLE_CLASS.TCP_TABLE_OWNER_PID_LISTENER,
                0);

            if (error != 0)
                return [];

            int count = Marshal.ReadInt32(buffer);
            int rowSize = Marshal.SizeOf<NativeMethods.MIB_TCPROW_OWNER_PID>();
            var rows = new List<NativeMethods.MIB_TCPROW_OWNER_PID>(count);

            for (int index = 0; index < count; index++)
            {
                IntPtr rowPtr = IntPtr.Add(buffer, 4 + index * rowSize);
                rows.Add(Marshal.PtrToStructure<NativeMethods.MIB_TCPROW_OWNER_PID>(rowPtr));
            }

            return rows;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }
}
