using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace Zapretus {
    internal static class Probe {
        internal static bool ValidVideoUrl(string text) {
            Uri uri;
            return Uri.TryCreate(text, UriKind.Absolute, out uri) && uri.Scheme == "https" && uri.IsDefaultPort
                && uri.UserInfo.Length == 0 && uri.Host.EndsWith(".googlevideo.com", StringComparison.OrdinalIgnoreCase)
                && uri.AbsolutePath == "/videoplayback";
        }
        public static async Task<ProbeResult> Run(Action<string> log, string videoUrl, CancellationToken token) {
            if (!String.IsNullOrWhiteSpace(videoUrl) && !ValidVideoUrl(videoUrl))
                throw new ArgumentException("Нужна HTTPS-ссылка googlevideo.com/videoplayback.");
            var jobs = new List<Task<ProbeItem>> {
                Http("YouTube: страница", "https://www.youtube.com/", "html", 1, token),
                Http("YouTube: изображение", "https://i.ytimg.com/vi/jNQXAC9IVRw/default.jpg", "image", 1, token),
                Http("Discord: API", "https://discord.com/api/v10/gateway", "json", 2, token),
                Http("Discord: CDN", "https://cdn.discordapp.com/embed/avatars/0.png", "image", 1, token),
                Gateway(token)
            };
            if (!String.IsNullOrWhiteSpace(videoUrl)) jobs.Add(Http("YouTube: медиаданные", videoUrl, "video", 5, token));
            else jobs.Add(Task.FromResult(new ProbeItem { Name = "YouTube: медиаданные", Skipped = true, Detail = "Не задана прямая ссылка на видео" }));
            var result = new ProbeResult { Items = await Task.WhenAll(jobs) };
            foreach (var item in result.Items) log(item.Name + ": " + (item.Skipped ? "ПРОПУСК" : item.Passed ? "OK" : "СБОЙ") + " — " + item.Detail);
            return result;
        }
        static async Task<ProbeItem> Http(string name, string url, string kind, int weight, CancellationToken token) {
            var item = new ProbeItem { Name = name, Weight = weight }; var watch = Stopwatch.StartNew();
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(token)) {
                timeout.CancelAfter(TimeSpan.FromSeconds(12));
                try {
                    using (var handler = new HttpClientHandler { UseProxy = false, AllowAutoRedirect = kind != "video" })
                    using (var client = new HttpClient(handler))
                    using (timeout.Token.Register(client.Dispose))
                    using (var request = new HttpRequestMessage(HttpMethod.Get, url)) {
                        request.Headers.UserAgent.ParseAdd("Mozilla/5.0 Zapretus/0.2");
                        if (kind == "video") request.Headers.Range = new RangeHeaderValue(0, 262143);
                        using (var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token)) {
                            item.Detail = "HTTP " + (int)response.StatusCode;
                            if (!response.IsSuccessStatusCode) return item;
                            int limit = kind == "video" ? 262144 : 32768;
                            using (var stream = await response.Content.ReadAsStreamAsync())
                            using (var body = new MemoryStream()) {
                                byte[] buffer = new byte[8192];
                                while (body.Length < limit) {
                                    int n = await stream.ReadAsync(buffer, 0, Math.Min(buffer.Length, limit - (int)body.Length), timeout.Token);
                                    if (n == 0) break; body.Write(buffer, 0, n);
                                }
                                byte[] bytes = body.ToArray(); item.Bytes = bytes.Length;
                                string media = response.Content.Headers.ContentType == null ? "" : response.Content.Headers.ContentType.MediaType;
                                item.Passed = ValidateBody(kind, media, bytes);
                                item.Detail += ", " + bytes.Length + " байт" + (item.Passed ? "" : ", содержимое не прошло проверку");
                            }
                        }
                    }
                } catch (OperationCanceledException) { token.ThrowIfCancellationRequested(); item.Detail = "Таймаут 12 с"; }
                catch (Exception e) {
                    token.ThrowIfCancellationRequested();
                    if (timeout.IsCancellationRequested) { item.Detail = "Таймаут 12 с"; return item; }
                    if (!(e is HttpRequestException) && !(e is IOException)) throw;
                    // Exception messages can contain a signed URL. Do not include them.
                    item.Detail = "Ошибка соединения: " + e.GetType().Name;
                } finally { item.Milliseconds = watch.ElapsedMilliseconds; }
            }
            return item;
        }
        internal static bool ValidateBody(string kind, string media, byte[] bytes) {
            if (kind == "video") return bytes.Length >= 65536 && (media.StartsWith("video/") || media.StartsWith("audio/"));
            if (kind == "image") return media.StartsWith("image/") && bytes.Length > 100 &&
                ((bytes[0] == 0xff && bytes[1] == 0xd8) || (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4e && bytes[3] == 0x47));
            if (kind == "html") return media.Contains("html") && Encoding.UTF8.GetString(bytes).IndexOf("youtube", StringComparison.OrdinalIgnoreCase) >= 0;
            try {
                var obj = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(bytes));
                object value;
                return obj != null && obj.TryGetValue("url", out value) && (value as string) == "wss://gateway.discord.gg";
            } catch (ArgumentException) { return false; } catch (InvalidOperationException) { return false; }
        }
        static async Task<ProbeItem> Gateway(CancellationToken token) {
            var item = new ProbeItem { Name = "Discord: WebSocket", Weight = 3 }; var watch = Stopwatch.StartNew();
            using (var timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
            using (var ws = new ClientWebSocket()) {
                timeout.CancelAfter(TimeSpan.FromSeconds(12)); ws.Options.Proxy = null;
                try {
                    await ws.ConnectAsync(new Uri("wss://gateway.discord.gg/?v=10&encoding=json"), timeout.Token);
                    byte[] buffer = new byte[4096];
                    using (var body = new MemoryStream()) {
                        WebSocketReceiveResult message;
                        do {
                            message = await ws.ReceiveAsync(new ArraySegment<byte>(buffer), timeout.Token);
                            if (message.MessageType != WebSocketMessageType.Text || body.Length + message.Count > 32768)
                                throw new InvalidDataException("Unexpected gateway message");
                            body.Write(buffer, 0, message.Count);
                        } while (!message.EndOfMessage);
                        var obj = new JavaScriptSerializer().Deserialize<Dictionary<string, object>>(Encoding.UTF8.GetString(body.ToArray()));
                        object op; item.Passed = obj != null && obj.TryGetValue("op", out op) && Convert.ToInt32(op) == 10;
                        item.Bytes = body.Length; item.Detail = item.Passed ? "Получен Discord Hello (op=10)" : "Не получен Hello";
                    }
                } catch (OperationCanceledException) { token.ThrowIfCancellationRequested(); item.Detail = "Таймаут 12 с"; }
                catch (Exception e) {
                    if (!(e is WebSocketException) && !(e is IOException) && !(e is ArgumentException) && !(e is InvalidOperationException) && !(e is FormatException)) throw;
                    item.Detail = "Ошибка WebSocket: " + e.GetType().Name;
                }
                finally { ws.Abort(); item.Milliseconds = watch.ElapsedMilliseconds; }
            }
            return item;
        }
    }
}
