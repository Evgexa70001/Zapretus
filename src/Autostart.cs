using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Xml.Linq;

namespace Zapretus {
    internal sealed class AutostartStatus {
        public bool Enabled, CurrentExecutable;
    }
    internal static class Autostart {
        internal static readonly XNamespace Ns = "http://schemas.microsoft.com/windows/2004/02/mit/task";
        static string UserSid { get { using (var identity = WindowsIdentity.GetCurrent()) return identity.User.Value; } }
        static string TaskName { get { return "Zapretus.Autostart." + UserSid; } }
        // Task Scheduler logon trigger, interactive user token, highest available privileges.
        // No password is stored and registering this task does not run it.
        internal static string BuildXml(string executable, string sid) {
            if (!Path.IsPathRooted(executable)) throw new ArgumentException("Нужен абсолютный путь приложения.");
            return new XDocument(new XElement(Ns + "Task", new XAttribute("version", "1.2"),
                new XElement(Ns + "RegistrationInfo", new XElement(Ns + "Description", "Zapretus: запуск в трее и включение сохранённой стратегии при входе пользователя.")),
                new XElement(Ns + "Triggers", new XElement(Ns + "LogonTrigger",
                    new XElement(Ns + "Enabled", true), new XElement(Ns + "UserId", sid), new XElement(Ns + "Delay", "PT10S"))),
                new XElement(Ns + "Principals", new XElement(Ns + "Principal", new XAttribute("id", "User"),
                    new XElement(Ns + "UserId", sid), new XElement(Ns + "LogonType", "InteractiveToken"), new XElement(Ns + "RunLevel", "HighestAvailable"))),
                new XElement(Ns + "Settings",
                    new XElement(Ns + "MultipleInstancesPolicy", "IgnoreNew"),
                    new XElement(Ns + "DisallowStartIfOnBatteries", false), new XElement(Ns + "StopIfGoingOnBatteries", false),
                    new XElement(Ns + "StartWhenAvailable", true), new XElement(Ns + "Enabled", true),
                    new XElement(Ns + "ExecutionTimeLimit", "PT0S")),
                new XElement(Ns + "Actions", new XAttribute("Context", "User"), new XElement(Ns + "Exec",
                    new XElement(Ns + "Command", executable), new XElement(Ns + "Arguments", "--autostart"),
                    new XElement(Ns + "WorkingDirectory", Path.GetDirectoryName(executable)))))).ToString();
        }
        internal static bool MatchesExecutable(string xml, string executable) {
            var action = XDocument.Parse(xml).Root.Element(Ns + "Actions").Element(Ns + "Exec");
            return String.Equals(Path.GetFullPath((string)action.Element(Ns + "Command")), Path.GetFullPath(executable), StringComparison.OrdinalIgnoreCase)
                && (string)action.Element(Ns + "Arguments") == "--autostart";
        }
        internal static bool MissingTask(Exception e) {
            // .NET maps HRESULT_FROM_WIN32(ERROR_FILE_NOT_FOUND) to FileNotFoundException
            // for some COM calls; other runtimes surface COMException instead.
            return (e is FileNotFoundException || e is COMException)
                && e.HResult == unchecked((int)0x80070002);
        }
        static void Release(object value) { if (value != null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value); }
        public static AutostartStatus Read(string executable) {
            dynamic service = null, folder = null, task = null;
            try {
                service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service")); service.Connect(); folder = service.GetFolder("\\");
                try { task = folder.GetTask(TaskName); } catch (Exception e) { if (MissingTask(e)) return new AutostartStatus(); throw; }
                return new AutostartStatus { Enabled = (bool)task.Enabled, CurrentExecutable = MatchesExecutable((string)task.Xml, executable) };
            } finally { Release((object)task); Release((object)folder); Release((object)service); }
        }
        public static void SetEnabled(bool enabled, string executable) {
            dynamic service = null, folder = null, task = null;
            try {
                service = Activator.CreateInstance(Type.GetTypeFromProgID("Schedule.Service")); service.Connect(); folder = service.GetFolder("\\");
                if (enabled) task = folder.RegisterTask(TaskName, BuildXml(executable, UserSid), 6, UserSid, null, 3, null);
                else {
                    try { folder.DeleteTask(TaskName, 0); } catch (Exception e) { if (!MissingTask(e)) throw; }
                }
            } finally { Release((object)task); Release((object)folder); Release((object)service); }
        }
    }
}
