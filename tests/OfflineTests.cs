using System;
using System.IO;
using System.Text;

namespace Zapretus {
    static class OfflineTests {
        static int count;
        static void Assert(bool condition, string name) {
            if (!condition) throw new Exception("FAILED: " + name);
            count++;
        }
        static ProbeItem Item(string name, bool pass, int weight) { return new ProbeItem { Name = name, Passed = pass, Weight = weight }; }
        static ProbeResult Result(params ProbeItem[] items) { return new ProbeResult { Items = items }; }
        static void Main(string[] startupArgs) {
            if (startupArgs.Length == 1 && startupArgs[0] == "--read-autostart") {
                var state = Autostart.Read(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Zapretus-autostart-fix.exe"));
                Console.WriteLine("Task Scheduler read succeeded. Enabled=" + state.Enabled + "; current executable=" + state.CurrentExecutable + ". No tasks created/updated/deleted.");
                return;
            }
            AutostartTests.Run(Assert);
            Assert(GameFilter.NormalizePorts("27015, 27020-27030") == "27015,27020-27030", "port list normalized");
            foreach (var invalid in new[] { "", "0", "65536", "70000-70001", "200-100", "80,", "-1", "80 --new", "1;2", "1, 2 3" }) {
                bool rejected = false;
                try { GameFilter.NormalizePorts(invalid); } catch (ArgumentException) { rejected = true; }
                Assert(rejected, "invalid port list rejected");
            }
            foreach (var profile in Engine.Load(AppDomain.CurrentDomain.BaseDirectory)) {
                Assert(new GameFilter().Apply(profile.Arguments) == profile.Arguments, "off preserves " + profile.Name);
                foreach (var mode in new[] { "TCP", "UDP", "TCP + UDP" }) {
                    string args = new GameFilter { Enabled = true, Mode = mode, Ports = "27015,27020-27030" }.Apply(profile.Arguments);
                    foreach (var protocol in new[] { "tcp", "udp" }) {
                        bool selected = mode == "TCP + UDP" || mode.Equals(protocol, StringComparison.OrdinalIgnoreCase);
                        Assert(args.Contains("--filter-" + protocol + "=27015,27020-27030") == selected, "correct protocol expansion " + profile.Name);
                        if (selected) Assert(System.Text.RegularExpressions.Regex.IsMatch(args, "--wf-" + protocol + "=[^ ]*27015,27020-27030"), "capture includes game ports");
                    }
                    Assert(args.Contains("ipset-all.txt"), "IPSet scope preserved");
                }
            }
            var baseline = Result(Item("API", true, 2), Item("Media", false, 5));
            Assert(Result(Item("API", true, 2), Item("Media", true, 5)).Improves(baseline), "real gain accepted");
            Assert(!Result(Item("API", false, 2), Item("Media", true, 5)).Improves(baseline), "higher score with regression rejected");
            Assert(!baseline.Improves(baseline), "same result not improvement");
            var mainProfileResult = Result(Item("API", true, 2), Item("CDN", true, 1), Item("Image", true, 1), Item("WebSocket", true, 3), Item("Web", false, 1));
            var simpleFakeResult = Result(Item("API", true, 2), Item("CDN", true, 1), Item("Image", true, 1), Item("WebSocket", true, 3), Item("Web", false, 1));
            Assert(simpleFakeResult.Score == mainProfileResult.Score, "reported main/simple fake scan has equal score");
            Assert(!simpleFakeResult.Improves(mainProfileResult), "last equally passing candidate is not a new winner");
            Assert(Result(Item("API", true, 2), Item("CDN", true, 1), Item("Image", true, 1), Item("WebSocket", true, 3), Item("Web", true, 1)).Improves(mainProfileResult), "genuinely better simple fake would replace main");
            Assert(!Result(Item("API", true, 2), Item("Media", false, 5)).Preserves(Result(Item("API", true, 2), Item("Media", true, 5))), "confirmation cannot lose initial gain");
            Assert(!Result(Item("Media", true, 5)).Improves(baseline), "missing baseline endpoint rejected");
            var skipped = new ProbeItem { Name = "Media", Skipped = true, Weight = 5 };
            Assert(Result(Item("API", true, 2), skipped).Score == 2, "skipped excluded from score");
            Assert(Result(Item("API", true, 2), skipped).AllPassed, "skipped excluded from required tests");
            Assert(Probe.ValidVideoUrl("https://rr1---sn-example.googlevideo.com/videoplayback?expire=123&sig=secret"), "valid media URL accepted");
            foreach (string url in new[] { "https://googlevideo.com.evil.test/videoplayback", "http://rr1.googlevideo.com/videoplayback", "https://rr1.googlevideo.com:8443/videoplayback", "https://user:pass@rr1.googlevideo.com/videoplayback", "https://youtube.com/watch?v=x", "file:///tmp/test" })
                Assert(!Probe.ValidVideoUrl(url), "bad media URL rejected");
            Assert(!Probe.ValidateBody("video", "text/html", new byte[65536]), "HTML error is not video");
            Assert(!Probe.ValidateBody("video", "video/mp4", new byte[100]), "tiny video response rejected");
            Assert(Probe.ValidateBody("video", "video/mp4", new byte[65536]), "media content threshold accepted");
            Assert(!Probe.ValidateBody("image", "image/png", new byte[1024]), "fake image content rejected");
            Assert(!Probe.ValidateBody("json", "application/json", Encoding.UTF8.GetBytes("{\"error\":\"blocked\"}")), "JSON error rejected");
            Assert(Probe.ValidateBody("json", "application/json", Encoding.UTF8.GetBytes("{\"url\":\"wss://gateway.discord.gg\"}")), "Discord API shape accepted");
            string dir = Path.Combine(Path.GetTempPath(), "zapretus-offline-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try {
                string settings = Path.Combine(dir, "game-filter.xml");
                new GameFilter { Enabled = true, Mode = "TCP + UDP", Ports = "27015,27020-27030" }.Save(settings);
                var restored = GameFilter.Load(settings);
                Assert(restored.Enabled && restored.Mode == "TCP + UDP" && restored.Ports == "27015,27020-27030", "game filter settings round trip");
                File.Delete(settings);
                string path;
                using (var report = new ScanReport(dir)) {
                    path = report.Path;
                    report.Add("LAB, \"split\"", "trial", Result(new ProbeItem { Name = "Media", Detail = "HTTP 403", Skipped = true }));
                }
                string csv = File.ReadAllText(path);
                Assert(csv.Contains("\"LAB, \"\"split\"\"\""), "CSV quoting preserved");
                Assert(csv.Contains("false,true"), "CSV distinguishes skips from failures");
                File.Delete(path);
            } finally { Directory.Delete(dir); }
            Console.WriteLine(count + " offline assertions passed. No UI, engine, or network execution.");
        }
    }
}
