using DiscordBridge.Protocol;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordAPI {
    internal class PipeClient : IDisposable {
        private readonly NamedPipeClientStream pipe;
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement>> pending
            = new ConcurrentDictionary<int, TaskCompletionSource<JsonElement>>();
        private readonly SemaphoreSlim writeLock = new SemaphoreSlim(1, 1);
        private int nextReqId;
        private CancellationTokenSource readLoopCts;
        private Task readLoopTask;
        private const int MaxFrameBytes = 64 * 1024 * 1024;
        private const int WriteTimeoutMs = 5000;
        private const byte FrameJsonMarker = (byte)'{';
        private const byte FrameBinarySpeakPcm = 0x01;
        private static readonly JsonSerializerOptions jsonOpts = new JsonSerializerOptions {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        public event Action OnBotReady;
        public event Action<string, string> OnLog;
        public event Action<string> OnDisconnected;
        public event Action<string> OnPipeBroken;

        public PipeClient(NamedPipeClientStream pipe) {
            this.pipe = pipe;
        }

        public void Start() {
            readLoopCts = new CancellationTokenSource();
            readLoopTask = Task.Run(() => ReadLoopAsync(readLoopCts.Token));
        }

        public async Task<TResp> SendAsync<TResp>(IBridgeRequest request, TimeSpan? timeout = null) {
            int reqId = Interlocked.Increment(ref nextReqId);
            request.ReqId = reqId;

            var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending[reqId] = tcs;

            try {
                await SendFrameAsync(request);
                var to = timeout ?? TimeSpan.FromSeconds(60);
                var done = await Task.WhenAny(tcs.Task, Task.Delay(to));
                if (done != tcs.Task) {
                    throw new TimeoutException($"Bridge request '{request.GetType().Name}' timed out after {to.TotalSeconds:0}s.");
                }
                var element = await tcs.Task;
                return element.Deserialize<TResp>(jsonOpts);
            } finally {
                pending.TryRemove(reqId, out _);
            }
        }

        public Task SendFireAndForgetAsync(IBridgeRequest request) {
            return SendFrameAsync(request);
        }

        // Binary SpeakPcm frame. Plugin-only direction (client → bridge); the bridge
        // never sends binary back. Header is 12 bytes:
        //   [0x01][reqId u32 LE][sampleRate u32 LE][bits u8][channels u8][flags u8]
        // flags bit0 = apply a random sound effect. Followed by `pcm.Length` raw PCM
        // bytes. Response is the JSON `SpeakResult` op with the same reqId, dispatched
        // by the existing JSON path.
        public async Task<OkResponse> SendSpeakPcmAsync(byte[] pcm, int sampleRate, int bits, int channels, TimeSpan? timeout = null, bool randomEffect = false) {
            if (pcm == null) throw new ArgumentNullException(nameof(pcm));
            int reqId = Interlocked.Increment(ref nextReqId);

            var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
            pending[reqId] = tcs;

            try {
                byte[] payload = new byte[12 + pcm.Length];
                payload[0] = FrameBinarySpeakPcm;
                WriteUInt32LE(payload, 1, (uint)reqId);
                WriteUInt32LE(payload, 5, (uint)sampleRate);
                payload[9] = (byte)bits;
                payload[10] = (byte)channels;
                payload[11] = (byte)(randomEffect ? 0x01 : 0x00);
                Buffer.BlockCopy(pcm, 0, payload, 12, pcm.Length);

                await SendBinaryFrameAsync(payload);

                var to = timeout ?? TimeSpan.FromSeconds(60);
                var done = await Task.WhenAny(tcs.Task, Task.Delay(to));
                if (done != tcs.Task) {
                    throw new TimeoutException($"Bridge request 'SpeakPcm' timed out after {to.TotalSeconds:0}s.");
                }
                var element = await tcs.Task;
                return element.Deserialize<OkResponse>(jsonOpts);
            } finally {
                pending.TryRemove(reqId, out _);
            }
        }

        private static void WriteUInt32LE(byte[] buf, int offset, uint value) {
            buf[offset]     = (byte)(value & 0xFF);
            buf[offset + 1] = (byte)((value >> 8) & 0xFF);
            buf[offset + 2] = (byte)((value >> 16) & 0xFF);
            buf[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

        private async Task SendFrameAsync(object frame) {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(frame, frame.GetType(), jsonOpts);
            byte[] len = BitConverter.GetBytes(json.Length);
            await writeLock.WaitAsync();
            try {
                await WriteWithTimeoutAsync(len, 0, 4);
                await WriteWithTimeoutAsync(json, 0, json.Length);
                // No FlushAsync: on Windows named pipes that calls FlushFileBuffers, which
                // blocks until the peer drains the pipe. WriteAsync already pushes bytes into
                // the OS pipe buffer, which is what the peer reads.
            } finally {
                writeLock.Release();
            }
        }

        private async Task SendBinaryFrameAsync(byte[] payload) {
            byte[] len = BitConverter.GetBytes(payload.Length);
            await writeLock.WaitAsync();
            try {
                await WriteWithTimeoutAsync(len, 0, 4);
                await WriteWithTimeoutAsync(payload, 0, payload.Length);
            } finally {
                writeLock.Release();
            }
        }

        // Wraps pipe.WriteAsync with a hard deadline. A stalled peer (pipe buffer
        // full, peer not reading) would otherwise hold writeLock indefinitely and
        // wedge every subsequent send. On Windows .NET Framework, PipeStream's
        // CancellationToken-aware WriteAsync does not reliably abort the
        // underlying overlapped I/O, so we race against Task.Delay and force the
        // pipe to fail by disposing it. A partial write would corrupt the
        // length-prefixed frame stream, so disposing the pipe (which also faults
        // the read loop and triggers OnPipeBroken / FailAllPending) is the
        // correct teardown.
        private async Task WriteWithTimeoutAsync(byte[] buf, int offset, int count) {
            var writeTask = pipe.WriteAsync(buf, offset, count);
            var winner = await Task.WhenAny(writeTask, Task.Delay(WriteTimeoutMs));
            if (winner != writeTask) {
                try { pipe.Dispose(); } catch { }
                // Observe the orphaned write so its eventual fault doesn't surface
                // as an unobserved task exception.
                _ = writeTask.ContinueWith(t => { var _ = t.Exception; },
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
                throw new IOException("Bridge pipe write timed out after " + WriteTimeoutMs + "ms; tearing down.");
            }
            await writeTask;
        }

        private async Task ReadLoopAsync(CancellationToken ct) {
            string failureReason = "Bridge pipe closed";
            try {
                while (!ct.IsCancellationRequested && pipe.IsConnected) {
                    byte[] payload = await ReadFrameAsync(ct);
                    if (payload == null) break;
                    DispatchFrame(payload);
                }
            } catch (OperationCanceledException) {
                failureReason = "Bridge read cancelled";
            } catch (Exception ex) {
                failureReason = "Bridge pipe error: " + ex.Message;
            } finally {
                FailAllPending(failureReason);
                try { OnPipeBroken?.Invoke(failureReason); } catch { }
            }
        }

        private async Task<byte[]> ReadFrameAsync(CancellationToken ct) {
            byte[] lenBuf = new byte[4];
            int read = 0;
            while (read < 4) {
                int n = await pipe.ReadAsync(lenBuf, read, 4 - read, ct);
                if (n == 0) return null;
                read += n;
            }
            int len = BitConverter.ToInt32(lenBuf, 0);
            if (len <= 0 || len > MaxFrameBytes) {
                throw new IOException($"Bad frame length from bridge: {len}");
            }
            byte[] payload = new byte[len];
            read = 0;
            while (read < len) {
                int n = await pipe.ReadAsync(payload, read, len - read, ct);
                if (n == 0) return null;
                read += n;
            }
            return payload;
        }

        private void DispatchFrame(byte[] payload) {
            if (payload.Length == 0) return;
            byte first = payload[0];
            if (first == FrameJsonMarker) {
                DispatchJsonFrame(Encoding.UTF8.GetString(payload));
            } else {
                // Bridge does not currently push binary frames. Log silently and drop;
                // the JSON Log path is the ordinary diagnostic channel.
            }
        }

        private void DispatchJsonFrame(string json) {
            try {
                using (var doc = JsonDocument.Parse(json)) {
                    var root = doc.RootElement;
                    if (!root.TryGetProperty("op", out var opElem)) return;
                    string op = opElem.GetString() ?? "";

                    // Notification handlers run on a threadpool task, NOT on the read loop:
                    // the plugin's BotReady handler may call back into SendAsync (e.g. to fetch
                    // server/channel lists), and that response can only arrive after the read
                    // loop returns to ReadFrameAsync. Synchronous invocation here would
                    // deadlock the response read.
                    switch (op) {
                        case Op.BotReady: {
                            var handler = OnBotReady;
                            if (handler != null) Task.Run(() => { try { handler(); } catch { } });
                            return;
                        }
                        case Op.Log: {
                            string msg = root.TryGetProperty("message", out var m) ? (m.GetString() ?? "") : "";
                            string lvl = root.TryGetProperty("level", out var l) ? (l.GetString() ?? "Info") : "Info";
                            var handler = OnLog;
                            if (handler != null) Task.Run(() => { try { handler(msg, lvl); } catch { } });
                            return;
                        }
                        case Op.Disconnected: {
                            string reason = root.TryGetProperty("reason", out var rs) ? (rs.GetString() ?? "") : "";
                            var handler = OnDisconnected;
                            if (handler != null) Task.Run(() => { try { handler(reason); } catch { } });
                            return;
                        }
                    }

                    int? reqId = root.TryGetProperty("reqId", out var r) && r.ValueKind == JsonValueKind.Number
                        ? r.GetInt32() : (int?)null;
                    if (reqId.HasValue && pending.TryRemove(reqId.Value, out var tcs)) {
                        tcs.TrySetResult(root.Clone());
                    }
                }
            } catch { }
        }

        private void FailAllPending(string reason) {
            foreach (var kv in pending) {
                kv.Value.TrySetException(new IOException(reason));
            }
            pending.Clear();
        }

        public void Dispose() {
            // Order matters:
            //   1. cancel the read loop's CT
            //   2. dispose the pipe — faults any in-flight WriteAsync / ReadAsync
            //   3. wait for the read loop to exit so it stops touching writeLock-adjacent state
            //   4. dispose writeLock
            try { readLoopCts?.Cancel(); } catch { }
            try { pipe?.Dispose(); } catch { }
            try { readLoopTask?.Wait(TimeSpan.FromSeconds(2)); } catch { }
            try { writeLock?.Dispose(); } catch { }
        }
    }
}
