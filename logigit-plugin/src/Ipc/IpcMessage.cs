namespace Loupedeck.LogiGitPlugin.Ipc
{
    using System;
    using System.Text.Json.Serialization;
    internal sealed class IpcMessage
    {
        [JsonPropertyName("schemaVersion")]
        public Int32 SchemaVersion { get; init; } = 1;
        [JsonPropertyName("type")]
        public String Type { get; init; }
        [JsonPropertyName("correlationId")]
        public String CorrelationId { get; init; } = Guid.NewGuid().ToString();
        [JsonPropertyName("payload")]
        public Object Payload { get; init; }
        [JsonPropertyName("error")]
        public IpcMessageError Error { get; init; }
    }
    internal sealed class IpcMessageError
    {
        [JsonPropertyName("code")]
        public String Code { get; init; }
        [JsonPropertyName("message")]
        public String Message { get; init; }
        [JsonPropertyName("details")]
        public Object Details { get; init; }
    }
}
