namespace Loupedeck.LogiGitPlugin.Ipc
{
    using System;
    using System.IO.Pipes;
    using System.Threading;
    using System.Threading.Tasks;
    internal sealed class NamedPipeTransport : IIpcTransport
    {
        private NamedPipeClientStream _client;
        private CancellationTokenSource _listenCts;
        private Task _listenTask;
        public IpcTransportType TransportType => IpcTransportType.NamedPipe;
        public async Task StartAsync(Func<IpcMessage, Task> onMessageReceived, CancellationToken cancellationToken)
        {
            var pipeName = "LogiGitPipe";
            this._client = new NamedPipeClientStream(".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            await this._client.ConnectAsync(cancellationToken).ConfigureAwait(false);
            this._listenCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this._listenTask = Task.Run(() => this.ListenAsync(onMessageReceived, this._listenCts.Token));
        }
        public async Task SendAsync(IpcMessage message, CancellationToken cancellationToken)
        {
            await IpcMessageSerializer.SerializeAsync(this._client, message, cancellationToken).ConfigureAwait(false);
            await this._client.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this._listenCts?.Cancel();
            if (this._listenTask != null)
            {
                await this._listenTask.ConfigureAwait(false);
            }
            this._client?.Dispose();
        }
        public void Dispose()
        {
            this._listenCts?.Cancel();
            this._client?.Dispose();
            this._listenCts?.Dispose();
        }
        private async Task ListenAsync(Func<IpcMessage, Task> handler, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var message = await IpcMessageSerializer.DeserializeAsync(this._client, cancellationToken).ConfigureAwait(false);
                if (message != null)
                {
                    await handler(message).ConfigureAwait(false);
                }
            }
        }
    }
}
