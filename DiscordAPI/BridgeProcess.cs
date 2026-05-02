using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace DiscordAPI {
    // Spawns the node bridge directly: node.exe bundle.js <pipe-name>.
    //
    // Lifecycle: when this process dies (clean or hard kill), the OS closes the
    // pipe client handle, the bridge's net.Server sees socket close, and the
    // bridge calls process.exit(0) (see DiscordBridge-node/src/bridge.ts). No
    // intermediate launcher / Win32 Job Object is needed because there's only
    // one child process to coordinate.
    internal class BridgeProcess : IDisposable {
        private Process process;

        public string PipeName { get; private set; }

        public event Action<string> OnStderr;
        public event Action<int> OnExited;

        public async Task<NamedPipeClientStream> StartAndConnectAsync(string bridgeDir, TimeSpan? handshakeTimeout = null) {
            if (string.IsNullOrEmpty(bridgeDir)) {
                throw new ArgumentException("bridgeDir is required.", nameof(bridgeDir));
            }
            string nodeExe = Path.Combine(bridgeDir, "node.exe");
            string bundleJs = Path.Combine(bridgeDir, "bundle.js");
            if (!File.Exists(nodeExe)) {
                throw new FileNotFoundException("node.exe not found at: " + nodeExe, nodeExe);
            }
            if (!File.Exists(bundleJs)) {
                throw new FileNotFoundException("bundle.js not found at: " + bundleJs, bundleJs);
            }

            PipeName = "act-discord-bridge-" + Guid.NewGuid().ToString("N").Substring(0, 16);

            var psi = new ProcessStartInfo {
                FileName = nodeExe,
                Arguments = QuoteArg(bundleJs) + " " + QuoteArg(PipeName),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = bridgeDir,
            };

            process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            process.Exited += (_, __) => {
                int code = -1;
                try { code = process.ExitCode; } catch { }
                try { OnExited?.Invoke(code); } catch { }
            };
            process.ErrorDataReceived += (_, e) => {
                if (!string.IsNullOrEmpty(e.Data)) {
                    try { OnStderr?.Invoke(e.Data); } catch { }
                }
            };

            if (!process.Start()) {
                throw new InvalidOperationException("Failed to start node.exe");
            }
            process.BeginErrorReadLine();

            var to = handshakeTimeout ?? TimeSpan.FromSeconds(15);
            var deadline = DateTime.UtcNow + to;
            while (DateTime.UtcNow < deadline) {
                var lineTask = process.StandardOutput.ReadLineAsync();
                var remaining = deadline - DateTime.UtcNow;
                if (remaining < TimeSpan.Zero) remaining = TimeSpan.Zero;
                var done = await Task.WhenAny(lineTask, Task.Delay(remaining));
                if (done != lineTask) {
                    throw new TimeoutException("Bridge did not signal ready within " + to.TotalSeconds + "s.");
                }
                string line = await lineTask;
                if (line == null) {
                    int code = -1;
                    try { code = process.ExitCode; } catch { }
                    throw new IOException("Bridge exited before handshake. Exit code: " + code);
                }
                if (line.StartsWith("BRIDGE_READY", StringComparison.Ordinal)) {
                    var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                    await client.ConnectAsync(5000);
                    return client;
                }
                try { OnStderr?.Invoke(line); } catch { }
            }
            throw new TimeoutException("Bridge did not signal ready within " + to.TotalSeconds + "s.");
        }

        public async Task WaitForExitAsync(TimeSpan timeout) {
            if (process == null || process.HasExited) return;
            await Task.Run(() => process.WaitForExit((int)timeout.TotalMilliseconds));
        }

        public bool HasExited => process == null || process.HasExited;

        public void Kill() {
            try {
                if (process != null && !process.HasExited) {
                    process.Kill();
                }
            } catch { }
        }

        private static string QuoteArg(string s) {
            if (s.IndexOfAny(new[] { ' ', '"' }) < 0) return s;
            return "\"" + s.Replace("\"", "\\\"") + "\"";
        }

        public void Dispose() {
            Kill();
            try { process?.Dispose(); } catch { }
        }
    }
}
