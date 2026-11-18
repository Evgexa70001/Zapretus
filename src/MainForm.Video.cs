using System;
using System.Drawing;
using System.Windows.Forms;

namespace Zapretus {
    internal sealed partial class MainForm {
        void EditVideoUrl() {
            if (busy) { Log("Дождитесь завершения проверки перед изменением ссылки."); return; }
            using (var dialog = new Form { Text = "Проверка медиаданных YouTube", ClientSize = new Size(660, 240),
                FormBorderStyle = FormBorderStyle.FixedDialog, MaximizeBox = false, MinimizeBox = false,
                StartPosition = FormStartPosition.CenterParent, BackColor = Theme.Background, ForeColor = Theme.Text, Font = Font }) {
                var description = new Label { Text = "Вставьте свежую прямую ссылку HTTPS на googlevideo.com/videoplayback.\nЕё можно скопировать из Network в инструментах разработчика браузера.\nСсылка действует временно и хранится только до закрытия приложения.",
                    Location = new Point(20, 18), Size = new Size(620, 78) };
                var input = new TextBox { Text = videoUrl, Location = new Point(20, 105), Size = new Size(620, 30), BackColor = Theme.Surface, ForeColor = Theme.Text };
                var save = new ModernButton { Text = "Применить", Primary = true, Location = new Point(20, 167), Size = new Size(180, 44) };
                var clear = new ModernButton { Text = "Без медиатеста", Location = new Point(218, 167), Size = new Size(190, 44) };
                save.Click += (s, e) => {
                    string value = input.Text.Trim();
                    if (value.Length > 0 && !Probe.ValidVideoUrl(value)) { MessageBox.Show(dialog, "Нужна HTTPS-ссылка вида https://…googlevideo.com/videoplayback?…"); return; }
                    videoUrl = value; dialog.Close();
                    Log(value.Length > 0 ? "Медиатест включён: до 256 КиБ за проверку. Ссылка не записывается в отчёт." : "Медиатест отключён.");
                };
                clear.Click += (s, e) => { videoUrl = ""; dialog.Close(); Log("Медиатест отключён."); };
                dialog.Controls.Add(description); dialog.Controls.Add(input); dialog.Controls.Add(save); dialog.Controls.Add(clear);
                dialog.AcceptButton = save; dialog.ShowDialog(this);
            }
        }
    }
}
