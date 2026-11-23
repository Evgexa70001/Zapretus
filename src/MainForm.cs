using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Zapretus {
    internal sealed partial class MainForm : Form {
        readonly Engine engine;
        readonly ComboBox profiles = new DarkComboBox();
        readonly PowerButton toggle = new PowerButton();
        readonly ToolTip powerTip = new ToolTip();
        const string ScanButtonText = "Подобрать &стратегию";
        const string CancelButtonText = "Отменить операцию";
        readonly Button scan = new ModernButton(), check = new ModernButton();
        readonly Label state = new Label(), hint = new Label();
        readonly TextBox logs = new DarkTextBox();
        readonly NotifyIcon tray;
        readonly Icon appIcon, trayIcon;
        readonly System.Windows.Forms.Timer timer;
        readonly string dataDir, settingsPath;
        readonly object logLock = new object();
        CancellationTokenSource cancellation;
        bool busy, closing;
        string videoUrl = "";

        public MainForm() {
            dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Zapretus");
            Directory.CreateDirectory(dataDir); settingsPath = Path.Combine(dataDir, "profile.txt");
            engine = new Engine(AppDomain.CurrentDomain.BaseDirectory); engine.Output += Log;
            using (var iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("Zapretus.AppIcon"))
                appIcon = new Icon(iconStream);
            trayIcon = new Icon(appIcon, SystemInformation.SmallIconSize);
            BuildInterface();
            Icon = appIcon;
            LoadGameFilter();
            LoadAutostart();
            foreach (var profile in Engine.Load(AppDomain.CurrentDomain.BaseDirectory)) profiles.Items.Add(profile);
            if (profiles.Items.Count == 0) throw new InvalidOperationException("Нет профилей. Повторите сборку.");
            profiles.SelectedIndex = 0;
            if (File.Exists(settingsPath)) {
                string selected = File.ReadAllText(settingsPath).Trim();
                for (int i = 0; i < profiles.Items.Count; i++)
                    if (((Profile)profiles.Items[i]).Name == selected) profiles.SelectedIndex = i;
            }
            profiles.SelectedIndexChanged += (s, e) => SaveProfile();
            toggle.Click += async (s, e) => await Toggle();
            scan.Click += async (s, e) => { if (busy) { cancellation.Cancel(); return; } await Scan(); };
            check.Click += async (s, e) => await Check();
            var menu = new ContextMenuStrip(); menu.Items.Add("Открыть", null, (s, e) => Restore());
            menu.Items.Add("Выключить и выйти", null, (s, e) => Close());
            tray = new NotifyIcon { Icon = trayIcon, Text = "Zapretus — выключен", Visible = true, ContextMenuStrip = menu };
            tray.DoubleClick += (s, e) => Restore();
            Resize += (s, e) => { if (WindowState == FormWindowState.Minimized) Hide(); };
            timer = new System.Windows.Forms.Timer { Interval = 1000 };
            timer.Tick += (s, e) => RefreshState(); timer.Start();
            FormClosing += OnClosing;
            FormClosed += (s, e) => {
                timer.Dispose();
                tray.Dispose();
                trayIcon.Dispose();
                appIcon.Dispose();
                powerTip.Dispose();
                engine.Dispose();
            };
            RefreshState();
            Log("Приложение запущено.");
        }
        void Restore() { Show(); WindowState = FormWindowState.Normal; Activate(); }
        void SaveProfile() {
            try { File.WriteAllText(settingsPath, ((Profile)profiles.SelectedItem).Name); }
            catch (Exception e) { Log("Не удалось сохранить профиль: " + e.Message); }
        }
        void Log(string line) {
            if (closing || IsDisposed) return;
            if (InvokeRequired) { try { BeginInvoke(new Action<string>(Log), line); } catch (InvalidOperationException) {} return; }
            string formatted = DateTime.Now.ToString("HH:mm:ss") + "  " + line + Environment.NewLine;
            if (logs.TextLength > 100000) logs.Clear(); logs.AppendText(formatted);
            try { lock (logLock) File.AppendAllText(Path.Combine(dataDir, "session-" + DateTime.Now.ToString("yyyy-MM-dd") + ".log"), formatted); }
            catch (IOException) {} catch (UnauthorizedAccessException) {}
        }
        void RefreshState() {
            bool running = engine.Running;
            state.Text = busy ? "Идёт операция" : running ? "Движок запущен" : "Движок выключен";
            state.ForeColor = running && !busy ? Theme.Success : Theme.Text;
            hint.Text = busy ? "Ход операции и результаты видны в журнале." : running
                ? "Проверьте видео на YouTube и голосовой канал Discord."
                : "При включении запустится выбранная стратегия.";
            toggle.Active = running;
            toggle.Text = running ? "&Выключить" : "&Включить";
            string powerAction = running ? "Выключить движок" : "Включить движок";
            if (toggle.AccessibleName != powerAction) {
                toggle.AccessibleName = powerAction;
                powerTip.SetToolTip(toggle, powerAction);
            }
            tray.Text = running ? "Zapretus — движок работает" : "Zapretus — выключен";
            profiles.Enabled = !running && !busy; toggle.Enabled = !busy; check.Enabled = !busy;
            gameSwitch.Enabled = !running && !busy; gameSettings.Enabled = !running && !busy;
            mediaLink.Enabled = !busy;
        }
        void BeginWork() { busy = true; cancellation = new CancellationTokenSource(); RefreshState(); }
        void EndWork() { busy = false; cancellation.Dispose(); cancellation = null; scan.Text = ScanButtonText; scan.Enabled = true; RefreshState(); }
        async Task Start(Profile p, CancellationToken token) {
            Log("Game Filter: " + gameFilter.Description);
            engine.Start(p, gameFilter); await Task.Delay(1500, token);
            if (!engine.Running) throw new InvalidOperationException("Движок завершился. Подробности в журнале.");
        }
        async Task Toggle() {
            BeginWork(); scan.Enabled = false;
            try {
                if (engine.Running) { engine.Stop(); Log("Движок остановлен."); }
                else { SaveProfile(); await Start((Profile)profiles.SelectedItem, cancellation.Token); Log("Движок запущен. Проверьте видео и голос вручную."); }
            } catch (Exception e) { engine.Stop(); Log("Ошибка: " + e.Message); }
            finally { EndWork(); }
        }
        async Task Check() {
            BeginWork(); scan.Text = CancelButtonText;
            try {
                using (var report = new ScanReport(dataDir)) {
                    var result = await Probe.Run(Log, videoUrl, cancellation.Token);
                    report.Add(engine.Running ? ((Profile)profiles.SelectedItem).Name : "Без обхода", "manual", result);
                    Log("Диагностика: " + result.Summary + ". Отчёт: " + report.Path);
                }
            }
            catch (OperationCanceledException) { Log("Проверка отменена."); }
            catch (Exception e) { Log("Ошибка проверки: " + e.Message); }
            finally { EndWork(); }
        }
        void OnClosing(object sender, FormClosingEventArgs e) {
            if (busy) {
                cancellation.Cancel(); e.Cancel = true; Restore();
                Log("Операция отменяется. После её завершения закройте окно повторно."); return;
            }
            closing = true; engine.Stop();
        }
    }
}
