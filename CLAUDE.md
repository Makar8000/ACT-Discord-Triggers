# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & test

Plugin (net48, MSBuild — ACT only loads net48 assemblies):
```
msbuild ACT_DiscordTriggers/ACT_DiscordTriggers.csproj -p:Configuration=Release
```

Bridge (Node.js 22+, .NET 10 SDK; produces `DiscordBridge-node/dist/`):
```
cd DiscordBridge-node
npm ci
pwsh ./build.ps1
```
`build.ps1` runs `tsc --noEmit` first as a type-check gate (fails fast on type errors before paying for bundle/publish), then bundles TS via esbuild, publishes the launcher (self-contained single-file, **not** AOT — see `launcher/launcher.csproj`), copies `node.exe` from the current PATH, stages externals into `dist/node_modules/`, then spawns the bridge and asserts `BRIDGE_READY` appears on stdout. The self-test catches packaging regressions; **do not skip or weaken it**.

Tests (net10, xUnit):
```
dotnet test Tests/Tests.csproj
```
Integration tests in `Tests/BridgeIntegrationTests.cs` spawn the real `dist/DiscordBridge.exe`. They fail with a clear "build the bridge first" message if `dist/` is missing — run `build.ps1` before `dotnet test`. Run a single test with `dotnet test --filter "FullyQualifiedName~Bridge_handshake_succeeds_against_real_exe"`.

CI (`.github/workflows/ci.yml`) downloads ACT binaries to `packages/` so the plugin's `Advanced Combat Tracker.exe` reference resolves; reproduce locally by either installing ACT to `C:\Program Files (x86)\Advanced Combat Tracker\` (the csproj falls back to that path) or by copying `Advanced Combat Tracker.exe` to `packages/`.

## Architecture: why three processes

Discord enforced DAVE E2EE on voice in March 2026. Discord.Net 3.19 added DAVE but dropped net48; ACT only loads net48 plugins. The fix is to push voice out of the plugin process entirely:

```
ACT (net48) ─loads─▶ ACT_DiscordTriggers.dll (net48)
                              │ spawns
                              ▼
                       DiscordBridge.exe (net10 single-file launcher)
                              │ spawns
                              ▼
                       node.exe + bundle.js (discord.js + @snazzah/davey)
                              ▲
                              │ Windows named pipe, length-prefixed JSON
                              ▼
                       ACT_DiscordTriggers.dll IPC client
```

Three projects, three jobs:

- **`ACT_DiscordTriggers/`** (net48): UI, settings, hooks `ActGlobals.oFormActMain.PlayTtsMethod` / `PlaySoundMethod`. TTS is synthesized in-process via `System.Speech` to 48 kHz/16-bit/stereo PCM, then shipped to the bridge as base64. Audio file playback is resampled with NAudio to the same format. The plugin **never** opens a Discord WebSocket itself — that's what broke under DAVE.
- **`DiscordAPI/`** (net48): in-process IPC client (`DiscordClient`, `BridgeProcess`, `PipeClient`, `Protocol`). `DiscordClient` is the static facade the plugin calls. `BridgeProcess.StartAndConnectAsync` spawns `DiscordBridge.exe`, scans stdout for a line starting with `BRIDGE_READY`, then connects to the named pipe.
- **`DiscordBridge-node/launcher/`** (net10, single-file self-contained): a tiny C# shim. Spawns `node.exe bundle.js` with the original argv, inherits stdio, and assigns the child to a Win32 Job Object with `KILL_ON_JOB_CLOSE`. The job object is the **only** thing that kills node when ACT or the launcher dies via `TerminateProcess` (ProcessExit handlers don't fire on hard kill). `Program.cs` has the rationale in a header comment — read it before changing the lifecycle.
- **`DiscordBridge-node/src/`**: the actual bridge, written in TypeScript (esbuild bundles to `dist/bundle.js`; `tsc --noEmit` is the type-check gate). `bridge.ts` owns process lifecycle and the named-pipe server (one client, then `server.close()`). `pipe-server.ts` handles the framing + dispatch. `discord-host.ts` wraps discord.js + `@discordjs/voice` and feeds raw 48 kHz/16-bit/stereo PCM via `StreamType.Raw`. `protocol.ts` holds the wire-protocol types as a discriminated union — the JS-side mirror of `DiscordAPI/Protocol.cs`.
- **`Tests/`** (net10, xUnit): protocol round-tripping (`ProtocolTests`), pipe IPC against a mock server (`PipeIpcTests`), and `BridgeProcess` lifecycle (`BridgeProcessTests`) — all run without the real bridge. `BridgeIntegrationTests` spawns the actual built exe.

## Wire protocol — keep both sides in sync

