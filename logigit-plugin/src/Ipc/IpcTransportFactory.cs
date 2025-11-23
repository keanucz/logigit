namespace Loupedeck.LogiGitPlugin.Ipc
{
    using System;
    internal static class IpcTransportFactory
    {
        public static IIpcTransport Create()
        {
            if (OperatingSystem.IsWindows())
            {
                return new NamedPipeTransport();
            }
            if (OperatingSystem.IsMacOS() || OperatingSystem.IsLinux())
            {
                return new UnixDomainSocketTransport();
            }
            return new LoopbackTcpTransport();
        }
    }
}
