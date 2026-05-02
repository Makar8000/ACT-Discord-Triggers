# ACT Discord Triggers
[![Build Plugin](https://github.com/Makar8000/ACT-Discord-Triggers/actions/workflows/ci.yml/badge.svg)](https://github.com/Makar8000/ACT-Discord-Triggers/actions/workflows/ci.yml)

An ACT plugin for using Custom Triggers and/or Triggernometry with Discord bots.

## Download
See the [releases](https://github.com/Makar8000/ACT-Discord-Triggers/releases)

## Setup
See the [wiki](https://github.com/Makar8000/ACT-Discord-Triggers/wiki/First-Time-Setup-Guide)

The release archive now contains additional files alongside the plugin DLL
(`DiscordBridge.exe`, `node.exe`, `bundle.js`, `node_modules/`). Drop the
whole folder next to `ACT_DiscordTriggers.dll` — the plugin spawns the
bridge from the same directory.

## Why the rewrite (2026)

Discord enforced [DAVE](https://daveprotocol.com/) end-to-end encryption on
voice connections in March 2026. Bots that don't speak DAVE are now rejected
from voice channels with WebSocket close code `4017`.

[Discord.Net 3.19](https://github.com/discord-net/Discord.Net/releases/tag/3.19.1)
added DAVE support but dropped support for .NET Framework 4.8. ACT itself
runs on net48, so the plugin can't load a net8/net10 build of Discord.Net
in-process. The old in-process audio path is therefore unfixable.

## Architecture

To get DAVE without dropping ACT compatibility, voice now runs in a
separate process:

```
ACT (net48) ─loads─▶ ACT_DiscordTriggers.dll (net48)
                            │ spawns
                            ▼
                     DiscordBridge.exe (net10 self-contained shim)
                            │ spawns
                            ▼
                     node.exe + bundle.js (discord.js + @snazzah/davey)
                            ▲
                            │ named pipe (length-prefixed JSON frames)
                            ▼
                ACT_DiscordTriggers.dll IPC client
```

- **`ACT_DiscordTriggers`** (net48) — the ACT plugin: UI, settings, and the
  TTS/PlaySound delegate hooks.
- **`DiscordAPI`** (net48) — in-process IPC client. `BridgeProcess` spawns
  the bridge exe and reads `BRIDGE_READY` from stdout; `PipeClient` does
  length-prefixed JSON request/response over a Windows named pipe.
- **`DiscordBridge-node/launcher`** (net10 self-contained, single file) —
  a tiny C# launcher that spawns `node.exe bundle.js`. Wraps the child in
  a Win32 Job Object with `KILL_ON_JOB_CLOSE` so node dies if the launcher
  is hard-killed (e.g. ACT crash, Task Manager kill).
- **`DiscordBridge-node/src`** — the actual bridge: discord.js +
  `@snazzah/davey` for DAVE E2EE. Bundled with esbuild; native and
  path-tricking deps (`@snazzah/davey`, `opusscript`, `libsodium-wrappers`)
  ship as files in `dist/node_modules/` next to the exe.
- **`Tests`** (net10, xUnit) — protocol unit tests, named-pipe IPC tests
  against a mock server, and integration tests that spawn the real built
  bridge. CI runs all three.

The wire protocol is defined once in `DiscordAPI/Protocol.cs` and mirrored
in `DiscordBridge-node/src/pipe-server.js`. `ProtocolConstants.Version`
gates the Hello handshake so a stale bridge alongside a new plugin (or
vice-versa) fails fast.

## Building

Plugin (net48):
```
msbuild ACT_DiscordTriggers/ACT_DiscordTriggers.csproj -p:Configuration=Release
```

Bridge (Node.js 22+, .NET 10 SDK):
```
cd DiscordBridge-node
npm ci
pwsh ./build.ps1
```

`build.ps1` bundles the JS, publishes the launcher, copies `node.exe`,
stages the external `node_modules`, and runs a self-test that spawns the
bridge and waits for `BRIDGE_READY`.

Tests:
```
dotnet test Tests/Tests.csproj
```
Integration tests require the bridge to be built first (they spawn
`DiscordBridge-node/dist/DiscordBridge.exe`).

## Software Used
 * [Microsoft .NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework) (plugin)
 * [.NET 10](https://dotnet.microsoft.com/download/dotnet/10.0) (launcher, tests)
 * [Node.js 22+](https://nodejs.org/) (bridge runtime)
 * [NAudio](https://github.com/naudio/NAudio) (PCM resampling in plugin)
 * [discord.js](https://github.com/discordjs/discord.js) + [@discordjs/voice](https://github.com/discordjs/voice) (bridge)
 * [@snazzah/davey](https://github.com/snazzah/davey) (DAVE E2EE)
