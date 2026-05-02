using System;
using System.Threading.Tasks;

namespace ActDiscordTriggers.Tests {
    // Polyfill for Task.WaitAsync(TimeSpan) — net6+ only on the BCL, but the
    // production code targets net48. Keeps test bodies readable.
    internal static class TaskExtensions {
        public static async Task<T> WaitAsync<T>(this Task<T> task, TimeSpan timeout) {
            var winner = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
            if (winner != task) throw new TimeoutException();
            return await task.ConfigureAwait(false);
        }

        public static async Task WaitAsync(this Task task, TimeSpan timeout) {
            var winner = await Task.WhenAny(task, Task.Delay(timeout)).ConfigureAwait(false);
            if (winner != task) throw new TimeoutException();
            await task.ConfigureAwait(false);
        }
    }
}
