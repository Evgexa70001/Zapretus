using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Zapretus {
    internal static class Theme {
        public static readonly Color Background = Color.FromArgb(13, 17, 24);
        public static readonly Color Surface = Color.FromArgb(22, 28, 38);
        public static readonly Color Border = Color.FromArgb(40, 49, 64);
        public static readonly Color Text = Color.FromArgb(236, 241, 249);
        public static readonly Color Muted = Color.FromArgb(148, 163, 184);
        public static readonly Color Accent = Color.FromArgb(113, 225, 193);
        public static GraphicsPath Round(RectangleF r, float radius) {
            float d = radius * 2;
            var p = new GraphicsPath();
            p.AddArc(r.X, r.Y, d, d, 180, 90); p.AddArc(r.Right-d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right-d, r.Bottom-d, d, d, 0, 90); p.AddArc(r.X, r.Bottom-d, d, d, 90, 90);
            p.CloseFigure(); return p;
        }
    }
    internal sealed class Card : Panel {
        public Card() { DoubleBuffered = true; BackColor = Theme.Background; Padding = new Padding(24); }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var p = Theme.Round(new RectangleF(0.5f, 0.5f, Width-1, Height-1), 18))
            using (var b = new SolidBrush(Theme.Surface))
            using (var pen = new Pen(Theme.Border)) { e.Graphics.FillPath(b, p); e.Graphics.DrawPath(pen, p); }
            base.OnPaint(e);
        }
    }
    internal sealed class ModernButton : Button {
        public bool Primary;
        bool hover;
        public ModernButton() {
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
            Cursor = Cursors.Hand; Font = new Font("Segoe UI", 10, FontStyle.Bold);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(Theme.Surface); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Color fill = Primary ? Theme.Accent : Color.FromArgb(35, 44, 59);
            if (hover && Enabled) fill = ControlPaint.Light(fill, .12f);
            if (!Enabled) fill = Color.FromArgb(38, 47, 59);
            using (var p = Theme.Round(new RectangleF(0, 0, Width-1, Height-1), 11))
            using (var b = new SolidBrush(fill)) e.Graphics.FillPath(b, p);
            TextRenderer.DrawText(e.Graphics, Text, Font, ClientRectangle,
                !Enabled ? Theme.Muted : Primary ? Theme.Background : Theme.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -5, -5));
        }
    }
    internal sealed class PowerButton : Button {
        public bool Active;
        bool hover;
        public PowerButton() {
            DoubleBuffered = true; BackColor = Theme.Surface; Cursor = Cursors.Hand;
            FlatStyle = FlatStyle.Flat; FlatAppearance.BorderSize = 0;
            AccessibleName = "Включить подключение";
        }
        protected override void OnMouseEnter(EventArgs e) { hover = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { hover = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnPaint(PaintEventArgs e) {
            var g = e.Graphics; g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Theme.Surface);
            Color color = !Enabled ? Theme.Muted : Theme.Accent;
            Color fill = Active ? Color.FromArgb(29, 74, 65) : Color.FromArgb(29, 47, 49);
            if (hover && Enabled) fill = ControlPaint.Light(fill, .14f);
            using (var b = new SolidBrush(fill))
                g.FillEllipse(b, 4, 4, Width-8, Height-8);
            using (var pen = new Pen(Active ? Theme.Accent : Theme.Border, 1.5f))
                g.DrawEllipse(pen, 4, 4, Width-8, Height-8);
            using (var pen = new Pen(color, 3) { StartCap = LineCap.Round, EndCap = LineCap.Round }) {
                g.DrawArc(pen, Width/2-18, Height/2-18, 36, 36, -48, 276);
                g.DrawLine(pen, Width/2, Height/2-23, Width/2, Height/2-2);
            }
            if (Focused && ShowFocusCues) using (var pen = new Pen(Theme.Accent) { DashStyle = DashStyle.Dot })
                g.DrawEllipse(pen, 9, 9, Width-18, Height-18);
        }
    }
    internal sealed class ModernSwitch : CheckBox {
        public ModernSwitch() {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            Cursor = Cursors.Hand; BackColor = Theme.Surface;
            AccessibleRole = AccessibleRole.CheckButton;
        }
        protected override void OnCheckedChanged(EventArgs e) { base.OnCheckedChanged(e); Invalidate(); }
        protected override void OnPaint(PaintEventArgs e) {
            e.Graphics.Clear(BackColor); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float scale = Height / 32f;
            var track = new RectangleF(1, 3 * scale, 46 * scale, 26 * scale);
            Color fill = Checked ? Theme.Accent : Color.FromArgb(64, 77, 96);
            if (!Enabled) fill = Checked ? Color.FromArgb(57, 98, 88) : Color.FromArgb(45, 54, 67);
            using (var path = Theme.Round(track, track.Height/2))
            using (var brush = new SolidBrush(fill)) e.Graphics.FillPath(brush, path);
            float size = track.Height-6*scale;
            using (var brush = new SolidBrush(Checked ? Theme.Background : Theme.Text))
                e.Graphics.FillEllipse(brush, Checked ? track.Right-size-3*scale : track.Left+3*scale, track.Top+3*scale, size, size);
            var textArea = new Rectangle((int)(62*scale), 0, Width-(int)(62*scale), Height);
            TextRenderer.DrawText(e.Graphics, Text, Font, textArea, Enabled ? Theme.Text : Theme.Muted, TextFormatFlags.VerticalCenter);
            if (Focused && ShowFocusCues) ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(ClientRectangle, -1, -1));
        }
    }
    internal static class NativeTheme {
        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        internal static extern int SetWindowTheme(IntPtr handle, string app, string id);
    }
    internal sealed class DarkTextBox : TextBox {
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); NativeTheme.SetWindowTheme(Handle, "DarkMode_Explorer", null); }
    }
    internal sealed class DarkComboBox : ComboBox {
        protected override void OnHandleCreated(EventArgs e) { base.OnHandleCreated(e); NativeTheme.SetWindowTheme(Handle, "DarkMode_Explorer", null); }
        protected override void WndProc(ref Message m) {
            base.WndProc(ref m);
            if (m.Msg != 0x000F || DropDownStyle != ComboBoxStyle.DropDownList) return;
            using (var g = Graphics.FromHwnd(Handle)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                int arrowWidth = (int)(28 * g.DpiX/96f);
                using (var brush = new SolidBrush(BackColor)) g.FillRectangle(brush, Width-arrowWidth, 0, arrowWidth, Height);
                using (var pen = new Pen(Theme.Border)) g.DrawRectangle(pen, 0, 0, Width-1, Height-1);
                float x = Width-arrowWidth/2f, y = Height/2f;
                using (var pen = new Pen(Enabled ? Theme.Muted : Theme.Border, 1.5f))
                    g.DrawLines(pen, new[] { new PointF(x-4, y-2), new PointF(x, y+2), new PointF(x+4, y-2) });
            }
        }
    }
}
