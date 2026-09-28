using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace BlueLobby.Core
{
    /// <summary>Bir oyun klasörüne özgü kullanıcı ayarları.</summary>
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
        private const long MaxProfileBytes = 512 * 1024;
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
                if (new FileInfo(path).Length > MaxProfileBytes)
                    throw new InvalidDataException("Profil dosyası beklenenden büyük.");

                GameProfile? profile = JsonSerializer.Deserialize<GameProfile>(File.ReadAllText(path));
                if (profile == null || string.IsNullOrWhiteSpace(profile.GameDir)) return null;
                if (!string.Equals(
                    PathSafety.Canonicalize(profile.GameDir),
                    PathSafety.Canonicalize(gameDir),
                    OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    return null;
                return profile;
            }
            catch
            {
                TryQuarantine(path);
                return null;
            }
        }

        public void Save(GameProfile profile)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            if (string.IsNullOrWhiteSpace(profile.GameDir)) throw new InvalidDataException("Profil için oyun klasörü gerekli.");
            profile.GameDir = PathSafety.Canonicalize(profile.GameDir);
            profile.SavedUtc = DateTime.UtcNow.ToString("O");
            string path = PathFor(profile.GameDir);
            FileIntegrity.WriteTextDurably(path, JsonSerializer.Serialize(profile, JsonOptions));
        }

        public void Delete(string gameDir)
        {
            string path = PathFor(gameDir);
            FileIntegrity.TryDelete(path);
        }

        public IReadOnlyList<GameProfile> ListAll()
        {
            var list = new List<GameProfile>();
            foreach (string file in Directory.EnumerateFiles(_dir, "*.json"))
            {
                try
                {
                    if (new FileInfo(file).Length > MaxProfileBytes) continue;
                    GameProfile? p = JsonSerializer.Deserialize<GameProfile>(File.ReadAllText(file));
                    if (p != null && !string.IsNullOrWhiteSpace(p.GameDir)) list.Add(p);
                }
                catch
                {
                    // Tek bir bozuk profil diğerlerini etkilemez.
                }
            }
            return list;
        }

        private static void TryQuarantine(string path)
        {
            try
            {
                if (!File.Exists(path)) return;
                string quarantine = path + $".corrupt-{DateTime.UtcNow:yyyyMMddHHmmssfff}";
                File.Move(path, quarantine, overwrite: false);
            }
            catch
            {
                // Profil okunamaması uygulamanın açılmasını engellememeli.
            }
        }
    }
}
