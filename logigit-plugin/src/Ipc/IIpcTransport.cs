namespace Loupedeck.LogiGitPlugin.Ipc
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    internal interface IIpcTransport : IDisposable
    {
        IpcTransportType TransportType { get; }
        Task StartAsync(Func<IpcMessage, Task> onMessageReceived, CancellationToken cancellationToken);
        Task SendAsync(IpcMessage message, CancellationToken cancellationToken);
        Task StopAsync(CancellationToken cancellationToken);
    }
}
