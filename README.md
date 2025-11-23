# LogiGit

![Build](https://github.com/keanucz/logigit/workflows/Build/badge.svg)

LogiGit pairs the IntelliJ Platform with Logitech Actions-enabled devices (MX Master 4, Loupedeck CT/Live family) to drive guarded Git automation. Logitech buttons, dials, and MX Actions Ring gestures emit structured IPC events; the IntelliJ plugin enforces guardrails, runs Git APIs, and reports telemetry.

<!-- Plugin description -->
LogiGit bridges Logitech hardware and the IntelliJ Git stack. Logitech buttons fire curated Git intents (stash/push/reset/etc.), the central dial scrolls the editor, and the MX Actions Ring triggers contextual shortcuts. A cross-platform IPC layer (Unix sockets, Named Pipes, TCP fallback) keeps both plugins in sync, with telemetry covering every handshake, gesture, and Git outcome.
<!-- Plugin description end -->

## Architecture

- **Logitech Actions SDK plugin (`logigit-plugin/src`)**
  - Actions: `git.stash`, `git.stash.pop`, `git.reset.head`, `git.push`, `git.pull`, `git.status`, `git.checkout`, `git.log`.
  - Dial adjustment: `EditorScrollAdjustment` streams `dial.scroll` events.
  - MX Master 4 Actions Ring gestures: `ring.up`→pull, `ring.down`→push, `ring.left`→status, `ring.right`→log.
  - Shared IPC client selects Unix domain sockets (macOS/Linux), Named Pipes (Windows), TCP fallback, and emits telemetry via `HttpTelemetryClient`.
- **IntelliJ plugin (`src/main/kotlin/com/github/keanucz/logigit`)**
  - `LogiGitIpcService` hosts a loopback TCP listener (matching the Logitech fallback) with reconnection/backoff logging.
  - `LogiGitCommandExecutor` validates repo cleanliness before destructive commands, logs each Git intent, and will integrate with IntelliJ Git APIs/editor events.
  - `LogiGitTelemetry` standardizes IDE-side telemetry/log messages.
- **Telemetry & Logging**
  - Every command, dial tick, and gesture carries a correlation ID.
  - Errors (IPC connect failure, rejected Git command) surface both in device logs and IDE telemetry for diagnosis.

## Hardware Mappings

| Hardware | Action | Logitech Intent |
| --- | --- | --- |
| Any button (LogiGit deck) | Git shortcuts | `git.stash`, `git.stash.pop`, `git.reset.head`, `git.push`, `git.pull`, `git.status`, `git.checkout`, `git.log` |
| Center dial | Scroll active editor | `dial.scroll` (tick diff + timestamp) |
| MX Master 4 Actions Ring | Ring Up = Pull, Down = Push, Left = Status, Right = Log | `gesture` payload with mapped command |

Icons live in `logigit-plugin/src/Resources`, with metadata wired under `logigit-plugin/src/package/metadata/LoupedeckPackage.yaml`.

## Guardrails & Security

- Destructive commands (`reset`, `push`, `stash pop`, `checkout`) require a clean working tree before the IDE acknowledges them.
- IPC payloads include schema version, correlation ID, and contextual payloads; invalid payloads are logged and ignored.
- Telemetry provides trace coverage for IPC lifecycle, Git intents, dial deltas, gesture mappings, and error paths.

## Build & Test

```bash
./gradlew clean build
cd logigit-plugin && dotnet build src/LogiGitPlugin.csproj
```

(Gradle builds the IntelliJ plugin; `dotnet` builds the Logitech plugin. Run from the repo root.)

## Manual Validation Checklist

1. **IPC Handshake**: Ensure the IntelliJ plugin starts (`idea.log` shows `LogiGit IPC listening`). Launch Logitech Logi Options+/Loupedeck to load the Logitech plugin; confirm connect/disconnect events in both logs.
2. **Git Intents**: Press each Logitech button. IntelliJ log should show `git.intent.accepted` or a rejection reason if the repo is dirty.
3. **Dial Scroll**: Rotate the assigned dial; verify `dial.scroll` entries in logs and that the editor scrolls (scroll integration placeholder currently logs intent).
4. **Gesture Flow**: Trigger each MX Actions Ring gesture; Logitech and IntelliJ logs must show `gesture` payloads with the mapped Git command.
5. **Telemetry**: Inspect local telemetry endpoint/logs for `ipc.*`, `git.*`, `dial.scroll`, and `gesture.received` events with consistent correlation IDs.
6. **Safety Checks**: Dirty the repo and press a destructive command (`git.push`, `git.reset.head`). Expect `git.intent.denied` telemetry.

## Troubleshooting

- If the IntelliJ plugin starts before the Logitech side, the Logitech IPC client retries with exponential backoff.
- Override telemetry endpoint with `LOGIGIT_PLUGIN_TELEMETRY_URL` env var for device-side diagnostics.
- Use `logigit-plugin/src/Helpers/PluginLog.cs` and IntelliJ `idea.log` for bidirectional tracing.
