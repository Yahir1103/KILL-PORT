using System.Runtime.InteropServices;

namespace KillPort.App.Services;

internal static class NativeMethods
{
    internal const int AF_INET = 2;

    internal enum TCP_TABLE_CLASS
    {
        TCP_TABLE_OWNER_PID_LISTENER = 3,
    }

    // State 2 = MIB_TCP_STATE_LISTEN
    internal const uint MIB_TCP_STATE_LISTEN = 2;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MIB_TCPROW_OWNER_PID
    {
        public uint dwState;
        public uint dwLocalAddr;
        public uint dwLocalPort;
        public uint dwRemoteAddr;
        public uint dwRemotePort;
        public uint dwOwningPid;
    }

    [DllImport("iphlpapi.dll", SetLastError = true)]
    internal static extern uint GetExtendedTcpTable(
        IntPtr pTcpTable,
        ref int dwOutBufLen,
        bool bOrder,
        int ulAf,
        TCP_TABLE_CLASS tableClass,
        uint reserved);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static extern bool DestroyIcon(IntPtr hIcon);
}
