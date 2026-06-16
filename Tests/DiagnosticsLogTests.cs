using System.Collections.Generic;
using System.Linq;
using DiscordAPI;
using Xunit;

namespace ActDiscordTriggers.Tests {
    // DiagnosticsLog.MergeInterleave is the only piece with real logic worth
    // testing: combining the plugin (PLG) and bridge (BRG) logs into one
    // chronological, source-tagged timeline. The file IO around it is best-effort
    // and swallow-on-error by design, so it's not unit-tested here.
    public class DiagnosticsLogTests {
        // Linked source compiles into this test assembly, so the `internal` merge
        // helper is reachable directly.
        private static List<string> Merge(string plugin, string bridge) =>
            DiagnosticsLog.MergeInterleave(plugin, bridge);

        [Fact]
        public void Interleaves_both_sources_by_timestamp() {
            string plugin =
                "2026-06-15 20:00:00.100 INFO Speak timing: synth=180ms ipc=2ms\n" +
                "2026-06-15 20:00:00.500 INFO done\n";
            string bridge =
                "2026-06-15 20:00:00.300 INFO --> SpeakPcm reqId=1 pcmBytes=384000\n";

            var merged = Merge(plugin, bridge);

            Assert.Equal(3, merged.Count);
            Assert.StartsWith("PLG | 2026-06-15 20:00:00.100", merged[0]);
            Assert.StartsWith("BRG | 2026-06-15 20:00:00.300", merged[1]);
            Assert.StartsWith("PLG | 2026-06-15 20:00:00.500", merged[2]);
        }

        [Fact]
        public void Tags_each_line_with_its_source() {
            var merged = Merge(
                "2026-06-15 20:00:00.000 INFO a\n",
                "2026-06-15 20:00:01.000 INFO b\n");
            Assert.Contains(merged, l => l.StartsWith("PLG | ") && l.Contains("INFO a"));
            Assert.Contains(merged, l => l.StartsWith("BRG | ") && l.Contains("INFO b"));
        }

        [Fact]
        public void Continuation_lines_stay_attached_after_their_header() {
            // A stack trace has no leading timestamp; it must sort immediately after
            // its (timestamped) header line, not float to the top.
            string bridge =
                "2026-06-15 20:00:00.200 ERROR boom :: Error: kaboom\n" +
                "    at Object.<anonymous> (bundle.js:1:1)\n" +
                "    at Module._compile (node:internal:1:1)\n";
            string plugin =
                "2026-06-15 20:00:00.100 INFO before\n" +
                "2026-06-15 20:00:00.300 INFO after\n";

            var merged = Merge(plugin, bridge);

            Assert.StartsWith("PLG | 2026-06-15 20:00:00.100", merged[0]);
            Assert.StartsWith("BRG | 2026-06-15 20:00:00.200 ERROR boom", merged[1]);
            Assert.Contains("at Object.<anonymous>", merged[2]);
            Assert.Contains("at Module._compile", merged[3]);
            Assert.StartsWith("PLG | 2026-06-15 20:00:00.300", merged[4]);
        }

        [Fact]
        public void Equal_timestamps_keep_plugin_before_bridge_and_within_source_order() {
            string plugin =
                "2026-06-15 20:00:00.000 INFO p1\n" +
                "2026-06-15 20:00:00.000 INFO p2\n";
            string bridge =
                "2026-06-15 20:00:00.000 INFO b1\n";

            var merged = Merge(plugin, bridge);

            // Stable order: all plugin records were collected before bridge records.
            Assert.Equal("p1", LastToken(merged[0]));
            Assert.Equal("p2", LastToken(merged[1]));
            Assert.Equal("b1", LastToken(merged[2]));
        }

        [Fact]
        public void Skips_blank_lines_and_tolerates_empty_sources() {
            string plugin =
                "2026-06-15 20:00:00.000 INFO only\n\n\n";
            var merged = Merge(plugin, "");
            Assert.Single(merged);
            Assert.Equal("only", LastToken(merged[0]));
        }

        [Fact]
        public void Both_empty_yields_empty_merge() {
            Assert.Empty(Merge("", ""));
            Assert.Empty(Merge(null, null));
        }

        private static string LastToken(string line) => line.Split(' ').Last();
    }
}
