using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
namespace Zapretus {
    static class NetworkTests {
        static readonly string Root = AppDomain.CurrentDomain.BaseDirectory;
        static readonly string LogPath = Path.Combine(Root, "network-tests.log");
        static void Log(string message) {
            lock (LogPath) File.AppendAllText(LogPath, DateTime.Now.ToString("HH:mm:ss") + " " + message + Environment.NewLine);
        }
        static int Main() {
            try { Run().GetAwaiter().GetResult(); return 0; }
            catch (Exception e) { Log("FATAL " + e); return 1; }
        }
        static async Task Run() {
            File.WriteAllText(LogPath, "Live network tests. Game Filter disabled.\r\n");
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            if (System.Diagnostics.Process.GetProcessesByName("winws").Length > 0 ||
                System.Diagnostics.Process.GetProcessesByName("winws2").Length > 0)
                throw new InvalidOperationException("Another engine is running; no processes were stopped.");
            using (var deadline = new CancellationTokenSource(TimeSpan.FromMinutes(8)))
            using (var report = new ScanReport(Root))
            using (var engine = new Engine(Root)) {
                Log("CSV " + report.Path);
                engine.Output += Log;
                for (int i = 1; i <= 2; i++) {
                    var baseline = await Probe.Run(Log, null, deadline.Token);
                    report.Add("BASELINE", "baseline-" + i, baseline);
                    Log("BASELINE " + i + " SCORE " + baseline.Score);
                }
                foreach (var profile in Engine.Load(Root).Where(p => p.Experimental)) {
                    for (int round = 1; round <= 2; round++) {
                        Log("START " + profile.Name + " ROUND " + round);
                        try {
                            engine.Start(profile, new GameFilter());
                            await Task.Delay(1500, deadline.Token);
                            if (!engine.Running) throw new Exception("Engine exited before probing");
                            var result = await Probe.Run(Log, null, deadline.Token);
                            report.Add(profile.Name, "round-" + round, result);
                            Log("RESULT " + profile.Name + " ROUND " + round + " SCORE " + result.Score + " ALL " + result.AllPassed);
                        } catch (Exception e) { Log("ERROR " + profile.Name + " " + e.Message); }
                        finally { engine.Stop(); }
                        await Task.Delay(1000, deadline.Token);
                    }
                }
            }
            Log("COMPLETE. All test engines stopped.");
        }
    }
}
