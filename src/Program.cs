using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using System.Linq;

namespace Zapretus {
    static class Program {
        [STAThread] static void Main(string[] args) {
            bool autostart = args.Contains("--autostart");
            bool first;
            using (var mutex = new Mutex(true, "Local\\Zapretus.Desktop", out first)) {
                if (!first) { if (!autostart) MessageBox.Show("Zapretus уже открыт. Проверьте значок в трее."); return; }
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try {
                    var form = new MainForm();
                    if (autostart) {
                        using (var context = new ApplicationContext()) {
                            form.FormClosed += (s, e) => context.ExitThread();
                            form.StartFromLogon(); Application.Run(context);
                        }
                    } else Application.Run(form);
                }
                catch (Exception e) { MessageBox.Show(e.Message, "Zapretus — ошибка"); }
            }
        }
    }
}
