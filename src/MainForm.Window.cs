using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Zapretus {
    internal sealed partial class MainForm {
        protected override void OnHandleCreated(EventArgs e) {
            base.OnHandleCreated(e);
            ApplyWindowColors(Handle);
        }
        static void ApplyWindowColors(IntPtr handle) {
            // Keep native resize, snap, system menu and caption buttons.
            // https://learn.microsoft.com/windows/win32/api/dwmapi/ne-dwmapi-dwmwindowattribute
            int dark = 1;
            DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
            int background = ColorRef(Theme.Background), text = ColorRef(Theme.Text), border = ColorRef(Theme.Border);
            // Caption/text/border colors are supported on Windows 11; older systems ignore these attributes.
            DwmSetWindowAttribute(handle, 35, ref background, sizeof(int));
            DwmSetWindowAttribute(handle, 36, ref text, sizeof(int));
            DwmSetWindowAttribute(handle, 34, ref border, sizeof(int));
        }
        static int ColorRef(Color value) { return value.R | (value.G << 8) | (value.B << 16); }
        [DllImport("dwmapi.dll")]
        static extern int DwmSetWindowAttribute(IntPtr window, int attribute, ref int value, int size);
    }
}
