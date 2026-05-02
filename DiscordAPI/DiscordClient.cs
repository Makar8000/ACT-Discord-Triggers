using DiscordBridge.Protocol;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Speech.AudioFormat;
using System.Speech.Synthesis;
using System.Threading.Tasks;

namespace DiscordAPI {
    public static class DiscordClient {
        private static PipeClient pipeClient;
        private static BridgeProcess bridge;
        private static string bridgeDir;
        private static readonly object lifecycleLock = new object();

        private static readonly SpeechAudioFormatInfo formatInfo =
            new SpeechAudioFormatInfo(48000, AudioBitsPerSample.Sixteen, AudioChannel.Stereo);

        public delegate void BotLoaded();
        public static BotLoaded BotReady;

        public delegate void BotMessage(string message);
        public static BotMessage Log;

        public static void SetBridgePath(string dir) {
            bridgeDir = dir;
        }

        public static async void InIt(string logintoken, string botstatus) {
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
            } catch (Exception ex) {
                Log?.Invoke("InIt error: " + ex.Message);
            }
        }

        public static async Task deInIt() {
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

        public static bool IsConnected() {
            var pc = pipeClient;
            if (pc == null) return false;
            try {
                return Task.Run(async () =>
                    (await pc.SendAsync<IsConnectedResponse>(new IsConnectedRequest(), TimeSpan.FromSeconds(3))).Connected
                ).GetAwaiter().GetResult();
            } catch {
                return false;
            }
        }

        public static string[] getServers() {
            var pc = pipeClient;
            if (pc == null) return new string[0];
            try {
                var resp = Task.Run(() => pc.SendAsync<GetServersResponse>(new GetServersRequest()))
                    .GetAwaiter().GetResult();
                return resp.Servers ?? new string[0];
            } catch (Exception ex) {
                Log?.Invoke("getServers failed: " + ex.Message);
                return new string[0];
            }
        }

        public static string[] getChannels(string server) {
            var pc = pipeClient;
            if (pc == null) return new string[0];
            try {
                var resp = Task.Run(() => pc.SendAsync<GetChannelsResponse>(
                    new GetChannelsRequest { Server = server }))
                    .GetAwaiter().GetResult();
                return resp.Channels ?? new string[0];
            } catch (Exception ex) {
                Log?.Invoke("getChannels failed: " + ex.Message);
                return new string[0];
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

        public static void LeaveChannel() {
            var pc = pipeClient;
            if (pc == null) return;
            try {
                Task.Run(() => pc.SendAsync<OkResponse>(new LeaveChannelRequest(), TimeSpan.FromSeconds(10)))
                    .GetAwaiter().GetResult();
            } catch (Exception ex) {
                Log?.Invoke("LeaveChannel failed: " + ex.Message);
            }
        }

        public static void Speak(string text, string voice, int vol, int speed) {
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
            var swIpc = Stopwatch.StartNew();
            SendSpeakPcm(pcm);
            swIpc.Stop();
            Log?.Invoke($"Speak timing: synth={swSynth.ElapsedMilliseconds}ms ipc={swIpc.ElapsedMilliseconds}ms bytes={pcm.Length}");
        }

        public static void SpeakFile(string path) {
            var pc = pipeClient;
            if (pc == null) {
                Log?.Invoke("Cannot play file: bridge not connected.");
                return;
            }
            var sw = Stopwatch.StartNew();
            try {
                var resp = Task.Run(() => pc.SendAsync<OkResponse>(
                    new SpeakFileRequest { Path = path },
                    TimeSpan.FromSeconds(30)))
                    .GetAwaiter().GetResult();
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

        private static void SendSpeakPcm(byte[] pcm) {
            var pc = pipeClient;
            if (pc == null) {
                Log?.Invoke("Cannot send audio: bridge not connected.");
                return;
            }
            try {
                var resp = Task.Run(() => pc.SendSpeakPcmAsync(pcm, 48000, 16, 2, TimeSpan.FromSeconds(30)))
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
