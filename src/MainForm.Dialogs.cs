using System;
using System.Drawing;
using System.Windows.Forms;

namespace Zapretus {
    internal sealed partial class MainForm {
        Form SettingsDialog(string title, Size size) {
            var dialog = new Form {
                Text = title, ClientSize = size, Font = Font, BackColor = Theme.Background, ForeColor = Theme.Text,
                AutoScaleDimensions = new SizeF(96, 96), AutoScaleMode = AutoScaleMode.Dpi,
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
                ShowInTaskbar = false, StartPosition = FormStartPosition.CenterParent
            };
            dialog.HandleCreated += (s, e) => ApplyWindowColors(dialog.Handle);
            return dialog;
        }

        static TableLayoutPanel DialogContent(Form dialog, params int[] rows) {
            var content = Grid(1, rows.Length);
            content.Padding = new Padding(20);
            foreach (int height in rows)
                content.RowStyles.Add(new RowStyle(height == 0 ? SizeType.Percent : SizeType.Absolute, height == 0 ? 100 : height));
            dialog.Controls.Add(content);
            return content;
        }

        static Control DialogActions(Form dialog, string closeText, params ModernButton[] buttons) {
            var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false, Margin = Padding.Empty };
            var close = new ModernButton { Text = closeText, DialogResult = DialogResult.Cancel };
            dialog.CancelButton = close;
            if (buttons.Length > 0) actions.Controls.Add(buttons[0]);
            actions.Controls.Add(close);
            for (int i = 1; i < buttons.Length; i++) actions.Controls.Add(buttons[i]);
            for (int i = 0; i < actions.Controls.Count; i++) {
                var button = actions.Controls[i];
                button.TabIndex = actions.Controls.Count - i - 1;
                int width = TextRenderer.MeasureText(button.Text, button.Font).Width;
                button.Size = new Size(Math.Max(104, width + 28), 44);
                button.Margin = i == 0 ? Padding.Empty : new Padding(0, 0, 8, 0);
            }
            return actions;
        }

        static Label DialogNote(string text, Color color) {
            var label = TextLabel(text, color: color);
            label.TextAlign = ContentAlignment.TopLeft;
            return label;
        }
    }
}
