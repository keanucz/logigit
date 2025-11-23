namespace Loupedeck.LogiGitPlugin
{
    using System;
    using System.Threading.Tasks;

    internal abstract class GitCommandBase : PluginDynamicCommand
    {
        private readonly String _iconResource;
        private readonly Boolean _requiresCleanRepo;
        private Ipc.IpcClient IpcClient => PluginServiceRegistry.IpcClient;
        private HttpTelemetryClient Telemetry => PluginServiceRegistry.TelemetryClient;

        protected GitCommandBase(String commandId, String displayName, String description, String groupName, String iconResource, Boolean requiresCleanRepo = false)
            : base(displayName: displayName, description: description, groupName: groupName)
        {
            this.CommandId = commandId;
            this._iconResource = iconResource;
            this._requiresCleanRepo = requiresCleanRepo;
        }

        protected String CommandId { get; }

        protected override void RunCommand(String actionParameter)
        {
            var correlationId = Guid.NewGuid().ToString();
            var payload = new
            {
                command = this.CommandId,
                parameter = actionParameter,
                requiresCleanRepo = this._requiresCleanRepo,
                issuedAt = DateTimeOffset.UtcNow,
                source = "logitech"
            };

            var message = new Ipc.IpcMessage
            {
                Type = "git.intent",
                CorrelationId = correlationId,
                Payload = payload
            };

            _ = this.EmitTelemetryAsync("git.intent", payload, correlationId);
            _ = this.SendMessageAsync(message, correlationId);
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using var builder = new BitmapBuilder(imageSize);
            builder.SetBackgroundImage(PluginResources.ReadImage(this._iconResource));
            builder.DrawText(this.DisplayName, 16);
            return builder.ToImage();
        }

        private async Task SendMessageAsync(Ipc.IpcMessage message, String correlationId)
        {
            try
            {
                await this.IpcClient.SendAsync(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                PluginLog.Error(ex, $"Failed to send {this.CommandId} intent ({correlationId}).");
            }
        }

        private Task EmitTelemetryAsync(String eventName, Object payload, String correlationId)
        {
            return Task.Run(() => this.Telemetry.EnqueueEvent(eventName, payload, correlationId));
        }
    }
}
