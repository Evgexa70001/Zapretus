using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Zapretus {
    internal static class HostsList {
        const string Source = "https://raw.githubusercontent.com/Flowseal/zapret-discord-youtube/refs/heads/main/.service/hosts";
        static readonly HttpClient Client = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) {
            Timeout = TimeSpan.FromSeconds(8), MaxResponseContentBufferSize = 128 * 1024
        };

        internal static async Task<string> DownloadAsync(CancellationToken token) {
            using (var request = new HttpRequestMessage(HttpMethod.Get, Source)) {
                request.Headers.CacheControl = new CacheControlHeaderValue { NoCache = true };
                using (var response = await Client.SendAsync(request, token).ConfigureAwait(false)) {
                    response.EnsureSuccessStatusCode();
                    return Format(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                }
            }
        }

        static string Format(string text) {
            var block = new StringBuilder();
            bool hasEntries = false;
            using (var reader = new StringReader(text.TrimStart('\uFEFF'))) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    var entry = line.Split(new[] { '#' }, 2)[0].Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (entry.Length != 0) {
                        IPAddress address;
                        if (entry.Length < 2 || !IPAddress.TryParse(entry[0], out address) ||
                            entry.Skip(1).Any(host => Uri.CheckHostName(host) != UriHostNameType.Dns)) {
                            throw new InvalidDataException("Файл Flowseal содержит строку в неожиданном формате.");
                        }
                        hasEntries = true;
                    }
                    block.AppendLine(line);
                }
            }
            if (!hasEntries) throw new InvalidDataException("В файле Flowseal нет записей hosts.");
            return "# Zapretus hosts (Flowseal)\r\n" + block + "# /Zapretus hosts (Flowseal)\r\n";
        }
    }
}
