using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace Zapretus {
    internal sealed class Profile {
        public string Name, Arguments;
        public bool Experimental;
        public override string ToString() { return Name == "general" ? "Основная" : Name.Replace("general ", ""); }
    }
    internal sealed class Engine : IDisposable {
        readonly string root;
        Process process;
        bool started;
        IntPtr job;
        public event Action<string> Output;
        public Engine(string root) { this.root = root; }
        public bool Running { get { return process != null && !process.HasExited; } }
        public static Profile[] Load(string root) {
            var upstream = XDocument.Load(Path.Combine(root, "profiles.xml")).Root.Elements("profile")
                .Select(x => new Profile { Name = (string)x.Attribute("name"), Arguments = x.Value })
                .OrderBy(x => x.Name == "general" ? "" : x.Name).ToArray();
            var lab = XDocument.Load(Path.Combine(root, "experimental-profiles.xml")).Root.Elements("profile")
                .Select(x => new Profile { Name = (string)x.Attribute("name"), Arguments = x.Value, Experimental = true });
            return upstream.Concat(lab).ToArray();
        }
        public void Start(Profile profile, GameFilter gameFilter) {
            Stop();
            if (Process.GetProcessesByName("winws").Length > 0 || Process.GetProcessesByName("winws2").Length > 0)
                throw new InvalidOperationException("Другой zapret уже работает. Остановите его перед запуском.");
            string bin = Path.Combine(root, "engine", "bin") + Path.DirectorySeparatorChar;
            string lists = Path.Combine(root, "engine", "lists") + Path.DirectorySeparatorChar;
            string args = gameFilter.Apply(profile.Arguments).Replace("{BIN}", bin).Replace("{LISTS}", lists);
            job = CreateJobObject(IntPtr.Zero, null);
            if (job == IntPtr.Zero) throw new InvalidOperationException("Не удалось создать группу процесса.");
            var info = new JobInfo(); info.Basic.LimitFlags = 0x2000; // terminate child when UI exits
            if (!SetInformationJobObject(job, 9, ref info, (uint)Marshal.SizeOf(typeof(JobInfo)))) {
                Stop(); throw new InvalidOperationException("Не удалось настроить завершение движка.");
            }
            process = new Process { StartInfo = new ProcessStartInfo(Path.Combine(bin, "winws.exe"), args) {
                WorkingDirectory = bin, UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true } };
            process.OutputDataReceived += Read;
            process.ErrorDataReceived += Read;
            try {
                process.Start();
                started = true;
                if (!AssignProcessToJobObject(job, process.Handle))
                    throw new InvalidOperationException("Не удалось привязать движок к приложению.");
                process.BeginOutputReadLine(); process.BeginErrorReadLine();
            } catch { Stop(); throw; }
        }
        void Read(object sender, DataReceivedEventArgs e) { if (e.Data != null && Output != null) Output(e.Data); }
        public void Stop() {
            if (process != null) {
                try { if (started && !process.HasExited) { process.Kill(); process.WaitForExit(3000); } }
                finally { process.Dispose(); process = null; started = false; }
            }
            if (job != IntPtr.Zero) { CloseHandle(job); job = IntPtr.Zero; }
        }
        public void Dispose() { Stop(); }
        [StructLayout(LayoutKind.Sequential)] struct BasicInfo {
            public long ProcessTime, JobTime; public uint LimitFlags;
            public UIntPtr MinWorking, MaxWorking; public uint ActiveProcesses;
            public UIntPtr Affinity; public uint Priority, Scheduling;
        }
        [StructLayout(LayoutKind.Sequential)] struct IoInfo { public ulong A, B, C, D, E, F; }
        [StructLayout(LayoutKind.Sequential)] struct JobInfo {
            public BasicInfo Basic; public IoInfo Io;
            public UIntPtr ProcessMemory, JobMemory, PeakProcessMemory, PeakJobMemory;
        }
        [DllImport("kernel32.dll", CharSet=CharSet.Unicode)] static extern IntPtr CreateJobObject(IntPtr attr, string name);
        [DllImport("kernel32.dll")] static extern bool SetInformationJobObject(IntPtr job, int cls, ref JobInfo info, uint length);
        [DllImport("kernel32.dll")] static extern bool AssignProcessToJobObject(IntPtr job, IntPtr process);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
    }
}
