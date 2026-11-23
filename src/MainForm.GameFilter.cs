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
            gameDescription.Text = gameFilter.Enabled
                ? "Включён · " + gameFilter.Description + " · адреса из ipset-all.txt"
                : "Выключен · фильтрация по списку ipset-all.txt";
        }
        void SaveGameFilter() {
            try { gameFilter.Save(GamePath); Log("Game Filter: " + gameFilter.Description); }
            catch (Exception e) { Log("Не удалось сохранить Game Filter: " + e.Message); }
            UpdateGameFilter();
        }
        Control GameSettingsRow() {
            gameSwitch.Text = "Game Filter";
            gameSwitch.CheckedChanged += (s, e) => {
                if (loadingGameFilter) return;
                gameFilter.Enabled = gameSwitch.Checked;
                SaveGameFilter();
            };
            gameSettings.Text = "Настроить порты";
            gameSettings.LinkClicked += (s, e) => EditGameFilter();
            return SettingsRow(gameSwitch, gameDescription, gameSettings);
        }
        void EditGameFilter() {
            if (busy || engine.Running) return;
            using (var dialog = SettingsDialog("Настройка Game Filter", new Size(556, 286))) {
                var content = DialogContent(dialog, 54, 36, 58, 0, 44);
                var fields = Grid(2, 2);
                fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 152));
                fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));
                fields.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                var mode = new DarkComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill,
                    Margin = new Padding(0, 0, 12, 0), AccessibleName = "Протокол",
                    BackColor = Theme.Field, ForeColor = Theme.Text, DrawMode = DrawMode.OwnerDrawFixed };
                mode.DrawItem += DrawProfile;
                mode.Items.AddRange(new object[] { "UDP", "TCP", "TCP + UDP" });
                mode.SelectedItem = gameFilter.Mode;
                var ports = new DarkTextBox { Text = gameFilter.Ports, Dock = DockStyle.Fill, Margin = Padding.Empty };
                fields.Controls.Add(TextLabel("Протокол"), 0, 0);
                fields.Controls.Add(TextLabel("Порты и диапазоны"), 1, 0);
                fields.Controls.Add(mode, 0, 1);
                fields.Controls.Add(InputFrame(ports, "Порты и диапазоны"), 1, 1);
                var error = DialogNote("", Theme.Error);
                var save = new ModernButton { Text = "Сохранить", Primary = true };
                save.Click += (s, e) => {
                    try {
                        string normalized = GameFilter.NormalizePorts(ports.Text);
                        gameFilter.Ports = normalized;
                        gameFilter.Mode = (string)mode.SelectedItem;
                        SaveGameFilter();
                        dialog.Close();
                    } catch (ArgumentException exception) {
                        error.Text = "Проверьте порты. " + exception.Message;
                        ports.AccessibleDescription = error.Text;
                        ports.Focus();
                    }
                };
                content.Controls.Add(fields, 0, 0);
                content.Controls.Add(DialogNote("Например: 1024-65535 или 27015,27020-27030.", Theme.Muted), 0, 1);
                content.Controls.Add(DialogNote("Фильтр использует адреса из ipset-all.txt. Изменения вступят в силу при следующем запуске движка.", Theme.Muted), 0, 2);
                content.Controls.Add(error, 0, 3);
                content.Controls.Add(DialogActions(dialog, "Отмена", save), 0, 4);
                dialog.AcceptButton = save;
                dialog.ShowDialog(this);
            }
        }
    }
}
