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

        public async Task<TResp> SendAsync<TResp>(object request, TimeSpan? timeout = null) {
            int reqId = Interlocked.Increment(ref nextReqId);
            var prop = request.GetType().GetProperty("ReqId");
            if (prop != null) {
                prop.SetValue(request, (int?)reqId);
            }

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
                return JsonSerializer.Deserialize<TResp>(element.GetRawText(), jsonOpts);
            } finally {
                pending.TryRemove(reqId, out _);
            }
        }

        public Task SendFireAndForgetAsync(object request) {
            return SendFrameAsync(request);
        }

        private async Task SendFrameAsync(object frame) {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(frame, frame.GetType(), jsonOpts);
            await writeLock.WaitAsync();
            try {
                byte[] len = BitConverter.GetBytes(json.Length);
                await pipe.WriteAsync(len, 0, 4);
                await pipe.WriteAsync(json, 0, json.Length);
                // No FlushAsync: on Windows named pipes that calls FlushFileBuffers, which
                // blocks until the peer drains the pipe. WriteAsync already pushes bytes into
                // the OS pipe buffer, which is what the peer reads.
            } finally {
                writeLock.Release();
            }
        }

        private async Task ReadLoopAsync(CancellationToken ct) {
            string failureReason = "Bridge pipe closed";
            try {
                while (!ct.IsCancellationRequested && pipe.IsConnected) {
                    string json = await ReadFrameAsync(ct);
                    if (json == null) break;
                    DispatchFrame(json);
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

        private async Task<string> ReadFrameAsync(CancellationToken ct) {
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
            return Encoding.UTF8.GetString(payload);
        }

        private void DispatchFrame(string json) {
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
            try { readLoopCts?.Cancel(); } catch { }
            try { pipe?.Dispose(); } catch { }
            try { writeLock?.Dispose(); } catch { }
        }
    }
}
