namespace Loupedeck.LogiGitPlugin.Ipc
{
    using System;
    using System.Buffers.Binary;
    using System.IO;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    internal static class IpcMessageSerializer
    {
        private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        public static async Task WriteMessageAsync(Stream stream, IpcMessage message, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(message, Options);
            Span<Byte> lengthBuffer = stackalloc Byte[sizeof(Int32)];
            BinaryPrimitives.WriteInt32LittleEndian(lengthBuffer, payload.Length);
            await stream.WriteAsync(lengthBuffer, cancellationToken).ConfigureAwait(false);
            await stream.WriteAsync(payload, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
        public static async Task<IpcMessage> ReadMessageAsync(Stream stream, CancellationToken cancellationToken)
        {
            var lengthBuffer = new Byte[sizeof(Int32)];
            await ReadExactlyAsync(stream, lengthBuffer, cancellationToken).ConfigureAwait(false);
            var length = BinaryPrimitives.ReadInt32LittleEndian(lengthBuffer);
            if (length <= 0)
            {
                throw new InvalidDataException("IPC payload length must be positive.");
            }
            var payloadBuffer = new Byte[length];
            await ReadExactlyAsync(stream, payloadBuffer, cancellationToken).ConfigureAwait(false);
            return JsonSerializer.Deserialize<IpcMessage>(payloadBuffer, Options);
        }
        private static async Task ReadExactlyAsync(Stream stream, Byte[] buffer, CancellationToken cancellationToken)
        {
            var offset = 0;
            while (offset < buffer.Length)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(offset, buffer.Length - offset), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException("Unexpected end of IPC stream.");
                }
                offset += read;
            }
        }
    }
}
