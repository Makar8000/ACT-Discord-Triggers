# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & test

Top-level release build (produces `release/ACT_DiscordTriggers/` with everything
an end user drops into ACT's plugins directory):
```
cd DiscordBridge-node && npm ci && cd ..
pwsh ./build.ps1            # add -Zip to also emit ACT_DiscordTriggers.zip at the repo root
```
`./build.ps1` (root) runs `dotnet build` on the plugin (net48 — ACT only loads net48 assemblies; Costura.Fody weaves under the SDK's MSBuild and produces a single merged DLL, so no separate Visual Studio install is needed), then `tsc --noEmit` as a type-check gate, then bundles TS via esbuild, copies `node.exe` from the current PATH, stages externals into `DiscordBridge-node/dist/node_modules/`, spawns `node.exe bundle.js <pipe>` and asserts `BRIDGE_READY` appears on stdout, then assembles `release/ACT_DiscordTriggers/` from the plugin DLL + bridge dist (plus `README.md`/`LICENSE`). The self-test catches packaging regressions; **do not skip or weaken it**. With `-Zip` it then compresses the wrapper folder to `ACT_DiscordTriggers.zip` so the archive's top-level entry is `ACT_DiscordTriggers\` — extracting into `Plugins\` yields a self-contained `Plugins\ACT_DiscordTriggers\` subfolder rather than loose files in ACT's plugin root.

For bridge-only iteration use the npm scripts in `DiscordBridge-node/`:
- `npm run typecheck` — `tsc --noEmit`
- `npm run bundle` — esbuild only (no node.exe staging or self-test)
- `npm test` — JS test suite (covers protocol invariants, frame parsing, op dispatch against a `FakeHost`/`FakeSocket`, and a Windows-only lifecycle suite that spawns `tsx src/bridge.ts` and exercises the real handshake/Shutdown/peer-disconnect paths). Independent of the build — does not need `dist/`. Lifecycle tests no-op (skip) on non-Windows because the bridge uses Windows named pipes.

C# tests (net48, xUnit):
```
dotnet test Tests/Tests.csproj
```
The Tests project targets net48 to match the runtime the plugin ships on. Pinned to xUnit 2.9.x + xunit.runner.visualstudio 2.8.x because the 3.x line dropped Framework support; `Tests/xunit.runner.json` disables shadow-copy so `Assembly.Location` reflects the original output dir (the test assembly's `FindBridgeDir()` walks up from there). A small `TaskExtensions.cs` polyfills `Task.WaitAsync(TimeSpan)` since that overload is net6+.

Integration tests in `Tests/BridgeIntegrationTests.cs` spawn the real bridge from `dist/`. They fail with a clear "build the bridge first" message if `node.exe` / `bundle.js` aren't there — run `build.ps1` before `dotnet test`. Run a single test with `dotnet test --filter "FullyQualifiedName~Bridge_handshake_succeeds_against_real_exe"`.

CI (`.github/workflows/ci.yml`) downloads ACT binaries to `packages/` so the plugin's `Advanced Combat Tracker.exe` reference resolves; reproduce locally by either installing ACT to `C:\Program Files (x86)\Advanced Combat Tracker\` (the csproj falls back to that path) or by copying `Advanced Combat Tracker.exe` to `packages/`. CI runs entirely on `msbuild` + `node` — no .NET SDK setup step is needed beyond what `windows-latest` ships.

## Architecture: two processes

Discord enforced DAVE E2EE on voice in March 2026. Discord.Net 3.19 added DAVE but dropped net48; ACT only loads net48 plugins. The fix is to push voice out of the plugin process entirely:

```
ACT (net48) ─loads─▶ ACT_DiscordTriggers.dll (net48)
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
- **`DiscordAPI/`** (net48): in-process IPC client (`DiscordClient`, `BridgeProcess`, `PipeClient`, `Protocol`). `DiscordClient` is the static facade the plugin calls. `BridgeProcess.StartAndConnectAsync` takes the bridge directory, spawns `node.exe bundle.js <pipe>`, scans stdout for a line starting with `BRIDGE_READY`, then connects to the named pipe.
- **`DiscordBridge-node/src/`**: the actual bridge, written in TypeScript (esbuild bundles to `dist/bundle.js`; `tsc --noEmit` is the type-check gate). `bridge.ts` owns process lifecycle and the named-pipe server (one client, then `server.close()`). `pipe-server.ts` handles the framing + dispatch. `discord-host.ts` wraps discord.js + `@discordjs/voice` and feeds raw 48 kHz/16-bit/stereo PCM via `StreamType.Raw`. `protocol.ts` holds the wire-protocol types as a discriminated union — the JS-side mirror of `DiscordAPI/Protocol.cs`.
- **`Tests/`** (net48, xUnit): protocol round-tripping (`ProtocolTests`), pipe IPC against a mock server (`PipeIpcTests`), and `BridgeProcess` lifecycle (`BridgeProcessTests`) — all run without the real bridge. `BridgeIntegrationTests` spawns the actual built bridge.

### Lifecycle: no launcher, no Job Object

Earlier iterations of this code used a tiny C# launcher (`DiscordBridge.exe`) that spawned node and assigned it to a Win32 Job Object with `KILL_ON_JOB_CLOSE`, so a hard-killed launcher would also kill node. That guarantee only mattered because there were two child processes; with the plugin spawning `node.exe` directly there's a single process to coordinate:

- Plugin calls `process.Kill()` → `TerminateProcess` on node directly.
- Plugin process dies (clean exit, ACT crash, Task Manager) → OS closes the pipe client handle → `bridge.ts` `socket.close` handler runs `host.deinit()` then `process.exit(0)`.

Don't reintroduce a launcher unless you can show that **both** of those paths fail.

## Wire protocol — keep both sides in sync

The protocol is defined **twice**: once in `DiscordAPI/Protocol.cs` (C# DTOs + `Op` constants) and once in `DiscordBridge-node/src/protocol.ts` (TS discriminated union + `Op` table); `pipe-server.ts` consumes the latter for handler dispatch. Both sides also hold their own copy of `PROTOCOL_VERSION` (currently 1). When you add/change an op:

1. Update both `Protocol.cs` and `protocol.ts` (and the dispatch in `pipe-server.ts`).
2. If the wire shape changes incompatibly, bump `ProtocolConstants.Version` in `Protocol.cs` and `PROTOCOL_VERSION` in `protocol.ts`. The Hello handshake fails fast if the two disagree, which is what catches a stale bridge living next to a new plugin (or vice versa).
3. Add/extend both `ProtocolTests.cs` (C# side) and `tests/protocol.test.ts` (JS side).

Framing: little-endian uint32 length prefix, then UTF-8 JSON. Max frame is 64 MiB on both sides. Each request carries a `reqId`; the response echoes it. Notifications (`Log`, `BotReady`, `Disconnected`) have no `reqId` and are server-pushed.

`PipeClient.DispatchFrame` deliberately runs notification handlers on a thread-pool task, **not** on the read loop. `BotReady` calls back into `SendAsync` (to fetch servers/channels), and that response can only arrive once the read loop is back at `ReadFrameAsync`. Synchronous invocation here deadlocks. There's a comment marking this; preserve it if you refactor.

`PipeClient.SendFrameAsync` deliberately does **not** call `FlushAsync` — on Windows named pipes that's `FlushFileBuffers`, which blocks until the peer drains. `WriteAsync` already pushes into the OS pipe buffer.

## Bridge runtime: bundling, externals

Why plain `node.exe bundle.js` and not Node SEA: SEA's `embedderRequire` only resolves built-in modules and the bundled main script — external requires like `@snazzah/davey` (native `.node`), `opusscript` (uses `__dirname`), and `libsodium-wrappers` (loads its own WASM) cannot be resolved from a sibling `node_modules/`. Plain `node.exe bundle.js` walks `node_modules` normally, so we ship that pattern as-is.

`esbuild.config.mjs` `external:` list and `build.ps1` `$externals` list **must agree**. Anything marked external in esbuild has to be staged into `dist/node_modules/`, and anything staged has to be marked external (otherwise it's bundled and the staged copy is dead weight). The banner in `esbuild.config.mjs` injects the `dist/node_modules/` path into `NODE_PATH` so Node's CJS resolver finds the externals next to `node.exe`. If a bump breaks startup with `Cannot find module '<x>'`, audit both lists — npm hoists transitive deps and the externals list silently misses them.

esbuild is configured with `conditions: ['node', 'require']` so conditional-exports packages (`@discordjs/voice` is the one that bites) resolve via their CJS path. Without this, esbuild bundles the `.mjs` flavour, which contains `createRequire(import.meta.url)` — but `import.meta` becomes `{}` in CJS output and `createRequire(undefined)` throws on startup. Don't drop that condition.

Stdout discipline: `BRIDGE_READY pipe=<name>` is the **only** line the bridge writes to stdout that's expected. The plugin's `BridgeProcess.StartAndConnectAsync` reads stdout linewise with a 15 s deadline and treats anything else as a stderr-style log line. If you add stdout writes in node code, the handshake will hang until those happen to flush. Use `log.info`/`log.error` (writes to `DiscordBridge.log`) or stderr.

## Audio format constraint

Discord voice in this project is hard-wired to 48 kHz / 16-bit signed / stereo PCM end-to-end. `DiscordClient.formatInfo`, the NAudio resampler in `SpeakFile`, the `SpeakPcmRequest` defaults, and `discord-host.ts` `StreamType.Raw` all assume this. If you need to support another sample rate, change all four — don't add a conversion step in the bridge.

## Plugin packaging

The release archive shipped to users contains a single top-level `ACT_DiscordTriggers/` folder holding: `ACT_DiscordTriggers.dll`, `node.exe`, `bundle.js`, `node_modules/`, plus `README.md`/`LICENSE`. `DiscordPlugin.FindBridgeDir()` looks for `node.exe` + `bundle.js` next to the plugin DLL first, then falls back to ACT's `AppData\Plugins\Discord\`. Users extract the zip into ACT's `Plugins\` directory, yielding `Plugins\ACT_DiscordTriggers\`, then Browse to the DLL inside it (ACT supports plugin subfolders; loose DLLs in ACT's root are discouraged).

Releases are automated: `.github/workflows/release.yml` fires on a pushed `v*` tag, runs the full build/test, calls `build.ps1 -Zip`, renames the archive to `ACT_DiscordTriggers-<tag>.zip`, and publishes a GitHub Release (via `softprops/action-gh-release`) with install instructions + auto-generated notes. Tags containing `-` (e.g. `v2.0.0-pre.9`) are flagged as pre-releases. To cut a release: bump `AssemblyVersion`/`FileVersion`/`Version` in `ACT_DiscordTriggers.csproj` to match, then `git tag vX.Y.Z && git push origin vX.Y.Z`. `ci.yml` (on every push) runs the same build via `build.ps1 -Zip` and uploads the zip as a CI artifact for validation.

The plugin uses Costura.Fody to merge net48 dependencies into the DLL — stick to it if you add managed deps to `ACT_DiscordTriggers/` so the plugin remains a single file.
