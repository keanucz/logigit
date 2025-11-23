namespace Loupedeck.LogiGitPlugin.Ipc
{
    using System;
    using System.IO;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    internal sealed class LoopbackTcpTransport : IIpcTransport
    {
        private const Int32 DefaultPort = 58555;
        private TcpClient _client;
        private NetworkStream _stream;
        private CancellationTokenSource _listenCts;
        private Task _listenTask;
        public IpcTransportType TransportType => IpcTransportType.LoopbackTcp;
        public async Task StartAsync(Func<IpcMessage, Task> onMessageReceived, CancellationToken cancellationToken)
        {
            this._client = new TcpClient();
            await this._client.ConnectAsync("127.0.0.1", DefaultPort, cancellationToken).ConfigureAwait(false);
            this._stream = this._client.GetStream();
            this._listenCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this._listenTask = Task.Run(() => this.ListenAsync(onMessageReceived, this._listenCts.Token));
        }
        public async Task SendAsync(IpcMessage message, CancellationToken cancellationToken)
        {
            await IpcMessageSerializer.SerializeAsync(this._stream, message, cancellationToken).ConfigureAwait(false);
            await this._stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            this._listenCts?.Cancel();
            if (this._listenTask != null)
            {
                await this._listenTask.ConfigureAwait(false);
            }
            this._client?.Close();
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
                var message = await IpcMessageSerializer.DeserializeAsync(this._stream, cancellationToken).ConfigureAwait(false);
                if (message != null)
                {
                    await handler(message).ConfigureAwait(false);
                }
            }
        }
    }
}
