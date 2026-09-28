using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using BlueLobby.Platform;

namespace BlueLobby.Core
{
    public sealed class ManifestReadResult
    {
        public bool Exists { get; init; }
        public bool Valid { get; init; }
        public PatchManifest? Manifest { get; init; }
        public string Error { get; init; } = string.Empty;
    }

    /// <summary>Manifest/ayar/log yollarını platform bazlı çözer ve crash-safe atomic kayıt yapar.</summary>
    public sealed class PatchManifestStore
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
        private const long MaxManifestBytes = 2 * 1024 * 1024;

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

        public IDisposable AcquireGameLock(string gameDir, TimeSpan timeout)
        {
            string path = Path.Combine(ManifestDir, GameScanner.ComputeKey(gameDir) + ".lock");
            DateTime deadline = DateTime.UtcNow + timeout;
            Exception? last = null;
            while (DateTime.UtcNow < deadline)
            {
                try
                {
                    FileStream stream = new(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    return stream;
                }
                catch (IOException ex)
                {
                    last = ex;
                    Thread.Sleep(100);
                }
            }
            throw new IOException("Bu oyun için başka bir BlueLobby işlemi hâlen çalışıyor; işlem kilidi alınamadı.", last);
        }

        public ManifestReadResult ReadManifest(string gameDir)
        {
            string path = GetManifestPath(gameDir);
            if (!File.Exists(path)) return new ManifestReadResult { Exists = false, Valid = true };

            try
            {
                FileInfo info = new(path);
                if (info.Length > MaxManifestBytes)
                    throw new InvalidDataException("Manifest beklenenden büyük; güvenlik nedeniyle okunmadı.");

                PatchManifest? manifest = JsonSerializer.Deserialize<PatchManifest>(File.ReadAllText(path), JsonOptions);
                if (manifest == null)
                    throw new InvalidDataException("Manifest boş veya çözümlenemedi.");

                UpgradeLegacyManifest(manifest);
                ValidateManifest(manifest, gameDir);
                return new ManifestReadResult { Exists = true, Valid = true, Manifest = manifest };
            }
            catch (Exception ex)
            {
                return new ManifestReadResult { Exists = true, Valid = false, Error = ex.Message };
            }
        }

        public PatchManifest? LoadManifest(string gameDir)
        {
            ManifestReadResult result = ReadManifest(gameDir);
            if (!result.Exists) return null;
            if (!result.Valid) throw new InvalidDataException($"Manifest okunamadı: {result.Error}");
            return result.Manifest;
        }

        public void SaveManifest(PatchManifest manifest)
        {
            ValidateManifest(manifest, manifest.GameDir);
            string path = GetManifestPath(manifest.GameDir);
            FileIntegrity.WriteTextDurably(path, JsonSerializer.Serialize(manifest, JsonOptions));
        }

        public void DeleteManifest(string gameDir)
        {
            string path = GetManifestPath(gameDir);
            FileIntegrity.TryDelete(path);
        }

        public AppSettings LoadSettings()
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            try
            {
                FileInfo info = new(SettingsPath);
                if (info.Length > MaxManifestBytes) throw new InvalidDataException("Ayar dosyası çok büyük.");
                AppSettings settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(SettingsPath), JsonOptions) ?? new AppSettings();
                if (settings.SchemaVersion < 2)
                {
                    // v1 settings had no schema marker and are otherwise compatible.
                    // Do not write during load; the next normal settings save persists the migration.
                    settings.SchemaVersion = 2;
                }
                return settings;
            }
            catch
            {
                TryQuarantineCorruptFile(SettingsPath);
                return new AppSettings();
            }
        }

        public void SaveSettings(AppSettings settings)
        {
            FileIntegrity.WriteTextDurably(SettingsPath, JsonSerializer.Serialize(settings, JsonOptions));
        }

        public void AppendLog(string line)
        {
            try
            {
                RotateLogIfNeeded();
                File.AppendAllText(LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {line}{Environment.NewLine}");
            }
            catch
            {
                // Log yazılamazsa uygulama düşmemeli.
            }
        }

        private void RotateLogIfNeeded()
        {
            if (!File.Exists(LogPath)) return;
            if (new FileInfo(LogPath).Length < 5 * 1024 * 1024) return;

            string rotated = LogPath + ".1";
            FileIntegrity.TryDelete(rotated);
            File.Move(LogPath, rotated, overwrite: true);
        }

        private static void UpgradeLegacyManifest(PatchManifest manifest)
        {
            manifest.Entries ??= new List<PatchEntry>();
            manifest.CreatedFiles ??= new List<string>();
            manifest.CreatedFileRecords ??= new List<ManagedFile>();
            manifest.CreatedDirectories ??= new List<string>();
            manifest.TextBackups ??= new List<FileBackup>();

            if (manifest.SchemaVersion >= 2)
            {
                if (string.IsNullOrWhiteSpace(manifest.TransactionId)) manifest.TransactionId = Guid.NewGuid().ToString("N");
                // Schema v2 state alanı gerçektir; Preparing değerini Committed'a dönüştürme.
                return;
            }

            manifest.SchemaVersion = 2;
            if (string.IsNullOrWhiteSpace(manifest.TransactionId)) manifest.TransactionId = Guid.NewGuid().ToString("N");
            bool legacyRecoverySafe = manifest.Entries.Count > 0 && manifest.Entries.All(e => File.Exists(e.BackupPath)) &&
                manifest.TextBackups.All(e => File.Exists(e.BackupPath));
            manifest.State = legacyRecoverySafe ? PatchTransactionState.Committed : PatchTransactionState.Preparing;
            foreach (PatchEntry entry in manifest.Entries)
            {
                if (string.IsNullOrWhiteSpace(entry.BackupSha256) && File.Exists(entry.BackupPath))
                    entry.BackupSha256 = FileIntegrity.Sha256(entry.BackupPath);
                if (string.IsNullOrWhiteSpace(entry.OriginalSha256) && File.Exists(entry.BackupPath))
                    entry.OriginalSha256 = FileIntegrity.Sha256(entry.BackupPath);
                if (string.IsNullOrWhiteSpace(entry.PatchedSha256) && File.Exists(entry.DllPath))
                    entry.PatchedSha256 = FileIntegrity.Sha256(entry.DllPath);
                if (entry.Architecture == BinaryArchitecture.Unknown)
                    entry.Architecture = entry.Is64Bit ? BinaryArchitecture.X64 : BinaryArchitecture.X86;
            }
            foreach (FileBackup backup in manifest.TextBackups)
            {
                if (string.IsNullOrWhiteSpace(backup.BackupSha256) && File.Exists(backup.BackupPath))
                    backup.BackupSha256 = FileIntegrity.Sha256(backup.BackupPath);
                if (string.IsNullOrWhiteSpace(backup.OriginalSha256) && File.Exists(backup.BackupPath))
                    backup.OriginalSha256 = FileIntegrity.Sha256(backup.BackupPath);
                if (string.IsNullOrWhiteSpace(backup.ManagedContentSha256) && File.Exists(backup.OriginalPath))
                    backup.ManagedContentSha256 = FileIntegrity.Sha256(backup.OriginalPath);
            }
        }

        private static void ValidateManifest(PatchManifest manifest, string requestedGameDir)
        {
            if (manifest.SchemaVersion < 1 || manifest.SchemaVersion > 2)
                throw new InvalidDataException($"Desteklenmeyen manifest sürümü: {manifest.SchemaVersion}.");
            if (!Enum.IsDefined(typeof(PatchTransactionState), manifest.State))
                throw new InvalidDataException($"Geçersiz transaction durumu: {manifest.State}.");
            if (string.IsNullOrWhiteSpace(manifest.TransactionId))
                throw new InvalidDataException("Manifest TransactionId eksik.");
            if (string.IsNullOrWhiteSpace(manifest.GameDir))
                throw new InvalidDataException("Manifest GameDir eksik.");

            string root = PathSafety.Canonicalize(requestedGameDir);
            if (!string.Equals(PathSafety.Canonicalize(manifest.GameDir), root,
                OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
            {
                throw new InvalidDataException("Manifest farklı bir oyun klasörüne ait.");
            }

            if (!string.IsNullOrWhiteSpace(manifest.ExePath))
            {
                PathSafety.EnsureInside(root, manifest.ExePath, "ExePath");
                PathSafety.EnsureNoReparsePoint(root, manifest.ExePath, "ExePath");
            }

            foreach (PatchEntry entry in manifest.Entries)
            {
                PathSafety.EnsureInside(root, entry.DllPath, "DLL yolu");
                PathSafety.EnsureNoReparsePoint(root, entry.DllPath, "DLL yolu");
                PathSafety.EnsureInside(root, entry.BackupPath, "DLL yedek yolu");
                PathSafety.EnsureNoReparsePoint(root, entry.BackupPath, "DLL yedek yolu");
            }

            foreach (FileBackup backup in manifest.TextBackups)
            {
                PathSafety.EnsureInside(root, backup.OriginalPath, "metin dosyası yolu");
                PathSafety.EnsureNoReparsePoint(root, backup.OriginalPath, "metin dosyası yolu");
                PathSafety.EnsureInside(root, backup.BackupPath, "metin yedek yolu");
                PathSafety.EnsureNoReparsePoint(root, backup.BackupPath, "metin yedek yolu");
            }

            foreach (string path in manifest.CreatedFiles)
            {
                PathSafety.EnsureInside(root, path, "oluşturulan dosya yolu");
                PathSafety.EnsureNoReparsePoint(root, path, "oluşturulan dosya yolu");
            }

            foreach (ManagedFile file in manifest.CreatedFileRecords)
            {
                PathSafety.EnsureInside(root, file.Path, "managed created file");
                PathSafety.EnsureNoReparsePoint(root, file.Path, "managed created file");
            }

            foreach (string path in manifest.CreatedDirectories)
            {
                PathSafety.EnsureInside(root, path, "oluşturulan klasör yolu");
                PathSafety.EnsureNoReparsePoint(root, path, "oluşturulan klasör yolu");
            }

            if (!string.IsNullOrWhiteSpace(manifest.FirewallExePath))
            {
                PathSafety.EnsureInside(root, manifest.FirewallExePath, "Firewall exe yolu");
                PathSafety.EnsureNoReparsePoint(root, manifest.FirewallExePath, "Firewall exe yolu");
            }
        }

        private static void TryQuarantineCorruptFile(string path)
        {
            try
            {
                string quarantine = path + $".corrupt-{DateTime.UtcNow:yyyyMMddHHmmss}";
                if (File.Exists(path)) File.Move(path, quarantine, overwrite: false);
            }
            catch
            {
                // Bozuk dosyayı taşıyamamak uygulamanın açılmasını engellememeli.
            }
        }
    }
}
