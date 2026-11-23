using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace Zapretus {
    internal sealed partial class MainForm {
        const string HostsSource = "https://github.com/Flowseal/zapret-discord-youtube/blob/main/.service/hosts";
        // Discord entries from Flowseal's .service/hosts at tag 1.10.4.
        const string DiscordHosts = "# Zapretus Discord\r\n" +
            "162.159.138.232 discord.com\r\n" +
            "162.159.137.232 discord.com\r\n" +
            "162.159.128.233 discord.com\r\n" +
            "162.159.135.232 discord.com\r\n" +
            "162.159.138.232 updates.discord.com\r\n" +
            "162.159.137.232 updates.discord.com\r\n" +
            "162.159.128.233 updates.discord.com\r\n" +
            "162.159.135.232 updates.discord.com\r\n" +
            "# /Zapretus Discord\r\n";

        void ShowHostsSettings() {
            using (var loading = new CancellationTokenSource())
            using (var dialog = SettingsDialog("Настройка hosts", new Size(660, 465))) {
                var content = DialogContent(dialog, 152, 0, 58, 44);
                var instructions = DialogNote(
                    "Записи Flowseal для Discord, Telegram и GitHub.\n\n" +
                    "1. Скопируйте строки и откройте hosts.\n" +
                    "2. Вставьте блок в конец файла или замените старую версию.\n" +
                    "3. Сохраните файл через Ctrl+S и заново откройте нужный сервис.\n\n" +
                    "Для отмены удалите блок с пометками Zapretus и сохраните файл.", Theme.Text);
                var entries = new DarkTextBox { Dock = DockStyle.Fill, Margin = Padding.Empty,
                    ReadOnly = true, Multiline = true, WordWrap = false, ScrollBars = ScrollBars.Both,
                    Font = new Font("Consolas", 10) };
                var footer = Grid(2, 1);
                footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
                var status = TextLabel("Загружаем список Flowseal…", color: Theme.Muted);
                var source = new LinkLabel { Text = "Список на GitHub" };
                StyleLink(source);
                footer.Controls.Add(status, 0, 0);
                footer.Controls.Add(source, 1, 0);
                var copy = new ModernButton { Text = "Скопировать строки", Primary = true, Enabled = false };
                var open = new ModernButton { Text = "Открыть hosts" };
                copy.Click += (s, e) => {
                    try {
                        Clipboard.SetText(entries.Text);
                        copy.Text = "Скопировано";
                    } catch (ExternalException) {
                        entries.Focus(); entries.SelectAll();
                        MessageBox.Show(dialog, "Буфер обмена занят. Строки выделены, попробуйте скопировать их через Ctrl+C.",
                            "Zapretus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };
                open.Click += (s, e) => OpenHosts(dialog);
                source.LinkClicked += (s, e) => {
                    try {
                        using (Process.Start(new ProcessStartInfo(HostsSource) { UseShellExecute = true })) { }
                    } catch (Win32Exception e2) {
                        Log("Не удалось открыть источник hosts: " + e2.Message);
                        MessageBox.Show(dialog, "Откройте адрес в браузере.\n" + HostsSource,
                            "Zapretus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                };
                dialog.Shown += async (s, e) => {
                    try {
                        string block = await HostsList.DownloadAsync(loading.Token);
                        if (dialog.IsDisposed || !dialog.Visible) return;
                        entries.Text = block;
                        status.Text = "Полный список загружен с GitHub.";
                    } catch (Exception error) {
                        if (!(error is HttpRequestException || error is OperationCanceledException || error is InvalidDataException)) throw;
                        if (dialog.IsDisposed || !dialog.Visible) return;
                        Log("Не удалось загрузить список hosts: " + error.Message);
                        entries.Text = DiscordHosts;
                        status.Text = "Список не загрузился. Резервные записи для Discord.";
                    }
                    copy.Enabled = true;
                };
                dialog.FormClosed += (s, e) => loading.Cancel();
                content.Controls.Add(instructions, 0, 0);
                content.Controls.Add(InputFrame(entries, "Строки hosts"), 0, 1);
                content.Controls.Add(footer, 0, 2);
                content.Controls.Add(DialogActions(dialog, "Закрыть", copy, open), 0, 3);
                dialog.ShowDialog(this);
            }
        }

        void OpenHosts(IWin32Window owner) {
            string systemDirectory = Environment.SystemDirectory;
            string hostsPath = Path.Combine(systemDirectory, "drivers", "etc", "hosts");
            if (!File.Exists(hostsPath)) {
                MessageBox.Show(owner, "Файл hosts недоступен.\n" + hostsPath,
                    "Zapretus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try {
                var editor = new ProcessStartInfo(Path.Combine(systemDirectory, "notepad.exe"), "\"" + hostsPath + "\"") {
                    UseShellExecute = false
                };
                using (Process.Start(editor)) { }
            } catch (Win32Exception e) {
                Log("Не удалось открыть hosts: " + e.Message);
                MessageBox.Show(owner, "Не удалось открыть hosts в Блокноте.\n" + e.Message,
                    "Zapretus", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
