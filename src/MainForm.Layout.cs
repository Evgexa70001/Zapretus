using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Zapretus {
    internal sealed partial class MainForm {
        const int ConnectionHeight = 126;
        const int StrategyHeight = 176;
        const int SettingsHeight = 210;
        const int ControlsHeight = ConnectionHeight + StrategyHeight + SettingsHeight;
        const int CompactJournalHeight = 268;
        readonly Label scanSummary = new Label();
        readonly LinkLabel mediaLink = new LinkLabel { Text = "Ссылка для медиатеста" };
        Panel workspaceViewport;
        TableLayoutPanel workspace, controlsPane;
        Panel journalPane;
        bool? stackedWorkspace;

        void BuildInterface() {
            Text = "Zapretus";
            AutoScaleDimensions = new SizeF(96, 96);
            AutoScaleMode = AutoScaleMode.Dpi;
            var workArea = Screen.PrimaryScreen.WorkingArea;
            ClientSize = new Size(Math.Min(1080, workArea.Width - 32), Math.Min(680, workArea.Height - 72));
            MinimumSize = new Size(580, 450);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10);
            BackColor = Theme.Background;
            ForeColor = Theme.Text;
            DoubleBuffered = true;

            var shell = Grid(1, 2);
            shell.Padding = new Padding(24, 0, 24, 16);
            shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 76));
            shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(shell);

            var header = Grid(2, 1);
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 172));
            header.Controls.Add(TextLabel("Zapretus", 24, true), 0, 0);
            var trayButton = new ModernButton { Text = "Свернуть в трей", Dock = DockStyle.Fill,
                Margin = new Padding(0, 16, 0, 16) };
            trayButton.Click += (s, e) => Hide();
            header.Controls.Add(trayButton, 1, 0);
            shell.Controls.Add(header, 0, 0);

            controlsPane = Grid(1, 3);
            controlsPane.RowStyles.Add(new RowStyle(SizeType.Absolute, ConnectionHeight));
            controlsPane.RowStyles.Add(new RowStyle(SizeType.Absolute, StrategyHeight));
            controlsPane.RowStyles.Add(new RowStyle(SizeType.Absolute, SettingsHeight));
            controlsPane.Controls.Add(BuildConnection(), 0, 0);
            controlsPane.Controls.Add(BuildStrategy(), 0, 1);
            controlsPane.Controls.Add(BuildSettings(), 0, 2);
            journalPane = BuildJournal();

            workspace = Grid(2, 1);
            workspace.Dock = DockStyle.Top;
            workspace.Controls.Add(controlsPane, 0, 0);
            workspace.Controls.Add(journalPane, 1, 0);
            workspaceViewport = new DarkScrollPanel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
            workspaceViewport.Controls.Add(workspace);
            shell.Controls.Add(workspaceViewport, 0, 1);
            workspaceViewport.SizeChanged += (s, e) => ArrangeWorkspace();
            Shown += (s, e) => { ArrangeWorkspace(); toggle.Select(); };
            ArrangeWorkspace();
        }

        void ArrangeWorkspace() {
            using (var graphics = workspaceViewport.CreateGraphics()) {
                float scale = graphics.DpiX / 96f;
                bool stacked = workspaceViewport.Width < 810 * scale;
                if (stackedWorkspace != stacked) {
                    stackedWorkspace = stacked;
                    workspace.SuspendLayout();
                    workspace.RowStyles.Clear();
                    workspace.ColumnStyles.Clear();
                    workspace.ColumnCount = stacked ? 1 : 2;
                    workspace.RowCount = stacked ? 2 : 1;
                    workspace.SetCellPosition(journalPane, new TableLayoutPanelCellPosition(stacked ? 0 : 1, stacked ? 1 : 0));
                    workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, stacked ? 100 : 59));
                    if (!stacked) workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 41));
                    workspace.RowStyles.Add(new RowStyle(stacked ? SizeType.Absolute : SizeType.Percent, stacked ? ControlsHeight * scale : 100));
                    if (stacked) workspace.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                    journalPane.Margin = stacked
                        ? new Padding(0, (int)(16 * scale), 0, 0)
                        : new Padding((int)(24 * scale), 0, 0, 0);
                    workspace.ResumeLayout();
                }
                workspace.Height = Math.Max(workspaceViewport.ClientSize.Height, (int)((ControlsHeight + (stacked ? CompactJournalHeight : 0)) * scale));
            }
        }

        Control BuildConnection() {
            var connection = Grid(2, 2);
            connection.BackColor = Theme.Surface;
            connection.Padding = new Padding(16, 12, 16, 12);
            connection.Margin = new Padding(0, 0, 0, 12);
            connection.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            connection.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 88));
            connection.RowStyles.Add(new RowStyle(SizeType.Absolute, 56));
            connection.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            StyleLabel(state, 18, true);
            StyleLabel(hint, 10, color: Theme.Muted);
            hint.TextAlign = ContentAlignment.TopLeft;
            toggle.Anchor = AnchorStyles.None;
            toggle.Size = new Size(80, 80);
            toggle.Margin = new Padding(8, 0, 0, 0);
            connection.Controls.Add(state, 0, 0);
            connection.Controls.Add(toggle, 1, 0);
            connection.Controls.Add(hint, 0, 1);
            connection.SetRowSpan(toggle, 2);
            return connection;
        }

        Control BuildStrategy() {
            var section = Grid(2, 4);
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            section.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            section.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            section.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
            section.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var title = TextLabel("Стратегия обхода", 13, true);
            StyleLink(mediaLink);
            mediaLink.LinkClicked += (s, e) => EditVideoUrl();
            profiles.Dock = DockStyle.Fill;
            profiles.Margin = new Padding(0, 4, 0, 4);
            profiles.BackColor = Theme.Field;
            profiles.ForeColor = Theme.Text;
            profiles.DropDownStyle = ComboBoxStyle.DropDownList;
            profiles.DrawMode = DrawMode.OwnerDrawFixed;
            profiles.ItemHeight = 25;
            profiles.MaxDropDownItems = 12;
            profiles.AccessibleName = "Стратегия обхода";
            profiles.DrawItem += DrawProfile;

            scan.Text = ScanButtonText;
            scan.Dock = DockStyle.Fill;
            scan.Margin = new Padding(0, 0, 4, 4);
            check.Text = "&Диагностика";
            check.Dock = DockStyle.Fill;
            check.Margin = new Padding(4, 0, 0, 4);
            StyleLabel(scanSummary, 10, color: Theme.Muted);
            scanSummary.TextAlign = ContentAlignment.TopLeft;
            scanSummary.Padding = new Padding(0, 4, 0, 0);
            scanSummary.Text = "Подбор сохранит стратегию, если она улучшит результаты тестов.";

            section.Controls.Add(title, 0, 0);
            section.Controls.Add(mediaLink, 1, 0);
            section.Controls.Add(profiles, 0, 1);
            section.SetColumnSpan(profiles, 2);
            section.Controls.Add(scan, 0, 2);
            section.Controls.Add(check, 1, 2);
            section.Controls.Add(scanSummary, 0, 3);
            section.SetColumnSpan(scanSummary, 2);
            return section;
        }

        Control BuildSettings() {
            var section = Grid(2, 4);
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
            section.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            section.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));
            section.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            var title = TextLabel("Настройки", 13, true);
            var hosts = new LinkLabel { Text = "Настроить hosts" };
            StyleLink(hosts);
            hosts.LinkClicked += (s, e) => ShowHostsSettings();
            section.Controls.Add(title, 0, 0);
            section.Controls.Add(hosts, 1, 0);
            var game = GameSettingsRow();
            var autostart = AutostartSettingsRow();
            section.Controls.Add(game, 0, 1);
            section.SetColumnSpan(game, 2);
            section.Controls.Add(autostart, 0, 2);
            section.SetColumnSpan(autostart, 2);
            return section;
        }

        static Control SettingsRow(CheckBox setting, Label description, LinkLabel action) {
            var row = Grid(2, 3);
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
            row.RowStyles.Add(new RowStyle(SizeType.Absolute, 1));
            row.RowStyles.Add(new RowStyle(SizeType.Absolute, 37));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var rule = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Theme.Border };
            row.Controls.Add(rule, 0, 0);
            row.SetColumnSpan(rule, 2);
            setting.Dock = DockStyle.Fill;
            setting.Margin = Padding.Empty;
            StyleLabel(description, 10, color: Theme.Muted);
            description.TextAlign = ContentAlignment.TopLeft;
            StyleLink(action);
            row.Controls.Add(setting, 0, 1);
            row.Controls.Add(action, 1, 1);
            row.Controls.Add(description, 0, 2);
            row.SetColumnSpan(description, 2);
            return row;
        }

        Panel BuildJournal() {
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Surface, Padding = new Padding(16, 8, 16, 16) };
            var section = Grid(2, 2);
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            section.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));
            section.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
            section.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            var folder = new LinkLabel { Text = "Папка отчётов" };
            StyleLink(folder);
            folder.LinkClicked += (s, e) => Process.Start("explorer.exe", dataDir);
            logs.Multiline = true;
            logs.ReadOnly = true;
            logs.ScrollBars = ScrollBars.Vertical;
            logs.Font = new Font("Consolas", 10);
            section.Controls.Add(TextLabel("Журнал событий", 13, true), 0, 0);
            section.Controls.Add(folder, 1, 0);
            var journal = InputFrame(logs, "Журнал событий");
            section.Controls.Add(journal, 0, 1);
            section.SetColumnSpan(journal, 2);
            panel.Controls.Add(section);
            return panel;
        }

        static TableLayoutPanel Grid(int columns, int rows) {
            return new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = columns, RowCount = rows, Margin = Padding.Empty };
        }
        static Label TextLabel(string text, float size = 10, bool bold = false, Color? color = null) {
            var label = new Label { Text = text };
            StyleLabel(label, size, bold, color);
            return label;
        }
        static void StyleLabel(Label label, float size, bool bold = false, Color? color = null) {
            label.Dock = DockStyle.Fill;
            label.Margin = Padding.Empty;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular);
            label.ForeColor = color ?? Theme.Text;
        }
        static void StyleLink(LinkLabel link) {
            link.Dock = DockStyle.Fill;
            link.TextAlign = ContentAlignment.MiddleRight;
            link.Margin = new Padding(4, 0, 0, 0);
            link.LinkColor = Theme.Accent;
            link.ActiveLinkColor = Theme.Text;
            link.VisitedLinkColor = Theme.Accent;
            link.DisabledLinkColor = Theme.Disabled;
            link.LinkBehavior = LinkBehavior.AlwaysUnderline;
        }
        Control InputFrame(TextBox input, string name) {
            var frame = new Panel { Dock = DockStyle.Fill, Margin = Padding.Empty, BackColor = Theme.ControlBorder,
                Padding = new Padding(2) };
            input.AccessibleName = name;
            input.BackColor = Theme.Field;
            input.ForeColor = Theme.Text;
            input.BorderStyle = BorderStyle.None;
            input.Dock = DockStyle.Fill;
            input.GotFocus += (s, e) => frame.BackColor = Theme.Accent;
            input.LostFocus += (s, e) => frame.BackColor = Theme.ControlBorder;
            if (!input.Multiline) {
                input.Font = Font;
                frame.Dock = DockStyle.Top;
                frame.Height = input.PreferredHeight + frame.Padding.Vertical;
            }
            frame.Controls.Add(input);
            return frame;
        }
        static void DrawProfile(object sender, DrawItemEventArgs e) {
            var combo = (ComboBox)sender;
            if (e.Index < 0 || e.Index >= combo.Items.Count) return;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (var brush = new SolidBrush(selected ? Theme.Selection : Theme.Field))
                e.Graphics.FillRectangle(brush, e.Bounds);
            TextRenderer.DrawText(e.Graphics, combo.Items[e.Index].ToString(), combo.Font,
                Rectangle.Inflate(e.Bounds, -6, 0), combo.Enabled ? Theme.Text : Theme.Disabled,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }
    }
}
