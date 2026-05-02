# ACT Discord Triggers
[![Build Plugin](https://github.com/Makar8000/ACT-Discord-Triggers/actions/workflows/ci.yml/badge.svg)](https://github.com/Makar8000/ACT-Discord-Triggers/actions/workflows/ci.yml)

An ACT plugin for using Custom Triggers and/or Triggernometry with Discord bots.

## Download
See the [releases](https://github.com/Makar8000/ACT-Discord-Triggers/releases)

## Setup
See the [wiki](https://github.com/Makar8000/ACT-Discord-Triggers/wiki/First-Time-Setup-Guide)

The release archive now contains additional files alongside the plugin DLL
(`node.exe`, `bundle.js`, `node_modules/`). Drop the whole folder next to
`ACT_DiscordTriggers.dll` — the plugin spawns the bridge from the same
directory.

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
                     node.exe + bundle.js (discord.js + @snazzah/davey)
                            ▲
                            │ named pipe (length-prefixed JSON frames)
                            ▼
                ACT_DiscordTriggers.dll IPC client
```

- **`ACT_DiscordTriggers`** (net48) — the ACT plugin: UI, settings, and the
  TTS/PlaySound delegate hooks.
- **`DiscordAPI`** (net48) — in-process IPC client. `BridgeProcess` spawns
  `node.exe bundle.js <pipe>` and reads `BRIDGE_READY` from stdout;
  `PipeClient` does length-prefixed JSON request/response over a Windows
  named pipe. When the plugin exits the OS closes the pipe handle and the
  bridge self-terminates on `socket.close` — no launcher / Job Object
  needed because there is only one child process to coordinate.
- **`DiscordBridge-node/src`** — the actual bridge: discord.js +
  `@snazzah/davey` for DAVE E2EE. Bundled with esbuild; native and
  path-tricking deps (`@snazzah/davey`, `opusscript`, `libsodium-wrappers`)
  ship as files in `dist/node_modules/` next to `node.exe`.
- **`Tests`** (net48, xUnit) — protocol unit tests, named-pipe IPC tests
  against a mock server, and integration tests that spawn the real built
  bridge.
- **`DiscordBridge-node/tests`** (tsx + node:test) — JS-side tests for
  protocol round-tripping, pipe-server framing/dispatch, and bridge
  lifecycle.

The wire protocol is defined once in `DiscordAPI/Protocol.cs` and mirrored
in `DiscordBridge-node/src/protocol.ts`. `ProtocolConstants.Version` gates
the Hello handshake so a stale bridge alongside a new plugin (or vice-versa)
fails fast.

## Building

One command from a clean clone produces `release/` with everything an end
user drops into ACT's plugins directory:

```
cd DiscordBridge-node && npm ci && cd ..
pwsh ./build.ps1
```

`build.ps1` builds the plugin (`dotnet build` — net48 reference assemblies
auto-restore via NuGet, Costura.Fody merges into a single DLL), type-checks
and bundles the bridge, copies `node.exe`, stages the external
`node_modules/`, runs a spawn self-test (asserts `BRIDGE_READY`), and
assembles `release/`.

For bridge-only iteration, the npm scripts in `DiscordBridge-node/` stay
useful: `npm run typecheck`, `npm run bundle`, `npm test`.

Tests:
```
dotnet test Tests/Tests.csproj            # C# (net48): protocol + IPC + integration
cd DiscordBridge-node && npm test         # JS (tsx + node:test)
```
Integration tests require the bridge to be built first (they spawn
`DiscordBridge-node/dist/node.exe` with `bundle.js`).

## Software Used
 * [Microsoft .NET Framework 4.8](https://dotnet.microsoft.com/download/dotnet-framework) (plugin + tests)
 * [Node.js 22+](https://nodejs.org/) (bridge runtime)
 * [NAudio](https://github.com/naudio/NAudio) (PCM resampling in plugin)
 * [discord.js](https://github.com/discordjs/discord.js) + [@discordjs/voice](https://github.com/discordjs/voice) (bridge)
 * [@snazzah/davey](https://github.com/snazzah/davey) (DAVE E2EE)
