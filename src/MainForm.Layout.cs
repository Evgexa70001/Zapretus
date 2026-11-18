using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace Zapretus {
    internal sealed partial class MainForm {
        readonly Label selectedProfile = new Label();
        readonly Label scanSummary = new Label();
        void UpdateSelectedProfile() {
            var profile = profiles.SelectedItem as Profile;
            selectedProfile.Text = profile == null ? "" : "По умолчанию: " + profile.ToString();
        }
        void BuildInterface() {
            Text = "Zapretus"; AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
            ClientSize = new Size(860, 910); MinimumSize = new Size(800, 860);
            StartPosition = FormStartPosition.CenterScreen;
            Font = new Font("Segoe UI", 10); BackColor = Theme.Background; ForeColor = Theme.Text;
            Icon = SystemIcons.Shield; DoubleBuffered = true;
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(28, 16, 28, 20), ColumnCount = 1, RowCount = 7 };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 174));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 14));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            Controls.Add(layout);

            var header = new Panel { Dock = DockStyle.Fill };
            header.Controls.Add(LabelAt("zapretus", 0, 0, 300, 42, 25, true, Theme.Text, Theme.Background));
            header.Controls.Add(LabelAt("Свободнее в сети. Проще в управлении.", 2, 47, 460, 24, 10, false, Theme.Muted, Theme.Background));
            var trayLink = new LinkLabel { Text = "Свернуть в трей  ↗", Width = 170, Dock = DockStyle.Right, TextAlign = ContentAlignment.MiddleRight, LinkColor = Theme.Muted, ActiveLinkColor = Theme.Accent, LinkBehavior = LinkBehavior.HoverUnderline };
            trayLink.LinkClicked += (s, e) => Hide(); header.Controls.Add(trayLink); layout.Controls.Add(header, 0, 0);

            var connection = new Card { Dock = DockStyle.Fill, Margin = new Padding(0, 0, 0, 14) };
            toggle.SetBounds(25, 34, 100, 100); connection.Controls.Add(toggle);
            connection.Controls.Add(LabelAt("ПОДКЛЮЧЕНИЕ", 150, 25, 400, 22, 9, true, Theme.Muted, Theme.Surface));
            state.SetBounds(148, 51, 554, 38); state.BackColor = Theme.Surface;
            state.Font = new Font("Segoe UI", 21, FontStyle.Bold); connection.Controls.Add(state);
            hint.SetBounds(150, 99, 550, 46); hint.BackColor = Theme.Surface;
            hint.ForeColor = Theme.Muted; hint.Font = new Font("Segoe UI", 10);
            connection.Controls.Add(hint);
            layout.Controls.Add(connection, 0, 1);

            var strategy = new Card { Dock = DockStyle.Fill, Margin = new Padding(0) };
            strategy.Controls.Add(LabelAt("Стратегия обхода", 24, 18, 350, 28, 12, true, Theme.Text, Theme.Surface));
            scanSummary.SetBounds(350, 22, 400, 24); scanSummary.Font = new Font("Segoe UI", 9);
            scanSummary.TextAlign = ContentAlignment.TopRight; scanSummary.BackColor = Theme.Surface;
            scanSummary.ForeColor = Theme.Accent; scanSummary.AutoEllipsis = true; strategy.Controls.Add(scanSummary);
            selectedProfile.SetBounds(24, 49, 690, 22); selectedProfile.Font = new Font("Segoe UI", 9);
            selectedProfile.BackColor = Theme.Surface; selectedProfile.ForeColor = Theme.Muted; strategy.Controls.Add(selectedProfile);
            var mediaLink = new LinkLabel { Text = "Ссылка для медиатеста  ↗", Dock = DockStyle.Bottom, Height = 26, TextAlign = ContentAlignment.BottomRight, BackColor = Theme.Surface, LinkColor = Theme.Muted, ActiveLinkColor = Theme.Accent, LinkBehavior = LinkBehavior.HoverUnderline };
            mediaLink.LinkClicked += (s, e) => EditVideoUrl();
            var actions = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 43, ColumnCount = 3, RowCount = 1, BackColor = Theme.Surface, Margin = Padding.Empty };
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 216));
            actions.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 184));
            profiles.Dock = DockStyle.Fill; profiles.Margin = new Padding(0, 5, 14, 0);
            profiles.DropDownStyle = ComboBoxStyle.DropDownList; profiles.FlatStyle = FlatStyle.Flat;
            profiles.BackColor = Color.FromArgb(35, 44, 59); profiles.ForeColor = Theme.Text;
            profiles.DrawMode = DrawMode.OwnerDrawFixed; profiles.ItemHeight = 27;
            profiles.AccessibleName = "Стратегия обхода";
            profiles.DrawItem += DrawProfile;
            scan.Text = "Подобрать стратегию"; scan.Dock = DockStyle.Fill; scan.Margin = new Padding(0, 0, 10, 0);
            check.Text = "Диагностика"; check.Dock = DockStyle.Fill; check.Margin = Padding.Empty;
            actions.Controls.Add(profiles, 0, 0); actions.Controls.Add(scan, 1, 0); actions.Controls.Add(check, 2, 0);
            strategy.Controls.Add(actions); strategy.Controls.Add(mediaLink); layout.Controls.Add(strategy, 0, 2);
            layout.Controls.Add(GameCard(), 0, 3);
            layout.Controls.Add(AutostartCard(), 0, 4);

            var journal = new Card { Dock = DockStyle.Fill, Margin = Padding.Empty };
            var journalHeader = new Panel { Dock = DockStyle.Top, Height = 40, BackColor = Theme.Surface };
            journalHeader.Controls.Add(LabelAt("Журнал событий", 0, 0, 250, 26, 12, true, Theme.Text, Theme.Surface));
            var folder = new LinkLabel { Text = "Открыть папку  ↗", Width = 160, Dock = DockStyle.Right, TextAlign = ContentAlignment.TopRight, LinkColor = Theme.Muted, ActiveLinkColor = Theme.Accent, LinkBehavior = LinkBehavior.HoverUnderline };
            folder.LinkClicked += (s, e) => Process.Start("explorer.exe", dataDir); journalHeader.Controls.Add(folder);
            logs.Dock = DockStyle.Fill; logs.Multiline = true; logs.ReadOnly = true;
            logs.ScrollBars = ScrollBars.Vertical; logs.BackColor = Theme.Surface;
            logs.ForeColor = Color.FromArgb(169, 188, 209); logs.BorderStyle = BorderStyle.None;
            logs.Font = new Font("Consolas", 9);
            journal.Controls.Add(logs); journal.Controls.Add(journalHeader); layout.Controls.Add(journal, 0, 6);
        }
        static Label LabelAt(string text, int x, int y, int w, int h, float size, bool bold, Color color, Color background) {
            return new Label { Text = text, Location = new Point(x, y), Size = new Size(w, h),
                Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular), ForeColor = color, BackColor = background };
        }
        void DrawProfile(object sender, DrawItemEventArgs e) {
            if (e.Index < 0) return;
            bool selected = (e.State & DrawItemState.Selected) != 0;
            using (var b = new SolidBrush(selected ? Color.FromArgb(50, 69, 81) : Color.FromArgb(35, 44, 59))) e.Graphics.FillRectangle(b, e.Bounds);
            TextRenderer.DrawText(e.Graphics, profiles.Items[e.Index].ToString(), profiles.Font,
                Rectangle.Inflate(e.Bounds, -8, 0), profiles.Enabled ? Theme.Text : Theme.Muted,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            e.DrawFocusRectangle();
        }
    }
}
