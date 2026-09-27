using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BlueLobby.Core
{
    public sealed class HeroicGame
    {
        public string AppName { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string InstallPath { get; set; } = string.Empty;
    }

    /// <summary>Madde D (Linux): Heroic ve Proton entegrasyonu için tarayıcılar.</summary>
    public static class GameLibrary
    {
        /// <summary>Heroic Games Launcher'ın legendary installed.json dosyasından kurulu oyunları okur.</summary>
        public static List<HeroicGame> FindHeroicGames(string homeDir)
        {
            var games = new List<HeroicGame>();
            if (string.IsNullOrWhiteSpace(homeDir)) return games;

            string[] candidates =
            {
                Path.Combine(homeDir, ".config", "heroic", "legendaryConfig", "legendary", "installed.json"),
                Path.Combine(homeDir, ".var", "app", "com.heroicgameslauncher.hgl", "config", "heroic", "legendaryConfig", "legendary", "installed.json"),
            };

            foreach (string path in candidates)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(path));
                    foreach (JsonProperty prop in doc.RootElement.EnumerateObject())
                    {
                        JsonElement el = prop.Value;
                        string? title = el.TryGetProperty("title", out var t) ? t.GetString() : prop.Name;
                        string? installPath = el.TryGetProperty("install_path", out var p) ? p.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(installPath) && Directory.Exists(installPath))
                        {
                            games.Add(new HeroicGame
                            {
                                AppName = prop.Name,
                                Title = title ?? prop.Name,
                                InstallPath = installPath,
                            });
                        }
                    }
                }
                catch
                {
                    // Bozuk dosya: atlanır.
                }
                if (games.Count > 0) break;
            }
            return games;
        }

        /// <summary>Kurulu Proton/GE-Proton sürümlerini listeler (Steam klasöründen).</summary>
        public static List<string> FindProtonVersions(string homeDir)
        {
            var versions = new List<string>();
            if (string.IsNullOrWhiteSpace(homeDir)) return versions;

            string[] steamRoots =
            {
                Path.Combine(homeDir, ".steam", "steam"),
                Path.Combine(homeDir, ".local", "share", "Steam"),
            };

            foreach (string root in steamRoots)
            {
                string custom = Path.Combine(root, "compatibilitytools.d");
                if (Directory.Exists(custom))
                {
                    foreach (string dir in Directory.EnumerateDirectories(custom))
                    {
                        if (File.Exists(Path.Combine(dir, "compatibilitytool.vdf")))
                        {
                            versions.Add(Path.GetFileName(dir));
                        }
                    }
                }

                string common = Path.Combine(root, "steamapps", "common");
                if (Directory.Exists(common))
                {
                    foreach (string dir in Directory.EnumerateDirectories(common))
                    {
                        string name = Path.GetFileName(dir);
                        if (name.StartsWith("Proton", StringComparison.OrdinalIgnoreCase))
                        {
                            versions.Add(name);
                        }
                    }
                }
            }
            versions.Sort(StringComparer.OrdinalIgnoreCase);
            return versions;
        }
    }
}
