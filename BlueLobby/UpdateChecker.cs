using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;

namespace BlueLobby
{
    /// <summary>Basit güncelleme denetleyici. ReleasesUrl boşsa sessizce hiçbir şey yapmaz.</summary>
    public static class UpdateChecker
    {
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
                http.DefaultRequestHeaders.UserAgent.ParseAdd("BlueLobby/3.1");
                string json = await http.GetStringAsync(ReleasesUrl);
                using var doc = JsonDocument.Parse(json);

                string? tag = doc.RootElement.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() : null;
                string? url = doc.RootElement.TryGetProperty("html_url", out var urlEl) ? urlEl.GetString() : null;
                if (string.IsNullOrWhiteSpace(tag)) return;

                string current = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.0.0";
                string cleanTag = tag.TrimStart('v', 'V');
                if (IsNewer(cleanTag, current))
                {
                    var dialog = new ConfirmDialog("Güncelleme",
                        new[] { $"Yeni sürüm mevcut: {tag} (sizdeki: v{current})", "İndirme sayfası açılsın mı?" },
                        Loc.T("yes"), Loc.T("no"));
                    bool yes = await dialog.ShowDialog<bool>(owner);
                    if (yes)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = string.IsNullOrWhiteSpace(url) ? DownloadPageUrl : url,
                            UseShellExecute = true
                        });
                    }
                }
            }
            catch
            {
                // Güncelleme denetimi isteğe bağlıdır.
            }
        }

        private static bool IsNewer(string tag, string current)
        {
            if (!Version.TryParse(tag, out Version? newer) || !Version.TryParse(current, out Version? existing))
            {
                return false;
            }
            return newer > existing;
        }
    }
}
