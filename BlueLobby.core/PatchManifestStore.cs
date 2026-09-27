using System;
using System.IO;
using System.Text.Json;
using BlueLobby.Platform;

namespace BlueLobby.Core
{
    /// <summary>Manifest/ayar/log yollarını platform bazlı çözer ve manifest okuma/yazma yapar.</summary>
    public sealed class PatchManifestStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public string SettingsPath { get; }
        public string ManifestDir { get; }
        public string LogPath { get; }

        public PatchManifestStore(string settingsPath, string manifestDir, string logPath)
        {
            SettingsPath = settingsPath;
            ManifestDir = manifestDir;
            LogPath = logPath;
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            Directory.CreateDirectory(manifestDir);
        }

        public static PatchManifestStore CreateDefault(IPlatformPaths paths)
        {
            return new PatchManifestStore(
                Path.Combine(paths.RoamingDataDir, "settings.json"),
                Path.Combine(paths.LocalDataDir, "manifests"),
                Path.Combine(paths.LocalDataDir, "app.log"));
        }

        public string GetManifestPath(string gameDir) => Path.Combine(ManifestDir, GameScanner.ComputeKey(gameDir) + ".json");

        public PatchManifest? LoadManifest(string gameDir)
        {
            string path = GetManifestPath(gameDir);
            if (!File.Exists(path))
            {
                return null;
            }

            try
            {
                return JsonSerializer.Deserialize<PatchManifest>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        public void SaveManifest(PatchManifest manifest)
        {
            string path = GetManifestPath(manifest.GameDir);
            string temp = path + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(manifest, JsonOptions));
            File.Copy(temp, path, overwrite: true);
            File.Delete(temp);
        }

        public void DeleteManifest(string gameDir)
        {
            string path = GetManifestPath(gameDir);
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public AppSettings LoadSettings()
        {
            if (!File.Exists(SettingsPath))
            {
                return new AppSettings();
            }

            try
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath)) ?? new AppSettings();
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            string temp = SettingsPath + ".tmp";
            File.WriteAllText(temp, JsonSerializer.Serialize(settings, JsonOptions));
            File.Copy(temp, SettingsPath, overwrite: true);
            File.Delete(temp);
        }

        public void AppendLog(string line)
        {
            try
            {
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {line}{Environment.NewLine}");
            }
            catch
            {
                // Log yazılamazsa uygulama düşmemeli.
            }
        }
    }
}
