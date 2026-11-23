using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Zapretus {
    internal static class Theme {
        public static readonly Color Background = Color.FromArgb(22, 23, 26);
        public static readonly Color Surface = Color.FromArgb(32, 34, 38);
        public static readonly Color Field = Color.FromArgb(24, 25, 29);
        public static readonly Color Border = Color.FromArgb(59, 62, 70);
        public static readonly Color ControlBorder = Color.FromArgb(112, 117, 130);
        public static readonly Color Text = Color.FromArgb(241, 242, 245);
        public static readonly Color Muted = Color.FromArgb(182, 187, 198);
        public static readonly Color Disabled = Color.FromArgb(134, 140, 154);
        public static readonly Color Accent = Color.FromArgb(172, 187, 255);
        public static readonly Color OnAccent = Color.FromArgb(24, 30, 55);
        public static readonly Color AccentHover = Color.FromArgb(199, 209, 255);
        public static readonly Color AccentPressed = Color.FromArgb(144, 163, 245);
        public static readonly Color Selection = Color.FromArgb(51, 59, 86);
        public static readonly Color Hover = Color.FromArgb(47, 50, 58);
        public static readonly Color Success = Color.FromArgb(151, 218, 179);
        public static readonly Color SuccessHover = Color.FromArgb(179, 232, 199);
        public static readonly Color SuccessPressed = Color.FromArgb(126, 198, 157);
        public static readonly Color Error = Color.FromArgb(255, 172, 168);
    }

    internal class ModernButton : Button {
        public bool Primary {
            get { return primary; }
            set { if (primary != value) { primary = value; Invalidate(); } }
        }
        bool primary, hover, pressed;
        protected bool Hovered { get { return hover; } }
        protected bool Pressed { get { return pressed; } }

        public ModernButton() {
            FlatStyle = FlatStyle.Flat;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 10, FontStyle.Bold);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); hover = true; Invalidate(); }
        protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); hover = false; Invalidate(); }
        protected override void OnMouseDown(MouseEventArgs e) {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left) { pressed = true; Invalidate(); }
        }
        protected override void OnMouseUp(MouseEventArgs e) {
            pressed = false; Invalidate();
            base.OnMouseUp(e);
        }
        protected override void OnMouseCaptureChanged(EventArgs e) {
            base.OnMouseCaptureChanged(e);
            if (!Capture) { pressed = false; Invalidate(); }
        }
        protected override void OnKeyDown(KeyEventArgs e) {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Space) { pressed = true; Invalidate(); }
        }
        protected override void OnKeyUp(KeyEventArgs e) {
            pressed = false; Invalidate();
            base.OnKeyUp(e);
        }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); pressed = false; Invalidate(); }
        protected override void OnPaint(PaintEventArgs e) {
            Color fill = !Enabled ? Theme.Surface : Primary
                ? (pressed ? Theme.AccentPressed : hover ? Theme.AccentHover : Theme.Accent)
                : (pressed ? Theme.Selection : hover ? Theme.Hover : Theme.Surface);
            e.Graphics.Clear(fill);
            using (var pen = new Pen(!Enabled ? Theme.Border : Primary ? fill : Theme.ControlBorder))
                e.Graphics.DrawRectangle(pen, 0, 0, Width - 1, Height - 1);
            Color text = !Enabled ? Theme.Disabled : Primary ? Theme.OnAccent : Theme.Text;
            var flags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
            TextRenderer.DrawText(e.Graphics, Text, Font, Rectangle.Inflate(ClientRectangle, -6, 0), text, flags);
            if (Focused) {
                using (var pen = new Pen(Primary ? Theme.OnAccent : Theme.Accent, 2))
                    e.Graphics.DrawRectangle(pen, Rectangle.Inflate(ClientRectangle, -4, -4));
            }
        }
    }

    internal sealed class PowerButton : ModernButton {
        bool active;
        public bool Active {
            get { return active; }
            set { if (active != value) { active = value; Invalidate(); } }
        }

        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics;
            g.Clear(Theme.Surface);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float size = Math.Min(Width, Height);
            float scale = size / 80f;
            float x = Width / 2f, y = Height / 2f;
            var face = new RectangleF(x - 34 * scale, y - 34 * scale, 68 * scale, 68 * scale);
            Color fill = !Enabled ? Theme.Background : Active
                ? (Pressed ? Theme.SuccessPressed : Hovered ? Theme.SuccessHover : Theme.Success)
                : (Pressed ? Theme.Border : Hovered ? Theme.Hover : Theme.Field);
            Color outline = !Enabled ? Theme.Border : Active ? fill : Theme.ControlBorder;
            Color glyph = !Enabled ? Theme.Disabled : Active ? Theme.Background : Theme.Muted;

            using (var brush = new SolidBrush(fill)) g.FillEllipse(brush, face);
            using (var pen = new Pen(outline, 1.5f * scale)) g.DrawEllipse(pen, face);
            using (var pen = new Pen(glyph, 3 * scale) { StartCap = LineCap.Round, EndCap = LineCap.Round }) {
                float radius = 13 * scale;
                g.DrawArc(pen, x - radius, y - radius, radius * 2, radius * 2, -48, 276);
                g.DrawLine(pen, x, y - 16 * scale, x, y - 2 * scale);
            }

            if (Focused && ShowFocusCues) {
                face.Inflate(4 * scale, 4 * scale);
                using (var pen = new Pen(Theme.Accent, 2 * scale)) g.DrawEllipse(pen, face);
            }
        }
    }

    internal sealed class ModernSwitch : CheckBox {
        public ModernSwitch() {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 10, FontStyle.Bold);
            AccessibleRole = AccessibleRole.CheckButton;
        }
        protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }
        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics;
            g.Clear(BackColor);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = g.DpiX / 96f;
            float width = 34 * scale, height = 18 * scale;
            var track = new RectangleF(3 * scale, (Height - height) / 2, width, height);
            Color fill = Checked && Enabled ? Theme.Accent : Theme.Hover;
            if (Enabled && Capture) fill = Checked ? Theme.AccentPressed : Theme.Selection;
            using (var path = new GraphicsPath()) {
                path.AddArc(track.Left, track.Top, height, height, 90, 180);
                path.AddArc(track.Right - height, track.Top, height, height, 270, 180);
                path.CloseFigure();
                using (var brush = new SolidBrush(fill)) g.FillPath(brush, path);
                using (var pen = new Pen(Enabled ? Theme.ControlBorder : Theme.Border)) g.DrawPath(pen, path);
            }
            float inset = 4 * scale, dot = height - 2 * inset;
            using (var brush = new SolidBrush(Checked && Enabled ? Theme.OnAccent : Enabled ? Theme.Text : Theme.Disabled))
                g.FillEllipse(brush, Checked ? track.Right - dot - inset : track.Left + inset, track.Top + inset, dot, dot);
            int left = (int)(44 * scale);
            var textArea = new Rectangle(left, 0, Math.Max(0, Width - left), Height);
            var flags = TextFormatFlags.VerticalCenter;
            if (!ShowKeyboardCues) flags |= TextFormatFlags.HidePrefix;
            TextRenderer.DrawText(g, Text, Font, textArea, Enabled ? Theme.Text : Theme.Disabled, flags);
            if (Focused)
                using (var pen = new Pen(Theme.Accent, 2)) g.DrawRectangle(pen, Rectangle.Inflate(ClientRectangle, -2, -2));
        }
    }

    internal static class NativeTheme {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        internal static extern int SetWindowTheme(IntPtr handle, string app, string id);
    }
    internal sealed class DarkScrollPanel : Panel {
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            NativeTheme.SetWindowTheme(Handle, "DarkMode_Explorer", null);
        }
    }
    internal sealed class DarkTextBox : TextBox {
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            NativeTheme.SetWindowTheme(Handle, "DarkMode_Explorer", null);
        }
    }
    internal sealed class DarkComboBox : ComboBox {
        public DarkComboBox() {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
        }

        protected override CreateParams CreateParams {
            get {
                var parameters = base.CreateParams;
                const int WindowBorder = 0x00800000;
                const int ClientEdge = 0x00000200;
                parameters.Style &= ~WindowBorder;
                parameters.ExStyle &= ~ClientEdge;
                return parameters;
            }
        }

        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            NativeTheme.SetWindowTheme(Handle, "DarkMode_Explorer", null);
        }

        protected override void OnPaint(PaintEventArgs e) {
            var bounds = ClientRectangle;
            var graphics = e.Graphics;
            graphics.Clear(BackColor);
            if (bounds.Width < 2 || bounds.Height < 2) return;

            float scale = graphics.DpiX / 96f;
            int border = Math.Max(1, (int)Math.Round((Focused ? 2 : 1) * scale));
            Color borderColor = !Enabled ? Theme.Border : Focused ? Theme.Accent : Theme.ControlBorder;
            using (var brush = new SolidBrush(borderColor)) {
                graphics.FillRectangle(brush, 0, 0, bounds.Width, border);
                graphics.FillRectangle(brush, 0, bounds.Bottom - border, bounds.Width, border);
                graphics.FillRectangle(brush, 0, border, border, bounds.Height - 2 * border);
                graphics.FillRectangle(brush, bounds.Right - border, border, border, bounds.Height - 2 * border);
            }

            int textLeft = (int)(8 * scale);
            int arrowWidth = (int)(30 * scale);
            var textBounds = new Rectangle(textLeft, border, Math.Max(0, bounds.Width - textLeft - arrowWidth),
                bounds.Height - 2 * border);
            TextRenderer.DrawText(graphics, GetItemText(SelectedItem), Font, textBounds,
                Enabled ? ForeColor : Theme.Disabled, TextFormatFlags.VerticalCenter |
                    TextFormatFlags.SingleLine | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float x = bounds.Right - arrowWidth / 2f, y = bounds.Height / 2f;
            using (var pen = new Pen(Enabled ? Theme.Muted : Theme.Disabled, 1.5f * scale))
                graphics.DrawLines(pen, new[] { new PointF(x - 4 * scale, y - 2 * scale),
                    new PointF(x, y + 2 * scale), new PointF(x + 4 * scale, y - 2 * scale) });
        }

        protected override void OnGotFocus(EventArgs e) { base.OnGotFocus(e); Invalidate(); }
        protected override void OnLostFocus(EventArgs e) { base.OnLostFocus(e); Invalidate(); }
        protected override void OnSelectedIndexChanged(EventArgs e) { base.OnSelectedIndexChanged(e); Invalidate(); }
    }
}
