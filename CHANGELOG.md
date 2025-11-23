<!-- Keep a Changelog guide -> https://keepachangelog.com -->

# logigit Changelog

## [Unreleased]
### Added
- Rebranded both IntelliJ and Logitech plugins to LogiGit, retiring all Foto* artifacts.
- Logitech Actions SDK plugin now exposes Git buttons (stash, stash pop, reset `HEAD~1`, push, pull, status, checkout, log), IDE scroll dial, and MX Master 4 Actions Ring gestures wired to Git shortcuts with IPC + telemetry.
- IntelliJ plugin ships `LogiGitTelemetry`, `LogiGitIpcService`, and `LogiGitCommandExecutor` to ingest Logitech intents, enforce repo guardrails, and emit structured logging.
- Shared IPC schema (Unix domain sockets, Named Pipes, TCP fallback) plus correlation-ID telemetry for every handshake, dial tick, and gesture.
- README updated with architecture diagrams-in-text, hardware mapping matrix, guardrails, and manual validation checklist.
