using DiscordBridge.Protocol;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordAPI {
    public static class DiscordClient {
        private static PipeClient pipeClient;
        private static BridgeProcess bridge;
        private static string bridgeDir;
        private static readonly object lifecycleLock = new object();
        private static int initInProgress;

        private static readonly SpeechAudioFormatInfo formatInfo =
            new SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo);

        // Random sound-effects feature. The plugin mirrors its UI (checkbox + slider)
        // into these fields; we roll the dice here so the trigger thread never has to
        // touch WinForms controls cross-thread. The bridge owns the effect catalog and
        // picks which effect + params — we only send the "apply one" bit.
        public static volatile bool RandomEffectsEnabled;
        private static int randomEffectChance; // 0-100
        public static int RandomEffectChance {
            get { return randomEffectChance; }
            set { randomEffectChance = value < 0 ? 0 : (value > 100 ? 100 : value); }
        }
        private static readonly Random fxRandom = new Random();

        // Auto-leveling (loudness normalization). Like the FX fields, the plugin
        // mirrors its UI here. Unlike FX, this is global config the bridge applies
        // to every clip — so we push it to the bridge on connect and whenever it
        // changes, rather than tagging each trigger. Defaults match the bridge's
        // own defaults (on, -20 dBFS) so behavior is consistent before the first push.
        public static volatile bool NormalizeEnabled = true;
        private static int normalizeTargetDb = -20;
        public static int NormalizeTargetDb {
            get { return normalizeTargetDb; }
            set { normalizeTargetDb = value < -60 ? -60 : (value > 0 ? 0 : value); }
        }

        private static bool RollEffect() {
            if (!RandomEffectsEnabled) return false;
            int chance = randomEffectChance;
            if (chance <= 0) return false;
            if (chance >= 100) return true;
            lock (fxRandom) {
                return fxRandom.Next(100) < chance;
            }
        }

        public delegate void BotLoaded();
        public static event BotLoaded BotReady;

        public delegate void BotMessage(string message);
        public static event BotMessage Log;

        public static void SetBridgePath(string dir) {
            bridgeDir = dir;
        }

        public static async Task InitAsync(string logintoken, string botstatus) {
            // Race guard: a fast double-click on Connect would otherwise spawn two
            // node.exe processes since the long async work below runs unlocked.
            if (Interlocked.CompareExchange(ref initInProgress, 1, 0) != 0) {
                Log?.Invoke("Initialization already in progress.");
                return;
            }
            try {
                lock (lifecycleLock) {
                    if (pipeClient != null) {
                        Log?.Invoke("Already initialized.");
                        return;
                    }
                }
                if (string.IsNullOrEmpty(bridgeDir)) {
                    Log?.Invoke("Bridge directory not configured. Internal error.");
                    return;
                }

                BridgeProcess localBridge = new BridgeProcess();
                localBridge.OnStderr += msg => Log?.Invoke("[bridge] " + msg);
                localBridge.OnExited += code => {
                    Log?.Invoke($"Bridge process exited (code {code}).");
                    CleanupAfterPipeBroken();
                };

                NamedPipeClientStream pipe;
                try {
                    pipe = await localBridge.StartAndConnectAsync(bridgeDir);
                } catch (Exception ex) {
                    Log?.Invoke("Failed to start bridge: " + ex.Message);
                    try { localBridge.Dispose(); } catch { }
                    return;
                }

                PipeClient localClient = new PipeClient(pipe);
                localClient.OnLog += (msg, lvl) => Log?.Invoke(msg);
                localClient.OnBotReady += () => BotReady?.Invoke();
                localClient.OnDisconnected += reason => Log?.Invoke("Discord disconnected: " + reason);
                localClient.OnPipeBroken += reason => {
                    Log?.Invoke("Bridge connection lost: " + reason);
                    CleanupAfterPipeBroken();
                };
                localClient.Start();

                lock (lifecycleLock) {
                    bridge = localBridge;
                    pipeClient = localClient;
                }

                HelloResponse hello;
                try {
                    hello = await localClient.SendAsync<HelloResponse>(
                        new HelloRequest { ProtocolVersion = ProtocolConstants.Version },
                        TimeSpan.FromSeconds(10));
                } catch (Exception ex) {
                    Log?.Invoke("Bridge handshake error: " + ex.Message);
                    CleanupAfterPipeBroken();
                    return;
                }
                if (!hello.Ok) {
                    Log?.Invoke("Bridge handshake failed: " + hello.Error);
                    CleanupAfterPipeBroken();
                    return;
                }

                try {
                    var init = await localClient.SendAsync<OkResponse>(
                        new InitRequest { Token = logintoken, Status = botstatus },
                        TimeSpan.FromSeconds(20));
                    if (!init.Ok) {
                        Log?.Invoke("Discord login failed: " + init.Error);
                    }
                } catch (Exception ex) {
                    Log?.Invoke("Discord login error: " + ex.Message);
                }

                // Push current auto-leveling config so a fresh bridge matches the UI.
                await PushNormalizationAsync(localClient);
            } catch (Exception ex) {
                Log?.Invoke("Init error: " + ex.Message);
            } finally {
                Interlocked.Exchange(ref initInProgress, 0);
            }
        }

        public static async Task DeinitAsync() {
            PipeClient localClient;
            BridgeProcess localBridge;
            lock (lifecycleLock) {
                localClient = pipeClient;
                localBridge = bridge;
                pipeClient = null;
                bridge = null;
            }
            if (localClient == null && localBridge == null) return;

            try {
                if (localClient != null) {
                    try {
                        await localClient.SendAsync<OkResponse>(
                            new ShutdownRequest(), TimeSpan.FromSeconds(3));
                    } catch { }
                }
                if (localBridge != null) {
                    await localBridge.WaitForExitAsync(TimeSpan.FromSeconds(3));
                    if (!localBridge.HasExited) {
                        localBridge.Kill();
                    }
                }
            } finally {
                try { localClient?.Dispose(); } catch { }
                try { localBridge?.Dispose(); } catch { }
            }
        }

        private static void CleanupAfterPipeBroken() {
            PipeClient localClient;
            BridgeProcess localBridge;
            lock (lifecycleLock) {
                localClient = pipeClient;
                localBridge = bridge;
                pipeClient = null;
                bridge = null;
            }
            try { localClient?.Dispose(); } catch { }
            try { localBridge?.Dispose(); } catch { }
        }

        public static async Task<bool> IsConnectedAsync() {
            var pc = pipeClient;
            if (pc == null) return false;
            try {
                var resp = await pc.SendAsync<IsConnectedResponse>(
                    new IsConnectedRequest(), TimeSpan.FromSeconds(3));
                return resp.Connected;
            } catch {
                return false;
            }
        }

        public static async Task<string[]> GetServersAsync() {
            var pc = pipeClient;
            if (pc == null) return new string[0];
            try {
                var resp = await pc.SendAsync<GetServersResponse>(
                    new GetServersRequest());
                return resp.Servers ?? new string[0];
            } catch (Exception ex) {
                Log?.Invoke("GetServersAsync failed: " + ex.Message);
                return new string[0];
            }
        }

        public static async Task<string[]> GetChannelsAsync(string server) {
            var pc = pipeClient;
            if (pc == null) return new string[0];
            try {
                var resp = await pc.SendAsync<GetChannelsResponse>(
                    new GetChannelsRequest { Server = server });
                return resp.Channels ?? new string[0];
            } catch (Exception ex) {
                Log?.Invoke("GetChannelsAsync failed: " + ex.Message);
                return new string[0];
            }
        }

        // Push auto-leveling config to the bridge. Called on connect (with the
        // freshly-created client) and from the UI whenever the user toggles the
        // checkbox or moves the target slider. No-op when not connected — connect
        // re-pushes the current values anyway.
        public static Task SetNormalizationAsync() {
            return PushNormalizationAsync(pipeClient);
        }

        private static async Task PushNormalizationAsync(PipeClient pc) {
            if (pc == null) return;
            try {
                await pc.SendAsync<OkResponse>(
                    new SetNormalizationRequest { Enabled = NormalizeEnabled, TargetDb = NormalizeTargetDb },
                    TimeSpan.FromSeconds(5));
            } catch (Exception ex) {
                Log?.Invoke("SetNormalization failed: " + ex.Message);
            }
        }

        public static async Task SetGameAsync(string text) {
            var pc = pipeClient;
            if (pc == null) return;
            try {
                await pc.SendAsync<OkResponse>(new SetGameRequest { Text = text ?? "" });
            } catch (Exception ex) {
                Log?.Invoke("SetGameAsync failed: " + ex.Message);
            }
        }

        public static async Task<bool> JoinChannel(string server, string channel) {
            var pc = pipeClient;
            if (pc == null) {
                Log?.Invoke("Cannot join channel: bridge not connected.");
                return false;
            }
            try {
                var resp = await pc.SendAsync<JoinChannelResponse>(
                    new JoinChannelRequest { Server = server, Channel = channel },
                    TimeSpan.FromSeconds(15));
                if (!resp.Ok && !string.IsNullOrEmpty(resp.Error)) {
                    Log?.Invoke("JoinChannel failed: " + resp.Error);
                }
                return resp.Ok;
            } catch (Exception ex) {
                Log?.Invoke("JoinChannel error: " + ex.Message);
                return false;
            }
        }

        public static async Task LeaveChannelAsync() {
            var pc = pipeClient;
            if (pc == null) return;
            try {
                await pc.SendAsync<OkResponse>(
                    new LeaveChannelRequest(), TimeSpan.FromSeconds(10));
            } catch (Exception ex) {
                Log?.Invoke("LeaveChannelAsync failed: " + ex.Message);
            }
        }

        public static void Speak(string text, string voice, int vol, int speed) {
            // Called from ACT's PlayTtsMethod hook on a background thread, not the UI.
            // Synthesis itself is sync; downstream IPC blocks the trigger thread by design.
            var swSynth = Stopwatch.StartNew();
            byte[] pcm;
            try {
                using (var tts = new SpeechSynthesizer())
                using (var ms = new MemoryStream()) {
                    tts.SelectVoice(voice);
                    tts.Volume = vol * 5;
                    tts.Rate = speed - 10;
                    tts.SetOutputToAudioStream(ms, formatInfo);
                    tts.Speak(text);
                    pcm = ms.ToArray();
                }
            } catch (Exception ex) {
                Log?.Invoke("TTS synthesis failed: " + ex.Message);
                return;
            }
            swSynth.Stop();
            bool fx = RollEffect();
            var swIpc = Stopwatch.StartNew();
            SendSpeakPcm(pcm, fx);
            swIpc.Stop();
            Log?.Invoke($"Speak timing: synth={swSynth.ElapsedMilliseconds}ms ipc={swIpc.ElapsedMilliseconds}ms bytes={pcm.Length} fx={fx}");
        }

        public static void SpeakFile(string path) {
            // Called from ACT's PlaySoundMethod hook (signature: void(string,int)) on
            // a background thread. The single sync-over-async boundary lives here.
            try {
                Task.Run(() => SpeakFileAsync(path)).GetAwaiter().GetResult();
            } catch (Exception ex) {
                Log?.Invoke("SpeakFile error: " + ex.Message);
            }
        }

        private static async Task SpeakFileAsync(string path) {
            var pc = pipeClient;
            if (pc == null) {
                Log?.Invoke("Cannot play file: bridge not connected.");
                return;
            }
            bool fx = RollEffect();
            var sw = Stopwatch.StartNew();
            try {
                var resp = await pc.SendAsync<OkResponse>(
                    new SpeakFileRequest { Path = path, RandomEffect = fx },
                    TimeSpan.FromSeconds(30));
                sw.Stop();
                if (!resp.Ok && !string.IsNullOrEmpty(resp.Error)) {
                    Log?.Invoke("Bridge file rejected: " + resp.Error);
                } else {
                    Log?.Invoke($"SpeakFile timing: ipc={sw.ElapsedMilliseconds}ms");
                }
            } catch (Exception ex) {
                Log?.Invoke("SpeakFile error: " + ex.Message);
            }
        }

        private static void SendSpeakPcm(byte[] pcm, bool randomEffect) {
            var pc = pipeClient;
            if (pc == null) {
                Log?.Invoke("Cannot send audio: bridge not connected.");
                return;
            }
            try {
                var resp = Task.Run(() => pc.SendSpeakPcmAsync(pcm, 48000, 16, 2, TimeSpan.FromSeconds(30), randomEffect))
                    .GetAwaiter().GetResult();
                if (!resp.Ok && !string.IsNullOrEmpty(resp.Error)) {
                    Log?.Invoke("Bridge audio rejected: " + resp.Error);
                }
            } catch (Exception ex) {
                Log?.Invoke("SpeakPcm error: " + ex.Message);
            }
        }
    }
}
