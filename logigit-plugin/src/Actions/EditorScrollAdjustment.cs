namespace Loupedeck.LogiGitPlugin
{
    using System;

    // This class implements an example adjustment that counts the rotation ticks of a dial.

    public class EditorScrollAdjustment : PluginDynamicAdjustment
    {
        private readonly AdjustmentSessionService _sessionService;
        private readonly Ipc.IpcClient _ipcClient;

        // Initializes the adjustment class.
        // When `hasReset` is set to true, a reset command is automatically created for this adjustment.
        public EditorScrollAdjustment()
            : base(displayName: "Scroll IDE", description: "Scrolls the active editor", groupName: "LogiGit", hasReset: false)
        {
            this._sessionService = PluginServiceRegistry.SessionService;
            this._ipcClient = PluginServiceRegistry.IpcClient;
        }

        // This method is called when the dial associated to the plugin is rotated.
        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            this._sessionService.UpdateDial(diff, diff);
            _ = this.PublishScrollAsync(diff);
        }

        // This method is called when the reset command related to the adjustment is executed.
        protected override void RunCommand(String actionParameter)
        {
        }

        // Returns the adjustment value that is shown next to the dial.
        protected override String GetAdjustmentValue(String actionParameter) => String.Empty;

        private async Task PublishScrollAsync(Int32 diff)
        {
            var correlationId = Guid.NewGuid().ToString();
            var payload = new
            {
                action = "editor.scroll",
                ticks = diff,
                issuedAt = DateTimeOffset.UtcNow
            };

            var message = new Ipc.IpcMessage
            {
                Type = "dial.scroll",
                CorrelationId = correlationId,
                Payload = payload
            };

            try
            {
                await this._ipcClient.SendAsync(message).ConfigureAwait(false);
                PluginServiceRegistry.TelemetryClient.EnqueueEvent("dial.scroll", payload, correlationId);
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Failed to dispatch scroll event");
            }
        }
    }
}
