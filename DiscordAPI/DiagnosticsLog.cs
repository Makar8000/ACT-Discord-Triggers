using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace DiscordAPI {
    // Encapsulated, always-on diagnostics the end user never has to think about.
    //
    // Two source logs are written independently — this plugin-side log
    // (ACT_DiscordTriggers-plugin.log) and the node bridge's DiscordBridge.log —
    // so the audio hot path is never coupled to a single shared sink. The plugin
    // then merges both, timestamp-interleaved, into ONE unified file the user can
    // simply email. There is no UI and no toggle: the merge runs on a timer and at
    // shutdown, so <UnifiedPath> is always reasonably current. Telling a user "send
    // me ACT_DiscordTriggers-diagnostics.log from your ACT data folder" is the whole
    // capture procedure.
    //
    // Nothing here is allowed to throw into the host: a logger that can crash the
    // plugin is worse than no logger.
    public static class DiagnosticsLog {
        private const string UnifiedName = "ACT_DiscordTriggers-diagnostics.log";
        private const string PluginLogName = "ACT_DiscordTriggers-plugin.log";
        private const string BridgeLogName = "DiscordBridge.log";
        private const long MaxBytes = 3L * 1024 * 1024;
        private const string TagPlugin = "PLG";
        private const string TagBridge = "BRG";
        private const string TsFormat = "yyyy-MM-dd HH:mm:ss.fff";
        private static readonly int TsLen = TsFormat.Length;

        private static readonly object gate = new object();   // guards buffer + init fields
        private static readonly object writeGate = new object(); // serializes unified writes
        private static readonly List<string> buffer = new List<string>();
        private static string pluginLogPath;
        private static string bridgeLogPath;
        private static string unifiedPath;
        private static string headerInfo = "";
        private static Timer mergeTimer;
        private static bool initialized;

        public static string UnifiedPath { get { lock (gate) return unifiedPath; } }

        // deliverableDir: where the unified file + plugin log live (ACT's data folder).
        // bridgeDir: where the node bridge writes DiscordBridge.log (may be null/missing).
        public static void Init(string deliverableDir, string bridgeDir, string pluginVersion) {
            lock (gate) {
                if (initialized) return;
                try {
                    Directory.CreateDirectory(deliverableDir);
                    pluginLogPath = Path.Combine(deliverableDir, PluginLogName);
                    unifiedPath = Path.Combine(deliverableDir, UnifiedName);
                    bridgeLogPath = string.IsNullOrEmpty(bridgeDir)
                        ? null : Path.Combine(bridgeDir, BridgeLogName);
                    headerInfo = BuildHeaderInfo(pluginVersion);
                    RotateIfLarge(pluginLogPath);
                    initialized = true;
                } catch { return; }
            }
            Append($"==== Plugin session start (v{pluginVersion}, {Environment.OSVersion}) ====");
            Flush();
            // Finalize whatever the previous session left in the source files, then
            // keep the unified file fresh for the life of this session.
            try { WriteUnified(); } catch { }
            try {
                mergeTimer = new Timer(_ => { try { Flush(); WriteUnified(); } catch { } },
                    null, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(30));
            } catch { }
        }

        // Append one line. Thread-safe; callable from any thread (the bridge's Log
        // notifications arrive on thread-pool threads). Never blocks on disk — lines
        // are batched and flushed in bulk.
        public static void Append(string text) {
            if (text == null) return;
            string line = DateTime.Now.ToString(TsFormat, CultureInfo.InvariantCulture) + " " + text;
            lock (gate) {
                if (!initialized) return;
                buffer.Add(line);
                if (buffer.Count >= 64) FlushLocked();
            }
        }

        public static void Flush() { lock (gate) FlushLocked(); }

        private static void FlushLocked() {
            if (buffer.Count == 0 || pluginLogPath == null) return;
            try {
                using (var fs = new FileStream(pluginLogPath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
                using (var w = new StreamWriter(fs, new UTF8Encoding(false))) {
                    foreach (var l in buffer) w.WriteLine(l);
                }
            } catch { /* swallow: never throw into the host */ }
            buffer.Clear();
        }

        public static void Shutdown() {
            Append("==== Plugin session end ====");
            try { mergeTimer?.Dispose(); } catch { }
            mergeTimer = null;
            Flush();
            try { WriteUnified(); } catch { }
        }

        // Regenerate the single deliverable: header + timestamp-interleaved merge of
        // the plugin log and the bridge log. Cheap enough to rebuild wholesale (both
        // sources are size-capped). Written via a temp file so a reader/emailer never
        // catches a half-written file.
        public static void WriteUnified() {
            lock (writeGate) {
                string target, pluginText, bridgeText;
                lock (gate) {
                    if (!initialized || unifiedPath == null) return;
                    target = unifiedPath;
                }
                pluginText = SafeRead(pluginLogPath);
                bridgeText = SafeRead(bridgeLogPath);

                var sb = new StringBuilder();
                sb.Append("# Generated: ")
                  .Append(DateTime.Now.ToString(TsFormat, CultureInfo.InvariantCulture))
                  .Append('\n').Append(headerInfo);
                foreach (var line in MergeInterleave(pluginText, bridgeText))
                    sb.Append(line).Append('\n');

                try {
                    string tmp = target + ".tmp";
                    File.WriteAllText(tmp, sb.ToString(), new UTF8Encoding(false));
                    if (File.Exists(target)) File.Delete(target);
                    File.Move(tmp, target);
                } catch { }
            }
        }

        // ---- pure, unit-tested core ----

        // Merge two log texts into one chronological list. Each output line is
        // prefixed with its source tag (PLG/BRG). Lines whose first field isn't a
        // timestamp (e.g. a stack-trace continuation) inherit the previous line's
        // time within their own source, so multi-line entries stay intact and in
        // order. Equal timestamps keep insertion order (stable), which preserves
        // both within-source ordering and a deterministic plugin-before-bridge tie.
        internal static List<string> MergeInterleave(string pluginText, string bridgeText) {
            var records = new List<Rec>();
            CollectRecords(pluginText, TagPlugin, records);
            CollectRecords(bridgeText, TagBridge, records);
            return records
                .Select((r, i) => new { r, i })
                .OrderBy(x => x.r.Stamp)
                .ThenBy(x => x.i)
                .Select(x => x.r.Tag + " | " + x.r.Raw)
                .ToList();
        }

        private struct Rec { public DateTime Stamp; public string Tag; public string Raw; }

        private static void CollectRecords(string text, string tag, List<Rec> into) {
            if (string.IsNullOrEmpty(text)) return;
            DateTime last = DateTime.MinValue;
            foreach (var raw in text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n')) {
                if (raw.Length == 0) continue;
                DateTime stamp;
                if (TryParseStamp(raw, out stamp)) last = stamp;
                else stamp = last; // continuation inherits its predecessor's time
                into.Add(new Rec { Stamp = stamp, Tag = tag, Raw = raw });
            }
        }

        private static bool TryParseStamp(string line, out DateTime stamp) {
            stamp = DateTime.MinValue;
            if (line == null || line.Length < TsLen) return false;
            return DateTime.TryParseExact(line.Substring(0, TsLen), TsFormat,
                CultureInfo.InvariantCulture, DateTimeStyles.None, out stamp);
        }

        private static string SafeRead(string path) {
            if (string.IsNullOrEmpty(path)) return "";
            try {
                if (!File.Exists(path)) return "";
                // Share ReadWrite: the bridge (and our own appender) may be writing
                // concurrently. A torn final line just fails the timestamp parse and
                // attaches to the prior entry — harmless.
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var r = new StreamReader(fs, Encoding.UTF8)) {
                    return r.ReadToEnd();
                }
            } catch { return ""; }
        }

        private static void RotateIfLarge(string path) {
            try {
                var fi = new FileInfo(path);
                if (fi.Exists && fi.Length > MaxBytes) {
                    string bak = path + ".1";
                    try { if (File.Exists(bak)) File.Delete(bak); } catch { }
                    File.Move(path, bak);
                }
            } catch { }
        }

        private static string BuildHeaderInfo(string pluginVersion) {
            var sb = new StringBuilder();
            sb.Append("# ==================== ACT Discord Triggers diagnostics ====================\n");
            sb.Append("# Plugin: v").Append(pluginVersion)
              .Append("   OS: ").Append(Environment.OSVersion)
              .Append("   CLR: ").Append(Environment.Version)
              .Append("   64-bit proc: ").Append(Environment.Is64BitProcess).Append('\n');
            sb.Append("# Tag legend: PLG = plugin (C#), BRG = bridge (node). Lines are time-ordered across both.\n");
            sb.Append("# ==========================================================================\n");
            return sb.ToString();
        }
    }
}