The protocol is defined **twice**: once in `DiscordAPI/Protocol.cs` (C# DTOs + `Op` constants) and once in `DiscordBridge-node/src/protocol.ts` (TS discriminated union + `Op` table); `pipe-server.ts` consumes the latter for handler dispatch. Both sides also hold their own copy of `PROTOCOL_VERSION` (currently 1). When you add/change an op:

1. Update both `Protocol.cs` and `protocol.ts` (and the dispatch in `pipe-server.ts`).
2. If the wire shape changes incompatibly, bump `ProtocolConstants.Version` in `Protocol.cs` and `PROTOCOL_VERSION` in `protocol.ts`. The Hello handshake fails fast if the two disagree, which is what catches a stale bridge living next to a new plugin (or vice versa).
3. Add/extend `ProtocolTests.cs`.

Framing: little-endian uint32 length prefix, then UTF-8 JSON. Max frame is 64 MiB on both sides. Each request carries a `reqId`; the response echoes it. Notifications (`Log`, `BotReady`, `Disconnected`) have no `reqId` and are server-pushed.

`PipeClient.DispatchFrame` deliberately runs notification handlers on a thread-pool task, **not** on the read loop. `BotReady` calls back into `SendAsync` (to fetch servers/channels), and that response can only arrive once the read loop is back at `ReadFrameAsync`. Synchronous invocation here deadlocks. There's a comment marking this; preserve it if you refactor.

`PipeClient.SendFrameAsync` deliberately does **not** call `FlushAsync` — on Windows named pipes that's `FlushFileBuffers`, which blocks until the peer drains. `WriteAsync` already pushes into the OS pipe buffer.

## Bridge runtime: bundling, externals, lifecycle

Why a launcher and not Node SEA: SEA's `embedderRequire` only resolves built-in modules and the bundled main script — external requires like `@snazzah/davey` (native `.node`), `opusscript` (uses `__dirname`), and `libsodium-wrappers` (loads its own WASM) cannot be resolved from a sibling `node_modules/`. Plain `node.exe bundle.js` works, so the launcher's only job is to wrap that invocation in a Win32 Job Object.

`esbuild.config.mjs` `external:` list and `build.ps1` `$externals` list **must agree**. Anything marked external in esbuild has to be staged into `dist/node_modules/`, and anything staged has to be marked external (otherwise it's bundled and the staged copy is dead weight). The banner in `esbuild.config.mjs` injects the `dist/node_modules/` path into `NODE_PATH` at runtime so Node's CJS resolver finds the externals next to `node.exe`. If a bump breaks startup with `Cannot find module '<x>'`, audit both lists — npm hoists transitive deps and the externals list silently misses them.

esbuild is configured with `conditions: ['node', 'require']` so conditional-exports packages (`@discordjs/voice` is the one that bites) resolve via their CJS path. Without this, esbuild bundles the `.mjs` flavour, which contains `createRequire(import.meta.url)` — but `import.meta` becomes `{}` in CJS output and `createRequire(undefined)` throws on startup. Don't drop that condition.

Stdout discipline: `BRIDGE_READY pipe=<name>` is the **only** line the launcher/bridge writes to stdout that's expected. The plugin's `BridgeProcess.StartAndConnectAsync` reads stdout linewise with a 15 s deadline and treats anything else as a stderr-style log line. If you add stdout writes in node code, the handshake will hang until those happen to flush. Use `log.info`/`log.error` (writes to `DiscordBridge.log`) or stderr.

Bridge exits when its single named-pipe client disconnects (`bridge.ts` socket `close`/`error` → `host.deinit()` → `process.exit(0)`). This mirrors the .NET-side `ReadFrameAsync` returning null, so a crashed plugin doesn't leave an orphan node.exe holding the pipe.

## Audio format constraint

Discord voice in this project is hard-wired to 48 kHz / 16-bit signed / stereo PCM end-to-end. `DiscordClient.formatInfo`, the NAudio resampler in `SpeakFile`, the `SpeakPcmRequest` defaults, and `discord-host.ts` `StreamType.Raw` all assume this. If you need to support another sample rate, change all four — don't add a conversion step in the bridge.

## Plugin packaging

The release archive shipped to users contains **all** of: `ACT_DiscordTriggers.dll`, `DiscordBridge.exe`, `node.exe`, `bundle.js`, and `node_modules/`. `DiscordPlugin.FindBridgePath()` looks for `DiscordBridge.exe` next to the plugin DLL first, then falls back to ACT's `AppData\Plugins\Discord\`. Users drop the whole folder into ACT's plugins directory.

The plugin uses Costura.Fody to merge net48 dependencies into the DLL — stick to it if you add managed deps to `ACT_DiscordTriggers/` so the plugin remains a single file.
