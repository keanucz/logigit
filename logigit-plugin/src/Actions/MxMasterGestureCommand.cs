namespace Loupedeck.LogiGitPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    internal sealed class MxMasterGestureCommand : PluginDynamicCommand
    {
        private const String GestureGroup = "Gestures";
        private readonly Ipc.IpcClient _ipcClient;

        private static readonly IReadOnlyDictionary<String, String> GestureToCommand = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase)
        {
            ["ring.up"] = "git.pull",
            ["ring.down"] = "git.push",
            ["ring.left"] = "git.status",
            ["ring.right"] = "git.log",
        };

        public MxMasterGestureCommand() : base("MX Master 4 Gestures", "Git gestures", GestureGroup)
        {
            foreach (var gesture in GestureToCommand.Keys)
            {
                this.AddParameter(gesture, gesture, GestureGroup);
            }
            this._ipcClient = PluginServiceRegistry.IpcClient;
        }

        protected override void RunCommand(String actionParameter)
        {
            var gesture = actionParameter ?? "unknown";
            var correlationId = Guid.NewGuid().ToString();
            var mappedCommand = GestureToCommand.TryGetValue(gesture, out var command) ? command : "unknown";
            var payload = new
            {
                gesture,
                mappedCommand,
                issuedAt = DateTimeOffset.UtcNow
            };

            var message = new Ipc.IpcMessage
            {
                Type = "gesture",
                CorrelationId = correlationId,
                Payload = payload
            };

            _ = this.DispatchGestureAsync(message, payload, correlationId);
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using var builder = new BitmapBuilder(imageSize);
            builder.SetBackgroundImage(PluginResources.ReadImage("highlightOn.png"));
            builder.DrawText(actionParameter ?? "Gesture", 16);
            return builder.ToImage();
        }

        private async Task DispatchGestureAsync(Ipc.IpcMessage message, Object payload, String correlationId)
        {
            try
            {
                await this._ipcClient.SendAsync(message).ConfigureAwait(false);
                PluginServiceRegistry.TelemetryClient.EnqueueEvent("gesture", payload, correlationId);
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Failed to send gesture event");
            }
        }
    }
}
