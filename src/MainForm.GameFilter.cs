using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Zapretus {
    internal sealed partial class MainForm {
        GameFilter gameFilter = new GameFilter();
        readonly CheckBox gameSwitch = new ModernSwitch();
        readonly LinkLabel gameSettings = new LinkLabel();
        readonly Label gameDescription = new Label();
        bool loadingGameFilter;
        string GamePath { get { return Path.Combine(dataDir, "game-filter.xml"); } }
        void LoadGameFilter() {
            try { gameFilter = GameFilter.Load(GamePath); }
            catch (Exception e) { Log("Настройки Game Filter сброшены: " + e.Message); }
            UpdateGameFilter();
        }
        void UpdateGameFilter() {
            loadingGameFilter = true; gameSwitch.Checked = gameFilter.Enabled; loadingGameFilter = false;
            gameDescription.Text = gameFilter.Description + " • адреса из ipset-all.txt";
        }
        void SaveGameFilter() {
            try { gameFilter.Save(GamePath); Log("Game Filter: " + gameFilter.Description); }
            catch (Exception e) { Log("Не удалось сохранить Game Filter: " + e.Message); }
            UpdateGameFilter();
        }
        Card GameCard() {
            var card = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 14, 0, 0) };
            gameSwitch.Text = "Game Filter"; gameSwitch.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            gameSwitch.ForeColor = Theme.Text; gameSwitch.BackColor = Theme.Surface;
            gameSwitch.SetBounds(24, 15, 240, 32); gameSwitch.Cursor = Cursors.Hand;
            gameSwitch.CheckedChanged += (s, e) => {
                if (loadingGameFilter) return;
                gameFilter.Enabled = gameSwitch.Checked; SaveGameFilter();
            };
            gameDescription.SetBounds(26, 50, 620, 24); gameDescription.Font = new Font("Segoe UI", 9);
            gameDescription.ForeColor = Theme.Muted; gameDescription.BackColor = Theme.Surface;
            gameSettings.Text = "Настроить порты  ↗"; gameSettings.Width = 180; gameSettings.Dock = DockStyle.Right;
            gameSettings.TextAlign = ContentAlignment.TopRight; gameSettings.BackColor = Theme.Surface;
            gameSettings.LinkColor = Theme.Muted; gameSettings.ActiveLinkColor = Theme.Accent;
            gameSettings.LinkBehavior = LinkBehavior.HoverUnderline;
            gameSettings.LinkClicked += (s, e) => EditGameFilter();
            card.Controls.Add(gameSwitch); card.Controls.Add(gameDescription); card.Controls.Add(gameSettings); return card;
        }
        void EditGameFilter() {
            if (busy || engine.Running) return;
            using (var dialog = new Form { Text = "Game Filter", ClientSize = new Size(570, 285), StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
                BackColor = Theme.Background, ForeColor = Theme.Text, Font = Font }) {
                var mode = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(24, 58), Size = new Size(180, 30) };
                mode.Items.AddRange(new object[] { "UDP", "TCP", "TCP + UDP" }); mode.SelectedItem = gameFilter.Mode;
                var ports = new TextBox { Text = gameFilter.Ports, Location = new Point(228, 58), Size = new Size(315, 30) };
                dialog.Controls.Add(LabelAt("Протокол", 24, 24, 180, 25, 10, false, Theme.Muted, Theme.Background));
                dialog.Controls.Add(LabelAt("Порты и диапазоны", 228, 24, 315, 25, 10, false, Theme.Muted, Theme.Background));
                dialog.Controls.Add(LabelAt("Например: 1024-65535 или 27015,27020-27030.\nОбрабатываются адреса из engine/lists/ipset-all.txt.\nИзменения применятся при следующем включении движка.", 24, 110, 520, 80, 10, false, Theme.Muted, Theme.Background));
                var save = new ModernButton { Text = "Сохранить", Primary = true, Location = new Point(24, 215), Size = new Size(180, 44) };
                save.Click += (s, e) => {
                    try {
                        string normalized = GameFilter.NormalizePorts(ports.Text);
                        gameFilter.Ports = normalized; gameFilter.Mode = (string)mode.SelectedItem;
                        SaveGameFilter(); dialog.Close();
                    } catch (ArgumentException error) { MessageBox.Show(dialog, error.Message, "Проверьте порты"); }
                };
                dialog.Controls.Add(mode); dialog.Controls.Add(ports); dialog.Controls.Add(save); dialog.AcceptButton = save; dialog.ShowDialog(this);
            }
        }
    }
}
