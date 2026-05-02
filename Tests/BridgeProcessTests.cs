using DiscordAPI;
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace ActDiscordTriggers.Tests {
    public class BridgeProcessTests {
        private static string FindBridgeExe() {
            string testDir = Path.GetDirectoryName(typeof(BridgeProcessTests).Assembly.Location);
            string solDir = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
            string p = Path.Combine(solDir, "DiscordBridge-node", "dist", "DiscordBridge.exe");
            if (File.Exists(p)) return p;
            throw new FileNotFoundException(
                "DiscordBridge.exe not built. Run `pwsh DiscordBridge-node\\build.ps1` first. Looked in: " + p);
        }

        [Fact]
        public async Task Kill_is_idempotent() {
            using var bp = new BridgeProcess();
            await bp.StartAndConnectAsync(FindBridgeExe(), TimeSpan.FromSeconds(15));

            bp.Kill();
            await bp.WaitForExitAsync(TimeSpan.FromSeconds(5));
            Assert.True(bp.HasExited);

            // Second Kill must not throw
            bp.Kill();
            bp.Kill();
            Assert.True(bp.HasExited);
        }

        [Fact]
        public async Task HasExited_returns_true_after_Kill() {
            using var bp = new BridgeProcess();
            await bp.StartAndConnectAsync(FindBridgeExe(), TimeSpan.FromSeconds(15));
            Assert.False(bp.HasExited);

            bp.Kill();
            await bp.WaitForExitAsync(TimeSpan.FromSeconds(5));
            Assert.True(bp.HasExited);
        }

        [Fact]
        public async Task WaitForExitAsync_returns_immediately_when_already_exited() {
            using var bp = new BridgeProcess();
            await bp.StartAndConnectAsync(FindBridgeExe(), TimeSpan.FromSeconds(15));
            bp.Kill();
            await bp.WaitForExitAsync(TimeSpan.FromSeconds(5));

            // Calling again on an already-exited process should return promptly
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await bp.WaitForExitAsync(TimeSpan.FromSeconds(2));
            sw.Stop();
            Assert.True(sw.Elapsed < TimeSpan.FromSeconds(1),
                "WaitForExitAsync should return promptly when process already exited; took " + sw.Elapsed);
        }

        [Fact]
        public async Task OnExited_event_fires_when_bridge_dies() {
            using var bp = new BridgeProcess();
            int exitCode = -999;
            var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            bp.OnExited += code => { exitCode = code; tcs.TrySetResult(code); };

            await bp.StartAndConnectAsync(FindBridgeExe(), TimeSpan.FromSeconds(15));
            bp.Kill();

            int got = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(exitCode, got);
        }

        [Fact]
        public async Task PipeName_is_unique_per_BridgeProcess_instance() {
            using var bp1 = new BridgeProcess();
            using var bp2 = new BridgeProcess();
            try {
                await bp1.StartAndConnectAsync(FindBridgeExe(), TimeSpan.FromSeconds(15));
                await bp2.StartAndConnectAsync(FindBridgeExe(), TimeSpan.FromSeconds(15));
                Assert.NotNull(bp1.PipeName);
                Assert.NotNull(bp2.PipeName);
                Assert.NotEqual(bp1.PipeName, bp2.PipeName);
            } finally {
                bp1.Kill();
                bp2.Kill();
                await bp1.WaitForExitAsync(TimeSpan.FromSeconds(3));
                await bp2.WaitForExitAsync(TimeSpan.FromSeconds(3));
            }
        }
    }
}
