using System;
using System.Linq;
using System.Xml.Linq;
using System.IO;
using System.Runtime.InteropServices;

namespace Zapretus {
    internal static class AutostartTests {
        public static void Run(Action<bool, string> assert) {
            assert(Autostart.MissingTask(new FileNotFoundException()), "mapped missing task is normal disabled state");
            assert(Autostart.MissingTask(new COMException("missing", unchecked((int)0x80070002))), "COM missing task is normal disabled state");
            assert(!Autostart.MissingTask(new UnauthorizedAccessException()), "access denied remains visible error");
            assert(!Autostart.MissingTask(new COMException("denied", unchecked((int)0x80070005))), "COM access denied not swallowed");
            assert(!Autostart.MissingTask(new InvalidOperationException()), "unexpected errors not swallowed");
            string executable = @"C:\Apps\Zapretus & tools\Zapretus.exe", sid = "S-1-5-21-100-200-300-1001";
            string xml = Autostart.BuildXml(executable, sid);
            var root = XDocument.Parse(xml).Root; var ns = Autostart.Ns;
            var principal = root.Element(ns + "Principals").Element(ns + "Principal");
            var trigger = root.Element(ns + "Triggers").Elements().Single();
            var actions = root.Element(ns + "Actions"); var action = actions.Elements().Single();
            assert(trigger.Name == ns + "LogonTrigger", "autorun uses logon, not boot or registration trigger");
            assert((string)trigger.Element(ns + "UserId") == sid, "trigger restricted to current user");
            assert((string)principal.Element(ns + "UserId") == sid, "principal matches user");
            assert((string)principal.Element(ns + "LogonType") == "InteractiveToken", "interactive session without storing password");
            assert((string)principal.Element(ns + "RunLevel") == "HighestAvailable", "elevated engine permissions");
            assert((string)action.Element(ns + "Command") == executable, "executable path escaping round trip");
            assert((string)action.Element(ns + "WorkingDirectory") == @"C:\Apps\Zapretus & tools", "working directory explicit");
            assert((string)action.Element(ns + "Arguments") == "--autostart", "background start is explicit");
            assert((string)root.Element(ns + "Settings").Element(ns + "ExecutionTimeLimit") == "PT0S", "no automatic timeout kills engine");
            assert(!(bool)root.Element(ns + "Settings").Element(ns + "DisallowStartIfOnBatteries"), "laptop battery does not disable startup");
            assert(Autostart.MatchesExecutable(xml, executable), "current binding detected");
            assert(!Autostart.MatchesExecutable(xml, @"C:\Apps\Zapretus-new.exe"), "old executable binding detected");
            assert(!Autostart.MatchesExecutable(xml.Replace("--autostart", "--other"), executable), "invalid startup arguments detected");
            bool rejected = false;
            try { Autostart.BuildXml("relative.exe", sid); } catch (ArgumentException) { rejected = true; }
            assert(rejected, "relative executable rejected");
        }
    }
}
