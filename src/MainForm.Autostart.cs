using System;
using System.Drawing;
using System.Windows.Forms;

namespace Zapretus {
    internal sealed partial class MainForm {
        readonly CheckBox autostartSwitch = new ModernSwitch();
        readonly Label autostartDescription = new Label();
        readonly LinkLabel autostartUpdate = new LinkLabel();
        bool loadingAutostart;
        Card AutostartCard() {
            var card = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 14, 0, 0) };
            autostartSwitch.Text = "Автозапуск"; autostartSwitch.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            autostartSwitch.SetBounds(24, 15, 260, 32);
            autostartSwitch.CheckedChanged += (s, e) => { if (!loadingAutostart) ChangeAutostart(autostartSwitch.Checked); };
            autostartDescription.SetBounds(26, 50, 710, 24); autostartDescription.Font = new Font("Segoe UI", 9);
            autostartDescription.ForeColor = Theme.Muted; autostartDescription.BackColor = Theme.Surface;
            autostartUpdate.Text = "Привязать эту версию  ↗"; autostartUpdate.Width = 220; autostartUpdate.Dock = DockStyle.Right;
            autostartUpdate.TextAlign = ContentAlignment.TopRight; autostartUpdate.BackColor = Theme.Surface;
            autostartUpdate.LinkColor = Theme.Muted; autostartUpdate.ActiveLinkColor = Theme.Accent;
            autostartUpdate.LinkBehavior = LinkBehavior.HoverUnderline;
            autostartUpdate.LinkClicked += (s, e) => ChangeAutostart(true);
            card.Controls.Add(autostartSwitch); card.Controls.Add(autostartDescription); card.Controls.Add(autostartUpdate);
            return card;
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
