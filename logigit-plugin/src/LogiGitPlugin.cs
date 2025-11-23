namespace Loupedeck.LogiGitPlugin
{
    using System;
    using System.Threading.Tasks;

    // This class contains the plugin-level logic of the Loupedeck plugin.

    public class LogiGitPlugin : Plugin
    {
        private const String DefaultTelemetryUrl = "http://localhost:5055";
        private AdjustmentSessionService _sessionService;
        private HttpTelemetryClient _telemetryClient;
        private String _telemetryEndpoint;
        private Ipc.IpcClient _ipcClient;

        // Gets a value indicating whether this is an API-only plugin.
        public override Boolean UsesApplicationApiOnly => true;

        // Gets a value indicating whether this is a Universal plugin or an Application plugin.
        public override Boolean HasNoApplication => true;

        // Initializes a new instance of the plugin class.
        public LogiGitPlugin()
        {
            // Initialize the plugin log.
            PluginLog.Init(this.Log);

            // Initialize the plugin resources.
            PluginResources.Init(this.Assembly);
        }

        // This method is called when the plugin is loaded.
        public override void Load()
        {
            this._sessionService = new AdjustmentSessionService();
            this._telemetryEndpoint = this.ResolveTelemetryBaseUrl();
            this._telemetryClient = new HttpTelemetryClient(this._telemetryEndpoint);
            this._ipcClient = new Ipc.IpcClient(this.OnIpcMessageAsync);
            this._ipcClient.Start();

            this._sessionService.SessionChanged += this.OnSessionChanged;
            this._sessionService.DialChanged += this.OnDialChanged;

            PluginServiceRegistry.Initialize(this._sessionService, this._telemetryClient, this._ipcClient);

            PluginLog.Info($"LogiGitPlugin loaded. Telemetry endpoint: {this._telemetryEndpoint}");
        }

        // This method is called when the plugin is unloaded.
        public override void Unload()
        {
            if (this._sessionService != null)
            {
                this._sessionService.SessionChanged -= this.OnSessionChanged;
                this._sessionService.DialChanged -= this.OnDialChanged;
            }

            this._telemetryClient?.Dispose();
            this._ipcClient?.Dispose();
        }

        public String ActiveToggleId => this._sessionService?.ActiveToggleId;

        private void OnSessionChanged(Object sender, SessionChangedEventArgs args) =>
            this._telemetryClient?.EnqueueToggleEvent(args);

        private void OnDialChanged(Object sender, DialChangedEventArgs args)
        {
            this._telemetryClient?.EnqueueDialEvent(args);
            _ = this.PublishDialTelemetryAsync(args);
        }

        private async Task PublishDialTelemetryAsync(DialChangedEventArgs args)
        {
            if (args == null)
            {
                return;
            }

            var message = new Ipc.IpcMessage
            {
                Type = "dial.changed",
                Payload = new
                {
                    args.ToggleId,
                    args.Value,
                    args.Diff,
                    args.Timestamp,
                    correlationId = Guid.NewGuid().ToString()
                }
            };

            try
            {
                await this._ipcClient.SendAsync(message).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                PluginLog.Warning(ex, "Failed to publish dial telemetry over IPC.");
            }
        }

        private Task OnIpcMessageAsync(Ipc.IpcMessage message)
        {
            PluginLog.Info($"Received IPC message {message?.Type ?? "unknown"}");
            return Task.CompletedTask;
        }

        private String ResolveTelemetryBaseUrl()
        {
            var fromEnv = Environment.GetEnvironmentVariable("LOGIGIT_PLUGIN_TELEMETRY_URL");
            return String.IsNullOrWhiteSpace(fromEnv) ? DefaultTelemetryUrl : fromEnv;
        }
    }
}
