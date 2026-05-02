// DiscordBridge launcher — a tiny native exe that spawns node.exe with bundle.js
// and the original args. The plugin (BridgeProcess.cs) only knows it spawns
// "DiscordBridge.exe <pipe-name>" and reads "BRIDGE_READY" from stdout. We
// inherit stdin/stdout/stderr handles so the plugin sees the child's output
// directly. Forwards exit code and signals.
//
// We use a launcher (instead of Node's SEA) because SEA's embedderRequire only
// resolves built-in modules and the SEA-bundled main script. External requires
// like @snazzah/davey (the native DAVE module) can't be loaded from a sibling
// node_modules folder under SEA. Plain node.exe + bundle.js works correctly.
//
// Lifecycle: we put node.exe in a Win32 Job Object with KILL_ON_JOB_CLOSE so
// that if this launcher is hard-killed (Process.Kill / TerminateProcess / Task
// Manager / parent ACT crash), the kernel kills node.exe with us. Without this,
// node owns the named pipe and the plugin never sees a pipe-broken signal —
// only ProcessExit handlers fire on a clean exit, and TerminateProcess skips
// those entirely.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

string exeDir = Path.GetDirectoryName(Environment.ProcessPath!) ?? Directory.GetCurrentDirectory();
string nodeExe = Path.Combine(exeDir, "node.exe");
string bundleJs = Path.Combine(exeDir, "bundle.js");

if (!File.Exists(nodeExe)) {
    Console.Error.WriteLine($"BRIDGE_FATAL launcher: node.exe not found at {nodeExe}");
    return 10;
}
if (!File.Exists(bundleJs)) {
    Console.Error.WriteLine($"BRIDGE_FATAL launcher: bundle.js not found at {bundleJs}");
    return 11;
}

// Forward our argv to node, prepended with the bundle path.
string[] argv = Environment.GetCommandLineArgs();
StringBuilder argLine = new();
argLine.Append('"').Append(bundleJs).Append('"');
for (int i = 1; i < argv.Length; i++) {
    argLine.Append(' ').Append(QuoteArg(argv[i]));
}

ProcessStartInfo psi = new() {
    FileName = nodeExe,
    Arguments = argLine.ToString(),
    UseShellExecute = false,
    CreateNoWindow = false,   // inherit parent console
    WorkingDirectory = exeDir,
    // Don't redirect — let child inherit parent's stdin/stdout/stderr so the
    // plugin's RedirectStandardOutput on us captures node's BRIDGE_READY line.
    RedirectStandardInput = false,
    RedirectStandardOutput = false,
    RedirectStandardError = false,
};

// Job object FIRST, so even if Process.Start races with our death, the child is
// born already in the job (we assign before letting it run further — Process.Start
// returns once the child is created, and AssignProcessToJobObject works on a
// running process; the small window before assignment is acceptable for our use).
nint job = JobObject.Create();

Process child;
try {
    child = Process.Start(psi)!;
} catch (System.Exception ex) {
    Console.Error.WriteLine($"BRIDGE_FATAL launcher: failed to start node: {ex.Message}");
    return 12;
}

try {
    JobObject.Assign(job, child.Handle);
} catch (System.Exception ex) {
    Console.Error.WriteLine($"BRIDGE_FATAL launcher: AssignProcessToJobObject failed: {ex.Message}");
    try { child.Kill(true); } catch { }
    return 13;
}

// Best-effort clean-shutdown handlers — these fire on Ctrl-C and on graceful
// process exit. Hard kills bypass them; that's what the job object is for.
Console.CancelKeyPress += (_, e) => {
    e.Cancel = true;
    try { if (!child.HasExited) child.Kill(true); } catch { }
};
AppDomain.CurrentDomain.ProcessExit += (_, _) => {
    try { if (!child.HasExited) child.Kill(true); } catch { }
};

child.WaitForExit();
return child.ExitCode;

static string QuoteArg(string s) {
    if (s.IndexOfAny(new[] { ' ', '"' }) < 0) return s;
    return "\"" + s.Replace("\"", "\\\"") + "\"";
}

static class JobObject {
    private const int JobObjectExtendedLimitInformation = 9;
    private const uint JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE = 0x2000;

    [StructLayout(LayoutKind.Sequential)]
    private struct IO_COUNTERS {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_BASIC_LIMIT_INFORMATION {
        public long PerProcessUserTimeLimit;
        public long PerJobUserTimeLimit;
        public uint LimitFlags;
        public nuint MinimumWorkingSetSize;
        public nuint MaximumWorkingSetSize;
        public uint ActiveProcessLimit;
        public nuint Affinity;
        public uint PriorityClass;
        public uint SchedulingClass;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct JOBOBJECT_EXTENDED_LIMIT_INFORMATION {
        public JOBOBJECT_BASIC_LIMIT_INFORMATION BasicLimitInformation;
        public IO_COUNTERS IoInfo;
        public nuint ProcessMemoryLimit;
        public nuint JobMemoryLimit;
        public nuint PeakProcessMemoryUsed;
        public nuint PeakJobMemoryUsed;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateJobObject(nint lpJobAttributes, string? lpName);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetInformationJobObject(nint hJob, int JobObjectInfoClass, nint lpJobObjectInfo, uint cbJobObjectInfoLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AssignProcessToJobObject(nint hJob, nint hProcess);

    public static nint Create() {
        nint h = CreateJobObject(0, null);
        if (h == 0) throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "CreateJobObject failed");
        var info = new JOBOBJECT_EXTENDED_LIMIT_INFORMATION();
        info.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
        int size = Marshal.SizeOf<JOBOBJECT_EXTENDED_LIMIT_INFORMATION>();
        nint p = Marshal.AllocHGlobal(size);
        try {
            Marshal.StructureToPtr(info, p, false);
            if (!SetInformationJobObject(h, JobObjectExtendedLimitInformation, p, (uint)size)) {
                throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "SetInformationJobObject failed");
            }
        } finally {
            Marshal.FreeHGlobal(p);
        }
        return h;
    }

    public static void Assign(nint job, nint process) {
        if (!AssignProcessToJobObject(job, process)) {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(), "AssignProcessToJobObject failed");
        }
    }
}
