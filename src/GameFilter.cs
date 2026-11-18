using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Zapretus {
    internal sealed class GameFilter {
        public bool Enabled;
        public string Mode = "UDP", Ports = "1024-65535";
        public static string NormalizePorts(string text) {
            var parts = (text ?? "").Split(',');
            if (parts.Length == 0 || parts.Length > 64) throw new ArgumentException("Укажите порты или диапазоны через запятую.");
            return String.Join(",", parts.Select(part => {
                string value = part.Trim();
                if (!Regex.IsMatch(value, @"^[1-9][0-9]{0,4}(-[1-9][0-9]{0,4})?$"))
                    throw new ArgumentException("Пример диапазона: 1024-65535 или 27015,27020-27030.");
                var ends = value.Split('-');
                int first = Int32.Parse(ends[0], CultureInfo.InvariantCulture);
                int last = ends.Length == 2 ? Int32.Parse(ends[1], CultureInfo.InvariantCulture) : first;
                if (last > 65535 || first > last) throw new ArgumentException("Порты должны быть от 1 до 65535; начало не больше конца.");
                return value;
            }));
        }
        public string Description { get { return Enabled ? Mode + " / " + Ports : "выключен"; } }
        public string Apply(string args) {
            if (!Enabled) return args;
            if (Mode != "TCP" && Mode != "UDP" && Mode != "TCP + UDP") throw new ArgumentException("Неизвестный режим Game Filter.");
            string ports = NormalizePorts(Ports);
            foreach (string protocol in new[] { "tcp", "udp" }) {
                bool active = Mode == "TCP + UDP" || Mode.Equals(protocol, StringComparison.OrdinalIgnoreCase);
                if (!active) continue;
                // Upstream's disabled game-filter port is exactly 12, not part of another number/range.
                bool existing = Regex.IsMatch(args, @"--filter-" + protocol + @"=12(?=\s|$)");
                args = Regex.Replace(args, @"--wf-" + protocol + @"=([0-9,\-]+)", m => {
                    var basePorts = m.Groups[1].Value.Split(',').Where(x => x != "12");
                    return "--wf-" + protocol + "=" + String.Join(",", basePorts.Concat(new[] { ports }));
                });
                if (existing) args = Regex.Replace(args, @"--filter-" + protocol + @"=12(?=\s|$)", "--filter-" + protocol + "=" + ports);
                else {
                    // LAB has no upstream game sections: append scoped game rules.
                    string scope = " --ipset=\"{LISTS}ipset-all.txt\" --ipset-exclude=\"{LISTS}ipset-exclude.txt\" --ipset-exclude=\"{LISTS}ipset-exclude-user.txt\"";
                    args += " --new --filter-" + protocol + "=" + ports + scope;
                    args += protocol == "tcp"
                        ? " --dpi-desync=multisplit --dpi-desync-split-pos=1 --dpi-desync-any-protocol=1 --dpi-desync-cutoff=n3"
                        : " --dpi-desync=fake --dpi-desync-repeats=6 --dpi-desync-any-protocol=1 --dpi-desync-fake-unknown-udp=\"{BIN}ACTIVE_GAME_UDP.bin\" --dpi-desync-cutoff=n2";
                }
            }
            return args;
        }
        public static GameFilter Load(string path) {
            if (!File.Exists(path)) return new GameFilter();
            var root = XDocument.Load(path).Root;
            string mode = (string)root.Attribute("mode");
            if (mode != "TCP" && mode != "UDP" && mode != "TCP + UDP") throw new ArgumentException("Неверный режим Game Filter.");
            return new GameFilter { Enabled = (bool)root.Attribute("enabled"), Mode = mode, Ports = NormalizePorts((string)root.Attribute("ports")) };
        }
        public void Save(string path) {
            Ports = NormalizePorts(Ports);
            new XDocument(new XElement("game-filter", new XAttribute("enabled", Enabled), new XAttribute("mode", Mode), new XAttribute("ports", Ports))).Save(path);
        }
    }
}
