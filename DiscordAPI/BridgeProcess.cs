using System;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace DiscordAPI {
    internal class BridgeProcess : IDisposable {
        private Process process;

        public string PipeName { get; private set; }

        public event Action<string> OnStderr;
        public event Action<int> OnExited;

        public async Task<NamedPipeClientStream> StartAndConnectAsync(string exePath, TimeSpan? handshakeTimeout = null) {
            if (string.IsNullOrEmpty(exePath)) {
                throw new ArgumentException("exePath is required.", nameof(exePath));
            }
            if (!File.Exists(exePath)) {
                throw new FileNotFoundException("DiscordBridge.exe not found at: " + exePath, exePath);
            }

            PipeName = "act-discord-bridge-" + Guid.NewGuid().ToString("N").Substring(0, 16);

            var psi = new ProcessStartInfo {
                FileName = exePath,
                Arguments = QuoteArg(PipeName),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = Path.GetDirectoryName(exePath),
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
                throw new InvalidOperationException("Failed to start DiscordBridge.exe");
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
                    throw new TimeoutException("DiscordBridge.exe did not signal ready within " + to.TotalSeconds + "s.");
                }
                string line = await lineTask;
                if (line == null) {
                    int code = -1;
                    try { code = process.ExitCode; } catch { }
                    throw new IOException("DiscordBridge.exe exited before handshake. Exit code: " + code);
                }
                if (line.StartsWith("BRIDGE_READY", StringComparison.Ordinal)) {
                    var client = new NamedPipeClientStream(".", PipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
                    await client.ConnectAsync(5000);
                    return client;
                }
                try { OnStderr?.Invoke(line); } catch { }
            }
            throw new TimeoutException("DiscordBridge.exe did not signal ready within " + to.TotalSeconds + "s.");
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
