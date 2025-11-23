namespace Loupedeck.LogiGitPlugin
{
    using System;
    using System.Collections.Concurrent;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Asynchronously posts telemetry payloads to an HTTP endpoint with retry/backoff.
    /// </summary>
    internal sealed class HttpTelemetryClient : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly ConcurrentQueue<TelemetryEnvelope> _queue = new();
        private readonly SemaphoreSlim _queueSignal = new(0);
        private readonly CancellationTokenSource _cts = new();
        private readonly Task _worker;
        private readonly Int32 _maxRetries;
        private readonly TimeSpan _initialBackoff;
        private Boolean _disposed;

        public HttpTelemetryClient(String baseUrl, Int32 maxRetries = 3, TimeSpan? initialBackoff = null)
        {
            if (String.IsNullOrWhiteSpace(baseUrl))
            {
                throw new ArgumentException("Base URL must be provided.", nameof(baseUrl));
            }

            this._httpClient = new HttpClient
            {
                BaseAddress = new Uri(baseUrl, UriKind.Absolute),
                Timeout = TimeSpan.FromSeconds(5)
            };

            this._maxRetries = Math.Max(1, maxRetries);
            this._initialBackoff = initialBackoff ?? TimeSpan.FromMilliseconds(250);
            this._worker = Task.Run(this.ProcessQueueAsync);
        }

        public void EnqueueDialEvent(DialChangedEventArgs args)
        {
            if (args == null || !args.HasActiveToggle)
            {
                return;
            }

            var payload = new
            {
                toggleId = args.ToggleId,
                value = args.Value,
                diff = args.Diff,
                timestamp = args.Timestamp,
                source = "LogiGitPlugin"
            };

            this.Enqueue(new TelemetryEnvelope("/api/dial-events", payload, $"dial:{args.ToggleId}"));
        }

        public void EnqueueToggleEvent(SessionChangedEventArgs args)
        {
            if (args == null)
            {
                return;
            }

            var payload = new
            {
                toggleId = args.ToggleId,
                timestamp = DateTimeOffset.UtcNow,
                source = "LogiGitPlugin"
            };

            this.Enqueue(new TelemetryEnvelope("/api/toggle-events", payload, $"toggle:{args.ToggleId ?? "none"}"));
        }

        public void EnqueueEvent(String eventName, Object payload, String correlationId)
        {
            if (String.IsNullOrWhiteSpace(eventName))
            {
                throw new ArgumentException("Event name must be provided.", nameof(eventName));
            }

            var path = $"/api/events/{eventName}";
            var envelope = new TelemetryEnvelope(path, payload, correlationId ?? "none");
            this.Enqueue(envelope);
        }

        public void Dispose()
        {
            if (this._disposed)
            {
                return;
            }

            this._disposed = true;
            this._cts.Cancel();
            this._queueSignal.Release();

            try
            {
                this._worker.Wait();
            }
            catch (Exception ex) when (ex is TaskCanceledException || ex is AggregateException)
            {
                PluginLog.Warning(ex, "Telemetry worker stopped with cancellation.");
            }

            this._httpClient.Dispose();
            this._queueSignal.Dispose();
            this._cts.Dispose();
        }

        private void Enqueue(TelemetryEnvelope envelope)
        {
            this._queue.Enqueue(envelope);
            this._queueSignal.Release();
        }

        private async Task ProcessQueueAsync()
        {
            try
            {
                while (!this._cts.IsCancellationRequested)
                {
                    await this._queueSignal.WaitAsync(this._cts.Token).ConfigureAwait(false);

                    while (this._queue.TryDequeue(out var envelope))
                    {
                        await this.SendWithRetryAsync(envelope, this._cts.Token).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when disposing.
            }
        }

        private async Task SendWithRetryAsync(TelemetryEnvelope envelope, CancellationToken cancellationToken)
        {
            var delay = this._initialBackoff;

            for (var attempt = 1; attempt <= this._maxRetries && !cancellationToken.IsCancellationRequested; attempt++)
            {
                try
                {
                    using var response = await this._httpClient.PostAsJsonAsync(envelope.Path, envelope.Payload, cancellationToken).ConfigureAwait(false);

                    if (response.IsSuccessStatusCode)
                    {
                        return;
                    }

                    PluginLog.Warning($"Telemetry send failed ({envelope.Description}) with status {(Int32)response.StatusCode} on attempt {attempt}.");
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    PluginLog.Warning(ex, $"Telemetry send exception ({envelope.Description}) on attempt {attempt}.");
                }

                if (attempt < this._maxRetries)
                {
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    delay = TimeSpan.FromMilliseconds(Math.Min(delay.TotalMilliseconds * 2, 4000));
                }
            }

            PluginLog.Error($"Telemetry send dropped after {this._maxRetries} attempts ({envelope.Description}).");
        }

        private readonly struct TelemetryEnvelope
        {
            public TelemetryEnvelope(String path, Object payload, String description)
            {
                this.Path = path;
                this.Payload = payload;
                this.Description = description;
            }

            public String Path { get; }

            public Object Payload { get; }

            public String Description { get; }
        }
    }
}
