using System.Drawing;
using System.Windows.Forms;

namespace Zapretus {
    internal sealed partial class MainForm {
        void EditVideoUrl() {
            if (busy) { Log("Дождитесь завершения проверки перед изменением ссылки."); return; }
            using (var dialog = SettingsDialog("Медиатест YouTube", new Size(584, 262))) {
                var content = DialogContent(dialog, 55, 26, 30, 0, 44);
                var input = new DarkTextBox { Text = videoUrl, Dock = DockStyle.Fill, Margin = Padding.Empty };
                var error = DialogNote("", Theme.Error);
                var save = new ModernButton { Text = "Применить", Primary = true };
                var clear = new ModernButton { Text = "Без медиатеста" };
                save.Click += (s, e) => {
                    string value = input.Text.Trim();
                    if (value.Length > 0 && !Probe.ValidVideoUrl(value)) {
                        error.Text = "Нужна HTTPS-ссылка вида https://…googlevideo.com/videoplayback?…";
                        input.AccessibleDescription = error.Text;
                        input.Focus();
                        return;
                    }
                    videoUrl = value;
                    dialog.Close();
                    Log(value.Length > 0 ? "Медиатест включён: до 256 КиБ за проверку. Ссылка не записывается в отчёт." : "Медиатест отключён.");
                };
                clear.Click += (s, e) => { videoUrl = ""; dialog.Close(); Log("Медиатест отключён."); };
                content.Controls.Add(DialogNote("Возьмите прямую ссылку из вкладки Network в браузере. Она временная и хранится до закрытия Zapretus.", Theme.Muted), 0, 0);
                content.Controls.Add(TextLabel("Ссылка на видео"), 0, 1);
                content.Controls.Add(InputFrame(input, "Ссылка на видео"), 0, 2);
                content.Controls.Add(error, 0, 3);
                content.Controls.Add(DialogActions(dialog, "Отмена", save, clear), 0, 4);
                dialog.AcceptButton = save;
                dialog.ShowDialog(this);
            }
        }
    }
}
