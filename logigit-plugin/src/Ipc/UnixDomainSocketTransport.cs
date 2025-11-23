namespace Loupedeck.LogiGitPlugin.Ipc
{
    using System;
    using System.IO;
    using System.IO.Pipes;
    using System.Net.Sockets;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Threading.Tasks;
    internal sealed class UnixDomainSocketTransport : IIpcTransport
    {
        private Socket _socket;
        private NetworkStream _stream;
        private CancellationTokenSource _listenCts;
        private Task _listenTask;
        public IpcTransportType TransportType => IpcTransportType.UnixDomainSocket;
        public async Task StartAsync(Func<IpcMessage, Task> onMessageReceived, CancellationToken cancellationToken)
        {
            var path = "/tmp/logigit-ipc.sock";
            this._socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
            await this._socket.ConnectAsync(new UnixDomainSocketEndPoint(path), cancellationToken).ConfigureAwait(false);
            this._stream = new NetworkStream(this._socket, ownsSocket: true);
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
            this._stream?.Dispose();
        }
        public void Dispose()
        {
            this._listenCts?.Cancel();
            this._stream?.Dispose();
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
