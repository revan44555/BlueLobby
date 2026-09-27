using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BlueLobby.Core
{
    /// <summary>Bir oyun klasörüne özgü kullanıcı ayarları (madde 1: oyun başına profil).</summary>
    public sealed class GameProfile
    {
        public string GameDir { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public string AppId { get; set; } = string.Empty;
        public string Language { get; set; } = "english";
        public bool BroadcastOn { get; set; } = true;
        public bool DlcTemplateOn { get; set; }
        public bool FirewallOn { get; set; } = true;
        public string SavedUtc { get; set; } = string.Empty;
    }

    /// <summary>Oyun profillerini LocalDataDir/profiles altında, klasör-hash başına JSON olarak saklar.</summary>
    public sealed class ProfileStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private readonly string _dir;

        public ProfileStore(string localDataDir)
        {
            _dir = Path.Combine(localDataDir, "profiles");
            Directory.CreateDirectory(_dir);
        }

        private string PathFor(string gameDir) =>
            Path.Combine(_dir, GameScanner.ComputeKey(gameDir) + ".json");

        public GameProfile? Load(string gameDir)
        {
            string path = PathFor(gameDir);
            if (!File.Exists(path)) return null;
            try
            {
                return JsonSerializer.Deserialize<GameProfile>(File.ReadAllText(path));
            }
            catch
            {
                return null;
            }
        }

        public void Save(GameProfile profile)
        {
            profile.SavedUtc = DateTime.UtcNow.ToString("O");
            string path = PathFor(profile.GameDir);
            File.WriteAllText(path + ".tmp", JsonSerializer.Serialize(profile, JsonOptions));
            File.Copy(path + ".tmp", path, overwrite: true);
            File.Delete(path + ".tmp");
        }

        public void Delete(string gameDir)
        {
            string path = PathFor(gameDir);
            if (File.Exists(path)) File.Delete(path);
        }

        public IReadOnlyList<GameProfile> ListAll()
        {
            var list = new List<GameProfile>();
            foreach (string file in Directory.EnumerateFiles(_dir, "*.json"))
            {
                try
                {
                    GameProfile? p = JsonSerializer.Deserialize<GameProfile>(File.ReadAllText(file));
                    if (p != null) list.Add(p);
                }
                catch
                {
                    // Bozuk dosya atlanır.
                }
            }
            return list;
        }
    }
}
