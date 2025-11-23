Refactor and extend both the IntelliJ LogiGit plugin (under `src/main/kotlin/com/github/keanucz/logigit`) and the Logitech Actions SDK plugin (under `logigit-plugin/`) so the project moves from the legacy "Foto" naming to the new "LogiGit" branding and supports coordinated Git automation across Logitech hardware.

Context Summary
1. All `Foto*` artifacts inside `logigit-plugin` (solution files, `src/Fot*.cs`, resource folders, package metadata, namespaces) must be renamed to `LogiGitPlugin`. Align the IntelliJ plugin identifiers in `gradle.properties`, `src/main/resources/META-INF/plugin.xml`, Kotlin services (e.g., `MyProjectService`), and any user-facing names with the new branding.
2. The Logitech plugin must reuse patterns from `logigit-plugin/DemoPlugin/**` and align with documentation in `logigit-plugin/LogiActionSDK_agent_doc/**`. Use these references for action definitions, metadata packaging, resource layout, and best practices.
3. Establish a shared IPC abstraction with runtime backend selection: prefer Unix domain sockets on macOS/Linux, Named Pipes on Windows, and only fall back to loopback TCP if the primary transport is unavailable. Ensure both plugins can send/receive Git action events through this layer. Reference `logigit-plugin/src/Helpers/HttpTelemetryClient.cs` (or analogous helpers) for message/telemetry conventions.
4. Implement a suite of Git shortcut actions (stash, stash pop, reset `HEAD~1`, push, pull, status, branch checkout, log) mapped to Logitech buttons with matching icons/metadata in `logigit-plugin/src/Resources` and `package/metadata`. Reuse DemoPlugin action scaffolding (`ButtonSwitchesCommand.cs`, `ToggleMuteCommand.cs`, etc.) for structure.
5. Add optional dial-based adjustments so a knob can control IDE scrolling. Ensure the IntelliJ plugin exposes/consumes events accordingly.
6. Integrate MX Master 4 Actions Ring gestures per `logigit-plugin/LogiActionSDK_agent_doc/03-Advanced-Features/01-advanced-capabilities.md`, mapping gestures to Git shortcuts and propagating events through the IPC layer.
7. Enforce logging and telemetry conventions across both plugins. Each IPC event, Git command intent, and hardware interaction should emit structured logs and telemetry (reusing existing helpers when possible).
8. Update documentation (`README.md`, `CHANGELOG.md`) and ensure tests or manual validation steps cover the new IPC flow and actions. Guard against destructive Git commands by requiring repository-state checks or confirmations before execution.

Guardrails & Expectations
- Preserve existing user changes; only modify files necessary for the rename and new functionality.
- Keep the IntelliJ plugin sample scaffolding (like `MyProjectService`) only if it still provides value; otherwise replace it with meaningful LogiGit logic.
- Design IPC schemas that are versioned and resilient; include reconnection/backoff strategies and authentication/authorization considerations if plugins run separately.
- Ensure cross-platform parity: macOS and Windows are the only supported Logitech SDK targets, but the IntelliJ plugin may run elsewhere. Provide fallbacks where Logitech SDK components are unavailable.
- Use consistent naming (`LogiGitPlugin`, `LogiGitAction`, etc.) across solutions, namespaces, resources, and telemetry identifiers.
- For Git actions that mutate state (reset, push, stash pop), add guardrails (confirm repository clean state, prompt user, or expose dry-run mode) to avoid accidental data loss.
- Document how the IntelliJ and Logitech sides discover each other, negotiate IPC channels, and exchange payloads. Include diagrams or text descriptions if needed.
- After implementation, verify lint/tests/builds, or describe manual validation steps if automated coverage is unavailable.

Checklist for GPT-5.1-Codex
1. Audit the repository for `Foto` references and replace them with `LogiGit` equivalents across `logigit-plugin/**` and IntelliJ files (`gradle.properties`, `plugin.xml`, Kotlin services, resources, telemetry identifiers).
2. Update solution/project filenames (`FotoPlugin.sln`, `FotoPlugin.csproj`, etc.), namespaces, and class names to `LogiGitPlugin`, along with resource directories and package metadata in `logigit-plugin/src/package/**`.
3. Review `logigit-plugin/DemoPlugin/**` and `LogiActionSDK_agent_doc/**`; extract patterns for action definitions, metadata, and advanced integrations to inform the new implementation.
4. Define and implement the shared IPC abstraction:
   - Interface/API used by both plugins.
   - Unix domain socket backend for macOS/Linux, Named Pipe backend for Windows, loopback TCP fallback.
   - Message schema (event type, command payload, correlation IDs, error codes).
   - Lifecycle management (startup sequencing, reconnection/backoff, cleanup) and security/validation checks.
   - Logging/telemetry hooks using existing helpers (e.g., `Helpers/HttpTelemetryClient`).
5. Extend the Logitech Actions SDK plugin:
   - Create button actions for Git stash, stash pop, reset `HEAD~1`, push, pull, status, branch checkout, and log.
   - Define icons/resources in `logigit-plugin/src/Resources` and metadata entries so Logitech software displays the new actions correctly.
   - Introduce a dial adjustment action that sends scroll events (or equivalent) to IntelliJ via IPC.
   - Implement MX Master 4 Actions Ring gestures referencing the advanced capabilities doc.
6. Extend the IntelliJ plugin:
   - Expose services/listeners capable of receiving Logitech events over the IPC channel and executing the corresponding Git commands (using IntelliJ Git APIs).
   - Ensure command execution respects guardrails (confirmation prompts, dry-run options, logging of results/errors).
   - Emit telemetry/logs for incoming events, execution outcomes, and IPC state.
7. Synchronize resource bundles, plugin descriptions (`README.md`, `plugin.xml`), and telemetry identifiers to reflect the new LogiGit branding and functionality.
8. Document the IPC setup, Git actions, gesture mappings, and testing steps in `README.md` and `CHANGELOG.md`. Include instructions for manually verifying Logitech ↔ IntelliJ communication.
9. Run or outline relevant tests/lint/build commands for both the IntelliJ plugin and Logitech plugin. If automated tests are missing, specify manual validation steps and future test additions.
10. Review the entire codebase for consistency, ensuring no lingering `Foto` references or sample template artifacts remain unless intentionally kept.

