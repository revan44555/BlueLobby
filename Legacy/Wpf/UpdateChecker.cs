using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace BlueLobby
{
    /// <summary>
    /// Basit güncelleme denetleyici: yalnızca yeni sürüm bulunursa haber verir.
    /// ReleasesUrl boş bırakılırsa sessizce hiçbir şey yapmaz.
    /// </summary>
    public static class UpdateChecker
    {
        // Örnek: "https://api.github.com/repos/KULLANICI/bluelobby/releases/latest"
        // Şimdilik kapalı; gerçek repo adresi yazılınca otomatik aktif olur.
        private const string ReleasesUrl = "";
        private const string DownloadPageUrl = "";

        public static async Task CheckAsync(Window owner)
        {
            if (string.IsNullOrWhiteSpace(ReleasesUrl))
            {
                return;
            }

            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("BlueLobby/3.0");

                string json = await http.GetStringAsync(ReleasesUrl);
                using var doc = JsonDocument.Parse(json);

                string? tag = doc.RootElement.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() : null;
                string? url = doc.RootElement.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() : null;

                if (string.IsNullOrWhiteSpace(tag))
                {
                    return;
                }

                string current = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
                string cleanTag = tag.TrimStart('v', 'V');

                if (IsNewer(cleanTag, current))
                {
                    var result = MessageBox.Show(
                        owner,
                        $"Yeni sürüm mevcut: {tag}\n(Sizdeki: v{current})\n\nİndirme sayfası açılsın mı?",
                        "Güncelleme Mevcut",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (result == MessageBoxResult.Yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = string.IsNullOrWhiteSpace(url) ? DownloadPageUrl : url,
                            UseShellExecute = true
                        });
                    }
                }

                // Yeni sürüm yoksa: sessiz. Kullanıcı hiçbir şey görmüyor.
            }
            catch
            {
                // Ağ yoksa, GitHub kapalıysa vb.: sessizce geç.
            }
        }

        private static bool IsNewer(string remote, string local)
        {
            if (Version.TryParse(remote, out Version? rv) && Version.TryParse(local, out Version? lv))
            {
                return rv > lv;
            }

            return false;
        }
    }
}
