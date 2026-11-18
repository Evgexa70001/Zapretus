using System;
using System.IO;
using System.Text;

namespace Zapretus {
    internal sealed class ScanReport : IDisposable {
        readonly StreamWriter writer;
        public readonly string Path;
        public ScanReport(string directory) {
            Path = System.IO.Path.Combine(directory, "scan-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".csv");
            writer = new StreamWriter(Path, false, new UTF8Encoding(true));
            writer.WriteLine("time,profile,stage,test,passed,skipped,bytes,elapsed_ms,detail");
            writer.AutoFlush = true;
        }
        static string Quote(string value) { return "\"" + value.Replace("\"", "\"\"") + "\""; }
        public void Add(string profile, string stage, ProbeResult result) {
            foreach (var item in result.Items) writer.WriteLine(String.Join(",", new[] {
                Quote(DateTime.Now.ToString("s")), Quote(profile), Quote(stage), Quote(item.Name),
                item.Passed ? "true" : "false", item.Skipped ? "true" : "false", item.Bytes.ToString(), item.Milliseconds.ToString(), Quote(item.Detail ?? "") }));
        }
        public void Error(string profile, string stage, string message) {
            Add(profile, stage, new ProbeResult { Items = new[] { new ProbeItem { Name = "Движок / подбор", Detail = message } } });
        }
        public void Dispose() { writer.Dispose(); }
    }
}
