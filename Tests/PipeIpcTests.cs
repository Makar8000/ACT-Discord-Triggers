using DiscordAPI;
using DiscordBridge.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace ActDiscordTriggers.Tests {
    public class PipeIpcTests : IDisposable {
        private readonly NamedPipeServerStream serverPipe;
        private readonly NamedPipeClientStream clientPipe;
        private PipeClient pipeClient;
        private readonly string pipeName;

        public PipeIpcTests() {
            pipeName = "act-test-" + Guid.NewGuid().ToString("N").Substring(0, 16);
            // Explicit 64 KB buffers so writes don't block on tests that intentionally
            // skip reading. Production uses a small default buffer because the bridge's
            // read loop is always draining.
            serverPipe = new NamedPipeServerStream(
                pipeName, PipeDirection.InOut, 1,
                PipeTransmissionMode.Byte, PipeOptions.Asynchronous,
                inBufferSize: 64 * 1024, outBufferSize: 64 * 1024);
            clientPipe = new NamedPipeClientStream(
                ".", pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
        }

        private async Task ConnectAsync() {
            var srvTask = serverPipe.WaitForConnectionAsync();
            await clientPipe.ConnectAsync(5000);
            await srvTask;
            pipeClient = new PipeClient(clientPipe);
            pipeClient.Start();
        }

        public void Dispose() {
            try { pipeClient?.Dispose(); } catch { }
            try { serverPipe?.Dispose(); } catch { }
        }

        private static async Task<JsonElement> ReadFrameAsync(Stream pipe) {
            byte[] payload = await ReadRawFrameAsync(pipe);
            using var doc = JsonDocument.Parse(payload);
            return doc.RootElement.Clone();
        }

        private static async Task<byte[]> ReadRawFrameAsync(Stream pipe) {
            byte[] lenBuf = new byte[4];
            int read = 0;
            while (read < 4) {
                int n = await pipe.ReadAsync(lenBuf, read, 4 - read);
                if (n == 0) throw new EndOfStreamException();
                read += n;
            }
            int len = BitConverter.ToInt32(lenBuf, 0);
            byte[] payload = new byte[len];
            read = 0;
            while (read < len) {
                int n = await pipe.ReadAsync(payload, read, len - read);
                if (n == 0) throw new EndOfStreamException();
                read += n;
            }
            return payload;
        }

        private static async Task WriteFrameAsync(Stream pipe, object frame) {
            byte[] json = JsonSerializer.SerializeToUtf8Bytes(frame, frame.GetType());
            byte[] len = BitConverter.GetBytes(json.Length);
            await pipe.WriteAsync(len, 0, 4);
            await pipe.WriteAsync(json, 0, json.Length);
            await pipe.FlushAsync();
        }

        [Fact]
        public async Task SendAsync_correlates_response_by_reqId() {
            await ConnectAsync();

            var sendTask = pipeClient.SendAsync<HelloResponse>(
                new HelloRequest { ProtocolVersion = ProtocolConstants.Version },
                TimeSpan.FromSeconds(5));

            var requestFrame = await ReadFrameAsync(serverPipe);
            int reqId = requestFrame.GetProperty("reqId").GetInt32();
            Assert.Equal("Hello", requestFrame.GetProperty("op").GetString());
            Assert.Equal(ProtocolConstants.Version, requestFrame.GetProperty("protocolVersion").GetInt32());

            await WriteFrameAsync(serverPipe, new HelloResponse {
                ReqId = reqId, Ok = true, BridgeVersion = "test-1.0"
            });

            var resp = await sendTask;
            Assert.True(resp.Ok);
            Assert.Equal("test-1.0", resp.BridgeVersion);
        }

        [Fact]
        public async Task SendAsync_times_out_when_no_response() {
            await ConnectAsync();
            await Assert.ThrowsAsync<TimeoutException>(() =>
                pipeClient.SendAsync<HelloResponse>(
                    new HelloRequest { ProtocolVersion = 1 },
                    TimeSpan.FromMilliseconds(200)));
        }

        [Fact]
        public async Task OutOfOrder_responses_correlate_correctly() {
            await ConnectAsync();
            var t1 = pipeClient.SendAsync<HelloResponse>(
                new HelloRequest { ProtocolVersion = 1 }, TimeSpan.FromSeconds(5));
            var t2 = pipeClient.SendAsync<HelloResponse>(
                new HelloRequest { ProtocolVersion = 1 }, TimeSpan.FromSeconds(5));

            var f1 = await ReadFrameAsync(serverPipe);
            var f2 = await ReadFrameAsync(serverPipe);
            int id1 = f1.GetProperty("reqId").GetInt32();
            int id2 = f2.GetProperty("reqId").GetInt32();
            Assert.NotEqual(id1, id2);

            // respond to second first
            await WriteFrameAsync(serverPipe, new HelloResponse {
                ReqId = id2, Ok = true, BridgeVersion = "second"
            });
            await WriteFrameAsync(serverPipe, new HelloResponse {
                ReqId = id1, Ok = true, BridgeVersion = "first"
            });

            Assert.Equal("first", (await t1).BridgeVersion);
            Assert.Equal("second", (await t2).BridgeVersion);
        }

        [Fact]
        public async Task LogNotification_dispatches_to_OnLog_event() {
            await ConnectAsync();
            var done = new TaskCompletionSource<(string msg, string level)>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            pipeClient.OnLog += (m, l) => done.TrySetResult((m, l));

            await WriteFrameAsync(serverPipe, new LogNotification {
                Message = "hello world", Level = "Info"
            });

            var got = await done.Task.WaitAsync(TimeSpan.FromSeconds(2));
            Assert.Equal("hello world", got.msg);
            Assert.Equal("Info", got.level);
        }

        [Fact]
        public async Task BotReady_notification_fires_OnBotReady() {
            await ConnectAsync();
            var done = new TaskCompletionSource<bool>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            pipeClient.OnBotReady += () => done.TrySetResult(true);

            await WriteFrameAsync(serverPipe, new BotReadyNotification());
            Assert.True(await done.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public async Task DisconnectedNotification_fires_OnDisconnected_with_reason() {
            await ConnectAsync();
            var done = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            pipeClient.OnDisconnected += r => done.TrySetResult(r);

            await WriteFrameAsync(serverPipe, new DisconnectedNotification { Reason = "gateway closed" });
            Assert.Equal("gateway closed", await done.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public async Task PipeBroken_cancels_pending_requests_and_fires_event() {
            await ConnectAsync();

            var brokenSignal = new TaskCompletionSource<string>(
                TaskCreationOptions.RunContinuationsAsynchronously);
            pipeClient.OnPipeBroken += r => brokenSignal.TrySetResult(r);

            var sendTask = pipeClient.SendAsync<HelloResponse>(
                new HelloRequest { ProtocolVersion = 1 }, TimeSpan.FromSeconds(30));

            // Drain the request from the server side so we know it landed
            _ = await ReadFrameAsync(serverPipe);

            // Tear down the server side
            try { serverPipe.Disconnect(); } catch { }
            serverPipe.Dispose();

            await Assert.ThrowsAsync<IOException>(() => sendTask);
            Assert.NotNull(await brokenSignal.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public async Task SpeakPcm_binary_frame_arrives_at_server_intact() {
            await ConnectAsync();
            byte[] pcm = new byte[16 * 1024];
            new Random(42).NextBytes(pcm);

            var sendTask = pipeClient.SendSpeakPcmAsync(pcm, 48000, 16, 2, TimeSpan.FromSeconds(5));

            byte[] frame = await ReadRawFrameAsync(serverPipe);
            // Binary marker + 11-byte header + payload
            Assert.Equal(0x01, frame[0]);
            int reqId = BitConverter.ToInt32(frame, 1);
            int sampleRate = BitConverter.ToInt32(frame, 5);
            byte bits = frame[9];
            byte channels = frame[10];
            Assert.Equal(48000, sampleRate);
            Assert.Equal(16, bits);
            Assert.Equal(2, channels);

            byte[] gotPcm = new byte[frame.Length - 11];
            Buffer.BlockCopy(frame, 11, gotPcm, 0, gotPcm.Length);
            Assert.Equal(pcm, gotPcm);

            await WriteFrameAsync(serverPipe, new OkResponse {
                Op = Op.SpeakResult, ReqId = reqId, Ok = true
            });

            var resp = await sendTask;
            Assert.True(resp.Ok);
        }

        [Fact]
        public async Task Concurrent_SendAsync_calls_get_correct_responses() {
            await ConnectAsync();

            // Echo server: read each request, reply with HelloResponse echoing protocolVersion
            var echoCts = new System.Threading.CancellationTokenSource();
            var echoTask = Task.Run(async () => {
                try {
                    while (!echoCts.IsCancellationRequested) {
                        var f = await ReadFrameAsync(serverPipe);
                        int reqId = f.GetProperty("reqId").GetInt32();
                        int pv = f.GetProperty("protocolVersion").GetInt32();
                        await WriteFrameAsync(serverPipe, new HelloResponse {
                            ReqId = reqId, Ok = true, BridgeVersion = "v" + pv,
                        });
                    }
                } catch { }
            });

            const int N = 25;
            var tasks = new Task<HelloResponse>[N];
            for (int i = 0; i < N; i++) {
                int pv = i + 1;
                tasks[i] = pipeClient.SendAsync<HelloResponse>(
                    new HelloRequest { ProtocolVersion = pv }, TimeSpan.FromSeconds(5));
            }
            var results = await Task.WhenAll(tasks);
            echoCts.Cancel();

            for (int i = 0; i < N; i++) {
                Assert.True(results[i].Ok);
                Assert.Equal("v" + (i + 1), results[i].BridgeVersion);
            }
        }

        [Fact]
        public async Task Bad_JSON_frame_is_ignored_and_subsequent_frames_still_work() {
            await ConnectAsync();

            byte[] garbage = System.Text.Encoding.UTF8.GetBytes("not valid json }}}");
            byte[] len = BitConverter.GetBytes(garbage.Length);
            await serverPipe.WriteAsync(len, 0, 4);
            await serverPipe.WriteAsync(garbage, 0, garbage.Length);

            var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            pipeClient.OnLog += (m, l) => done.TrySetResult(m);

            await WriteFrameAsync(serverPipe, new LogNotification {
                Message = "after garbage", Level = "Info"
            });

            Assert.Equal("after garbage", await done.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public async Task Notification_with_unknown_op_does_not_crash_dispatch() {
            await ConnectAsync();

            // Send a notification with an op the client doesn't recognize
            await WriteFrameAsync(serverPipe, new { op = "MysteryOp", payload = "ignored" });

            // Subsequent valid notification still arrives
            var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            pipeClient.OnBotReady += () => done.TrySetResult(true);
            await WriteFrameAsync(serverPipe, new BotReadyNotification());

            Assert.True(await done.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        }

        [Fact]
        public async Task Large_binary_payload_round_trips_intact() {
            await ConnectAsync();
            // 256 KB random bytes — exceeds the 64 KB pipe buffer so this exercises
            // the partial-write / concurrent-read path on the binary frame path.
            byte[] big = new byte[256 * 1024];
            new Random(7).NextBytes(big);

            var sendTask = pipeClient.SendSpeakPcmAsync(big, 48000, 16, 2, TimeSpan.FromSeconds(10));

            byte[] frame = await ReadRawFrameAsync(serverPipe);
            Assert.Equal(0x01, frame[0]);
            int reqId = BitConverter.ToInt32(frame, 1);
            byte[] got = new byte[frame.Length - 11];
            Buffer.BlockCopy(frame, 11, got, 0, got.Length);
            Assert.Equal(big.Length, got.Length);
            Assert.Equal(big, got);

            await WriteFrameAsync(serverPipe, new OkResponse {
                Op = Op.SpeakResult, ReqId = reqId, Ok = true
            });

            var resp = await sendTask;
            Assert.True(resp.Ok);
        }

        [Fact]
        public async Task Response_with_unknown_reqId_does_not_crash() {
            await ConnectAsync();

            // Server sends a response correlating to a reqId we never issued
            await WriteFrameAsync(serverPipe, new HelloResponse {
                ReqId = 999_999, Ok = true, BridgeVersion = "ghost",
            });

            // Real subsequent request still works
            var sendTask = pipeClient.SendAsync<HelloResponse>(
                new HelloRequest { ProtocolVersion = 1 }, TimeSpan.FromSeconds(5));
            var f = await ReadFrameAsync(serverPipe);
            int reqId = f.GetProperty("reqId").GetInt32();
            await WriteFrameAsync(serverPipe, new HelloResponse {
                ReqId = reqId, Ok = true, BridgeVersion = "real",
            });
            var resp = await sendTask;
            Assert.Equal("real", resp.BridgeVersion);
        }

        [Fact]
        public async Task Frame_with_extra_unknown_fields_is_tolerated() {
            await ConnectAsync();

            var done = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            pipeClient.OnLog += (m, l) => done.TrySetResult(m);

            // Server sends a Log notification with an extra "futureField"
            await WriteFrameAsync(serverPipe, new {
                op = Op.Log,
                message = "hi",
                level = "Info",
                futureField = new { nested = 123 },
            });

            Assert.Equal("hi", await done.Task.WaitAsync(TimeSpan.FromSeconds(2)));
        }

        // The server pipe is built with outBufferSize=64KB and we never read it
        // here, so a >64KB binary frame fills the kernel buffer and stalls
        // pipe.WriteAsync. With the WriteWithTimeoutAsync guard, the send should
        // bail within WriteTimeoutMs (5s) instead of hanging on the full 30s
        // request timeout.
        [Fact]
        public async Task SendAsync_write_times_out_when_peer_never_reads() {
            await ConnectAsync();

            byte[] big = new byte[128 * 1024];
            new Random(7).NextBytes(big);

            var sw = System.Diagnostics.Stopwatch.StartNew();
            var t = pipeClient.SendSpeakPcmAsync(big, 48000, 16, 2, TimeSpan.FromSeconds(30));
            var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
                t.WaitAsync(TimeSpan.FromSeconds(15)));
            sw.Stop();

            Assert.True(ex is IOException || ex is OperationCanceledException,
                "expected IOException or OperationCanceledException, got " + ex.GetType().Name);
            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(15),
                "write should have failed fast via WriteTimeoutMs (~5s), not via the 30s request timeout");
        }

        // Verifies Dispose tears down in the correct order: cancel CT, dispose
        // pipe (faults in-flight ops), wait for read loop to exit, then dispose
        // writeLock. A pending SendAsync should fault cleanly without
        // ObjectDisposedException escaping Dispose.
        [Fact]
        public async Task Dispose_waits_for_read_loop_and_does_not_throw_with_pending_send() {
            await ConnectAsync();

            var t = pipeClient.SendAsync<HelloResponse>(
                new HelloRequest { ProtocolVersion = ProtocolConstants.Version },
                TimeSpan.FromSeconds(30));

            // Make sure the request has landed at the server before we tear down.
            _ = await ReadFrameAsync(serverPipe);

            // Must not throw.
            pipeClient.Dispose();

            // The pending send must fault rather than hang.
            await Assert.ThrowsAnyAsync<Exception>(() => t);
        }
    }
}
