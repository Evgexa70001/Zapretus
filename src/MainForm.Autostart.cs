using System;
using System.Drawing;
using System.Windows.Forms;

namespace Zapretus {
    internal sealed partial class MainForm {
        readonly CheckBox autostartSwitch = new ModernSwitch();
        readonly Label autostartDescription = new Label();
        readonly LinkLabel autostartUpdate = new LinkLabel();
        bool loadingAutostart;
        Control AutostartSettingsRow() {
            autostartSwitch.Text = "Автозапуск";
            autostartSwitch.CheckedChanged += (s, e) => { if (!loadingAutostart) ChangeAutostart(autostartSwitch.Checked); };
            autostartUpdate.Text = "Привязать эту версию";
            autostartUpdate.LinkClicked += (s, e) => ChangeAutostart(true);
            return SettingsRow(autostartSwitch, autostartDescription, autostartUpdate);
        }
        void LoadAutostart() {
            loadingAutostart = true;
            try {
                var status = Autostart.Read(Application.ExecutablePath);
                autostartSwitch.Checked = status.Enabled; autostartSwitch.Enabled = true;
                autostartUpdate.Visible = status.Enabled && !status.CurrentExecutable;
                autostartDescription.Text = status.Enabled
                    ? status.CurrentExecutable ? "При входе в Windows · в трее · сохранённая стратегия включается автоматически" : "Автозапуск включён для другой сборки. Можно привязать эту версию."
                    : "Выключен · запуск в трее и автоматическое подключение после входа в Windows";
            } catch (Exception e) {
                autostartSwitch.Enabled = false; autostartUpdate.Visible = false;
                autostartDescription.Text = "Не удалось прочитать настройки автозапуска"; Log("Автозапуск: " + e.Message);
            } finally { loadingAutostart = false; }
        }
        void ChangeAutostart(bool enabled) {
            try {
                Autostart.SetEnabled(enabled, Application.ExecutablePath);
                Log(enabled ? "Автозапуск включён: при следующем входе в Windows приложение включит сохранённую стратегию в трее." : "Автозапуск выключен. Текущее подключение не изменено.");
            } catch (Exception e) { Log("Не удалось изменить автозапуск: " + e.Message); MessageBox.Show(this, e.Message, "Автозапуск"); }
            finally { LoadAutostart(); }
        }
        public void StartFromLogon() {
            // Create an invisible window for UI callbacks, without showing the main form.
            IntPtr window = Handle;
            BeginInvoke(new Action(async () => {
                Log("Автозапуск: включаем сохранённую стратегию.");
                await Toggle();
                if (!engine.Running) { Restore(); tray.ShowBalloonTip(5000, "Zapretus", "Подключение не запущено. Подробности в журнале.", ToolTipIcon.Warning); }
            }));
        }
    }
}
