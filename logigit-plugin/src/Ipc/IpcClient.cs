namespace Loupedeck.LogiGitPlugin.Ipc
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    internal sealed class IpcClient : IDisposable
    {
        private readonly IIpcTransport _transport;
        private readonly Func<IpcMessage, Task> _handler;
        private readonly CancellationTokenSource _cts = new();
        private Task _startTask;
        public IpcClient(Func<IpcMessage, Task> handler)
        {
            this._handler = handler ?? throw new ArgumentNullException(nameof(handler));
            this._transport = IpcTransportFactory.Create();
        }
        public void Start()
        {
            this._startTask = Task.Run(() => this.RunAsync(this._cts.Token));
        }
        public async Task SendAsync(IpcMessage message)
        {
            await this._transport.SendAsync(message, this._cts.Token).ConfigureAwait(false);
        }
        public void Dispose()
        {
            this._cts.Cancel();
            this._transport.Dispose();
            this._cts.Dispose();
        }
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            try
            {
                await this._transport.StartAsync(this._handler, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"IPC transport {this._transport.TransportType} failed to start.");
            }
        }
    }
}
