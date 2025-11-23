namespace Loupedeck.LogiGitPlugin
{
    using System;

    /// <summary>
    /// Provides lightweight service-location for command/adjustment classes that are instantiated
    /// by the plugin host without constructor dependency injection.
    /// </summary>
    internal static class PluginServiceRegistry
    {
        private static AdjustmentSessionService _sessionService;
        private static HttpTelemetryClient _telemetryClient;
        private static Ipc.IpcClient _ipcClient;

        public static void Initialize(AdjustmentSessionService sessionService, HttpTelemetryClient telemetryClient, Ipc.IpcClient ipcClient)
        {
            _sessionService = sessionService ?? throw new ArgumentNullException(nameof(sessionService));
            _telemetryClient = telemetryClient ?? throw new ArgumentNullException(nameof(telemetryClient));
            _ipcClient = ipcClient ?? throw new ArgumentNullException(nameof(ipcClient));
        }

        public static AdjustmentSessionService SessionService => _sessionService ??
            throw new InvalidOperationException("AdjustmentSessionService has not been initialized yet.");

        public static HttpTelemetryClient TelemetryClient => _telemetryClient ??
            throw new InvalidOperationException("HttpTelemetryClient has not been initialized yet.");

        public static Ipc.IpcClient IpcClient => _ipcClient ??
            throw new InvalidOperationException("IpcClient has not been initialized yet.");
    }
}
