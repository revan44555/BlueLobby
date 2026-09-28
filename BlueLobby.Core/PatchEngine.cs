using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BlueLobby.Platform;

namespace BlueLobby.Core
{
    /// <summary>
    /// Crash-safe dosya yama/geri yükleme motoru. UI bu sınıfa dosya mutation callback'i vermez;
    /// tüm transaction durumu manifest üzerinden kalıcı olarak tutulur.
    /// </summary>
    public sealed class PatchEngine
    {
        public sealed record PatchOptions(string PlayerName, string Language, string BroadcastIp, bool CreateDlcTemplate, int AppId);

        private readonly IPlatformServices _platform;
        private readonly PatchManifestStore _store;
        private readonly SemaphoreSlim _gate = new(1, 1);

        public PatchEngine(IPlatformServices platform, PatchManifestStore store)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
            _store = store ?? throw new ArgumentNullException(nameof(store));
        }

        public static byte[]? LoadComponentBytes(string appBaseDir, ApiDllTarget target)
        {
            string fileName = GameScanner.GetComponentFileName(target);
            string localPath = Path.Combine(appBaseDir, fileName);
            if (!File.Exists(localPath)) return null;

            BinaryArchitecture componentArch = BinaryArchitectureDetector.Detect(localPath);
            if (!BinaryArchitectureDetector.Matches(componentArch, target.Architecture)) return null;
            byte[] bytes = File.ReadAllBytes(localPath);
            if (!BinaryArchitectureDetector.Matches(BinaryArchitectureDetector.Detect(bytes), target.Architecture)) return null;
            return bytes;
        }

        public async Task<PatchManifest> ApplyAsync(
            string gameDir,
            string exePath,
            IReadOnlyList<(ApiDllTarget Target, byte[] Bytes)> buffers,
            PatchOptions options,
            bool addFirewallRule,
            IProgress<string>? reporter = null,
            CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                using IDisposable processLock = _store.AcquireGameLock(gameDir, TimeSpan.FromSeconds(15));
                return await Task.Run(() => ApplyCoreAsync(gameDir, exePath, buffers, options, addFirewallRule, reporter, ct), ct).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<PatchManifest> ApplyCoreAsync(
            string gameDir,
            string exePath,
            IReadOnlyList<(ApiDllTarget Target, byte[] Bytes)> buffers,
            PatchOptions options,
            bool addFirewallRule,
            IProgress<string>? reporter,
            CancellationToken ct)
        {
            string root = PathSafety.Canonicalize(gameDir);
            if (!Directory.Exists(root)) throw new DirectoryNotFoundException(root);
            if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath)) throw new InvalidDataException("Geçerli bir oyun çalıştırıcısı seçilmedi.");
            PathSafety.EnsureInside(root, exePath, "ExePath");
            PathSafety.EnsureNoReparsePoint(root, exePath, "ExePath");
            ValidateOptions(options);
            if (buffers.Count == 0) throw new InvalidDataException("Yamalanacak hedef bulunamadı.");

            ManifestReadResult existing = _store.ReadManifest(root);
            if (existing.Exists && !existing.Valid)
                throw new InvalidDataException($"Mevcut manifest bozuk: {existing.Error}");
            PatchManifest? previousManifest = existing.Manifest;
            if (previousManifest != null)
            {
                switch (previousManifest.State)
                {
                    case PatchTransactionState.Preparing:
                    case PatchTransactionState.Restoring:
                    case PatchTransactionState.RollingBack:
                        throw new InvalidOperationException("Bu oyun için tamamlanmamış bir transaction var. Önce recovery/geri alma tamamlanmalı.");
                    case PatchTransactionState.RolledBack:
                        CleanupBackupArtifacts(previousManifest);
                        _store.DeleteManifest(root);
                        break;
                    case PatchTransactionState.Committed:
                        if (HasActivePatchedState(previousManifest))
                            throw new InvalidOperationException("Bu oyun için hâlâ etkin bir BlueLobby yaması var. Önce Geri Yükle çalıştırılmalı.");

                        // Oyun dosyası Steam tarafından güncellenmiş veya kullanıcı tarafından değiştirilmişse
                        // eski backup'ı yeni baseline olarak kullanma. Yeni transaction kendi backup'ını üretir.
                        if (previousManifest.FirewallRuleCreatedByUs && !string.IsNullOrWhiteSpace(previousManifest.FirewallRuleName))
                        {
                            bool removed = await _platform.RemoveFirewallRuleAsync(previousManifest.FirewallRuleName).ConfigureAwait(false);
                            if (!removed) throw new IOException("Eski BlueLobby firewall kuralı güvenli biçimde kaldırılamadı.");
                        }
                        CleanupBackupArtifacts(previousManifest);
                        _store.DeleteManifest(root);
                        break;
                }
            }

            var manifest = new PatchManifest
            {
                SchemaVersion = 2,
                TransactionId = Guid.NewGuid().ToString("N"),
                State = PatchTransactionState.Preparing,
                GameDir = root,
                ExePath = PathSafety.Canonicalize(exePath),
                CreatedUtc = DateTime.UtcNow.ToString("O"),
            };

            PreflightTargets(root, buffers);
            _store.SaveManifest(manifest);
            reporter?.Report("Transaction kaydı oluşturuldu.");

            try
            {
                int index = 0;
                foreach ((ApiDllTarget target, byte[] bytes) in buffers)
                {
                    ct.ThrowIfCancellationRequested();
                    index++;
                    reporter?.Report($"API dosyası hazırlanıyor ({index}/{buffers.Count}): {Path.GetFileName(target.Path)}");
                    ApplyTarget(target, bytes, manifest, ct);
                    _store.SaveManifest(manifest);
                }

                ApplyManagedFiles(buffers, options, manifest, ct, reporter);
                _store.SaveManifest(manifest);

                if (addFirewallRule && _platform.SupportsFirewall && !string.IsNullOrWhiteSpace(manifest.ExePath))
                {
                    if (_platform.IsElevated)
                    {
                        reporter?.Report("Güvenlik duvarı kuralı hazırlanıyor...");
                        string ruleName = "BlueLobby_" + manifest.TransactionId;
                        manifest.FirewallRuleName = ruleName;
                        manifest.FirewallExePath = manifest.ExePath;
                        manifest.FirewallRuleCreatedByUs = true;
                        // Side-effect'ten önce intent'i kalıcılaştır. Crash olursa recovery bu adı siler.
                        _store.SaveManifest(manifest);
                        bool created = await _platform.EnsureFirewallRuleAsync(manifest.ExePath, ruleName).ConfigureAwait(false);
                        if (!created) throw new IOException("Güvenlik duvarı kuralı oluşturulamadı.");
                    }
                    else
                    {
                        reporter?.Report("Yönetici yetkisi yok; firewall kuralı eklenmedi.");
                    }
                }

                VerifyPatchedState(manifest);
                manifest.State = PatchTransactionState.Committed;
                manifest.CommittedUtc = DateTime.UtcNow.ToString("O");
                _store.SaveManifest(manifest);
                reporter?.Report("Transaction commit edildi.");
                return manifest;
            }
            catch
            {
                // Kullanıcı iptal etse bile rollback iptal edilemez; aksi halde transaction yarım kalabilir.
                await TryRollbackAfterFailureAsync(manifest, CancellationToken.None, reporter).ConfigureAwait(false);
                throw;
            }
        }

        private static bool HasActivePatchedState(PatchManifest manifest)
        {
            bool hasTrackedChanges = manifest.Entries.Count > 0 || manifest.TextBackups.Count > 0 || manifest.CreatedFileRecords.Count > 0;
            if (!hasTrackedChanges) return false;

            // A committed manifest is considered active only when every tracked artifact
            // still matches the exact bytes BlueLobby wrote. A single matching DLL must not
            // block a new transaction if the game was legitimately updated in the meantime.
            bool allDllsPatched = manifest.Entries.All(entry =>
                FileIntegrity.MatchesSha256(entry.DllPath, entry.PatchedSha256));
            bool allTextPatched = manifest.TextBackups.All(backup =>
                FileIntegrity.MatchesSha256(backup.OriginalPath, backup.ManagedContentSha256));
            bool allCreatedFilesIntact = manifest.CreatedFileRecords.All(file =>
                FileIntegrity.MatchesSha256(file.Path, file.ManagedSha256));
            return allDllsPatched && allTextPatched && allCreatedFilesIntact;
        }

        private static void PreflightTargets(string root, IReadOnlyList<(ApiDllTarget Target, byte[] Bytes)> buffers)
        {
            var seen = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            foreach ((ApiDllTarget target, byte[] bytes) in buffers)
            {
                PathSafety.EnsureInside(root, target.Path, "DLL hedefi");
                PathSafety.EnsureNoReparsePoint(root, target.Path, "DLL hedefi");
                if (!seen.Add(PathSafety.Canonicalize(target.Path)))
                    throw new InvalidDataException("Aynı DLL hedefi transaction'a birden fazla kez eklendi.");
                if (!File.Exists(target.Path)) throw new FileNotFoundException("API hedefi bulunamadı.", target.Path);
                if (bytes == null || bytes.Length == 0) throw new InvalidDataException("Bileşen dosyası boş.");
                BinaryArchitecture actual = BinaryArchitectureDetector.Detect(target.Path);
                if (target.Architecture == BinaryArchitecture.Unknown || actual == BinaryArchitecture.Unknown || actual != target.Architecture)
                    throw new InvalidDataException($"Hedef mimarisi doğrulanamadı veya değişmiş: {Path.GetFileName(target.Path)}.");
                BinaryArchitecture componentArch = BinaryArchitectureDetector.Detect(bytes);
                if (!BinaryArchitectureDetector.Matches(componentArch, target.Architecture))
                    throw new InvalidDataException($"Bileşen mimarisi hedefle uyumlu değil: {Path.GetFileName(target.Path)}.");
            }
        }

        private void ApplyTarget(ApiDllTarget target, byte[] bytes, PatchManifest manifest, CancellationToken ct)
        {
            string apiPath = PathSafety.Canonicalize(target.Path);
            PathSafety.EnsureInside(manifest.GameDir, apiPath, "DLL hedefi");
            PathSafety.EnsureNoReparsePoint(manifest.GameDir, apiPath, "DLL hedefi");
            ct.ThrowIfCancellationRequested();

            string backupPath = apiPath + ".bluelobby." + manifest.TransactionId + ".bak";
            string originalHash = FileIntegrity.Sha256(apiPath);
            PatchEntry entry = manifest.Entries.FirstOrDefault(e => string.Equals(e.DllPath, apiPath, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                ?? new PatchEntry
                {
                    DllPath = apiPath,
                    BackupPath = backupPath,
                    Is64Bit = target.Is64Bit,
                    Architecture = target.Architecture,
                    OriginalSha256 = originalHash,
                };

            if (manifest.Entries.All(e => !string.Equals(e.DllPath, apiPath, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
                manifest.Entries.Add(entry);

            // Önce intent'i journal'a yaz. Process tam backup kopyalanırken ölürse recovery
            // manifest'te hangi backup yolunu araması gerektiğini yine bilir.
            entry.OriginalSha256 = originalHash;
            entry.BackupPath = backupPath;
            entry.BackupSha256 = string.Empty;
            entry.PatchedSha256 = string.Empty;
            _store.SaveManifest(manifest);

            if (!File.Exists(backupPath))
                FileIntegrity.CopyDurably(apiPath, backupPath);

            string backupHash = FileIntegrity.Sha256(backupPath);
            if (!string.Equals(originalHash, backupHash, StringComparison.OrdinalIgnoreCase))
                throw new IOException($"Yedek bütünlüğü doğrulanamadı: {Path.GetFileName(apiPath)}.");

            entry.BackupSha256 = backupHash;
            _store.SaveManifest(manifest);

            FileIntegrity.WriteBytesDurably(apiPath, bytes);
            string patchedHash = FileIntegrity.Sha256(apiPath);
            entry.PatchedSha256 = patchedHash;
            _store.SaveManifest(manifest);

            if (patchedHash.Equals(originalHash, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"Bileşen orijinal dosyadan farklı değil: {Path.GetFileName(apiPath)}.");
        }

        private void ApplyManagedFiles(
            IReadOnlyList<(ApiDllTarget Target, byte[] Bytes)> buffers,
            PatchOptions options,
            PatchManifest manifest,
            CancellationToken ct,
            IProgress<string>? reporter)
        {
            var targetDirs = buffers
                .Select(x => Path.GetDirectoryName(PathSafety.Canonicalize(x.Target.Path))!)
                .Distinct(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .ToList();

            int i = 0;
            foreach (string dir in targetDirs)
            {
                i++;
                ct.ThrowIfCancellationRequested();
                reporter?.Report($"Oyun ayarları yazılıyor ({i}/{targetDirs.Count})...");

                WriteManagedTextFile(Path.Combine(dir, "steam_appid.txt"), options.AppId.ToString(), manifest);
                string settingsDir = Path.Combine(dir, "steam_settings");
                if (!Directory.Exists(settingsDir))
                {
                    GameScanner.AddUnique(manifest.CreatedDirectories, settingsDir);
                    _store.SaveManifest(manifest);
                    Directory.CreateDirectory(settingsDir);
                }

                if (!string.IsNullOrWhiteSpace(options.PlayerName))
                    WriteManagedTextFile(Path.Combine(settingsDir, "force_account_name.txt"), options.PlayerName, manifest);

                if (!string.IsNullOrWhiteSpace(options.Language))
                    WriteManagedTextFile(Path.Combine(settingsDir, "force_language.txt"), options.Language, manifest);

                if (options.CreateDlcTemplate)
                {
                    const string template = "# Bu dosyaya yalnızca sahip olduğunuz DLC AppID'lerini satır satır yazın.\n# Otomatik kilit aşma yapılmaz. Örnek: 123456\n";
                    WriteManagedTextFile(Path.Combine(settingsDir, "DLC.txt"), template, manifest);
                }

                if (!string.IsNullOrWhiteSpace(options.BroadcastIp) && IPAddress.TryParse(options.BroadcastIp, out IPAddress? ip) && ip.AddressFamily == AddressFamily.InterNetwork)
                    WriteManagedTextFile(Path.Combine(settingsDir, "custom_broadcasts.txt"), options.BroadcastIp, manifest);
            }
        }

        private void WriteManagedTextFile(string path, string content, PatchManifest manifest)
        {
            path = PathSafety.Canonicalize(path);
            PathSafety.EnsureInside(manifest.GameDir, path, "managed metin yolu");
            PathSafety.EnsureNoReparsePoint(manifest.GameDir, path, "managed metin yolu");

            string newContentHash = HashText(content);
            if (File.Exists(path))
            {
                string backupPath = path + ".bluelobby." + manifest.TransactionId + ".bak";
                string originalHash = FileIntegrity.Sha256(path);
                manifest.TextBackups.RemoveAll(b => string.Equals(b.OriginalPath, path, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
                var backup = new FileBackup
                {
                    OriginalPath = path,
                    BackupPath = backupPath,
                    OriginalSha256 = originalHash,
                    ManagedContentSha256 = newContentHash,
                };
                manifest.TextBackups.Add(backup);

                // Backup yolu ve original hash önce journal'a girer.
                _store.SaveManifest(manifest);
                if (!File.Exists(backupPath)) FileIntegrity.CopyDurably(path, backupPath);
                string backupHash = FileIntegrity.Sha256(backupPath);
                if (!string.Equals(originalHash, backupHash, StringComparison.OrdinalIgnoreCase))
                    throw new IOException($"Metin yedeği doğrulanamadı: {Path.GetFileName(path)}.");

                backup.BackupSha256 = backupHash;
                _store.SaveManifest(manifest);
            }
            else
            {
                GameScanner.AddUnique(manifest.CreatedFiles, path);
                manifest.CreatedFileRecords.RemoveAll(f => string.Equals(f.Path, path, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal));
                manifest.CreatedFileRecords.Add(new ManagedFile { Path = path, ManagedSha256 = newContentHash });
                _store.SaveManifest(manifest);
            }

            FileIntegrity.WriteTextDurably(path, content);
            if (!FileIntegrity.MatchesSha256(path, newContentHash))
                throw new IOException($"Yazılan dosyanın bütünlüğü doğrulanamadı: {Path.GetFileName(path)}.");
        }

        private void VerifyPatchedState(PatchManifest manifest)
        {
            foreach (PatchEntry entry in manifest.Entries)
            {
                if (!FileIntegrity.MatchesSha256(entry.DllPath, entry.PatchedSha256))
                    throw new IOException($"Yama doğrulanamadı: {Path.GetFileName(entry.DllPath)}.");
                if (!FileIntegrity.MatchesSha256(entry.BackupPath, entry.BackupSha256))
                    throw new IOException($"Yedek doğrulanamadı: {Path.GetFileName(entry.BackupPath)}.");
            }

            foreach (FileBackup backup in manifest.TextBackups)
            {
                if (!FileIntegrity.MatchesSha256(backup.OriginalPath, backup.ManagedContentSha256))
                    throw new IOException($"Managed dosya doğrulanamadı: {Path.GetFileName(backup.OriginalPath)}.");
                if (!FileIntegrity.MatchesSha256(backup.BackupPath, backup.BackupSha256))
                    throw new IOException($"Metin yedeği doğrulanamadı: {Path.GetFileName(backup.BackupPath)}.");
            }

            foreach (ManagedFile file in manifest.CreatedFileRecords)
            {
                if (!FileIntegrity.MatchesSha256(file.Path, file.ManagedSha256))
                    throw new IOException($"Yeni oluşturulan dosya doğrulanamadı: {Path.GetFileName(file.Path)}.");
            }
        }

        private async Task TryRollbackAfterFailureAsync(PatchManifest manifest, CancellationToken ct, IProgress<string>? reporter)
        {
            try
            {
                manifest.State = PatchTransactionState.RollingBack;
                _store.SaveManifest(manifest);
                reporter?.Report("Hata oluştu; transaction geri alınıyor...");
                await RollbackCoreAsync(manifest, ct, reporter).ConfigureAwait(false);
                manifest.State = PatchTransactionState.RolledBack;
                _store.SaveManifest(manifest);
                CleanupBackupArtifacts(manifest);
                _store.DeleteManifest(manifest.GameDir);
            }
            catch (Exception rollbackEx)
            {
                try
                {
                    manifest.State = PatchTransactionState.RollingBack;
                    _store.SaveManifest(manifest);
                }
                catch
                {
                    // En kötü durumda dosya yedekleri ve journal fiziksel olarak kalır.
                }
                throw new InvalidOperationException("Yama işlemi başarısız oldu ve otomatik geri alma da tamamlanamadı. Manifest recovery için korunuyor.", rollbackEx);
            }
        }

        private async Task RollbackCoreAsync(PatchManifest manifest, CancellationToken ct, IProgress<string>? reporter)
        {
            foreach (PatchEntry entry in manifest.Entries.AsEnumerable().Reverse())
            {
                ct.ThrowIfCancellationRequested();
                if (!File.Exists(entry.BackupPath))
                {
                    if (FileIntegrity.MatchesSha256(entry.DllPath, entry.OriginalSha256)) continue;
                    throw new InvalidDataException($"Rollback için DLL yedeği bulunamadı: {Path.GetFileName(entry.BackupPath)}.");
                }
                if (string.IsNullOrWhiteSpace(entry.BackupSha256))
                {
                    if (!FileIntegrity.MatchesSha256(entry.BackupPath, entry.OriginalSha256))
                        throw new InvalidDataException($"Rollback DLL yedeği doğrulanamadı: {Path.GetFileName(entry.BackupPath)}.");
                }
                else if (!FileIntegrity.MatchesSha256(entry.BackupPath, entry.BackupSha256))
                    throw new InvalidDataException($"Rollback DLL yedeği değişmiş: {Path.GetFileName(entry.BackupPath)}.");
                reporter?.Report($"Geri alınıyor: {Path.GetFileName(entry.DllPath)}");
                FileIntegrity.CopyDurably(entry.BackupPath, entry.DllPath);
                if (!FileIntegrity.MatchesSha256(entry.DllPath, entry.OriginalSha256))
                    throw new IOException($"DLL geri alma doğrulanamadı: {Path.GetFileName(entry.DllPath)}.");
            }

            foreach (FileBackup backup in manifest.TextBackups.AsEnumerable().Reverse())
            {
                ct.ThrowIfCancellationRequested();
                if (!File.Exists(backup.BackupPath))
                {
                    if (FileIntegrity.MatchesSha256(backup.OriginalPath, backup.OriginalSha256)) continue;
                    throw new InvalidDataException($"Rollback için metin yedeği bulunamadı: {Path.GetFileName(backup.BackupPath)}.");
                }
                if (string.IsNullOrWhiteSpace(backup.BackupSha256))
                {
                    if (!FileIntegrity.MatchesSha256(backup.BackupPath, backup.OriginalSha256))
                        throw new InvalidDataException($"Rollback metin yedeği doğrulanamadı: {Path.GetFileName(backup.BackupPath)}.");
                }
                else if (!FileIntegrity.MatchesSha256(backup.BackupPath, backup.BackupSha256))
                    throw new InvalidDataException($"Rollback metin yedeği değişmiş: {Path.GetFileName(backup.BackupPath)}.");
                FileIntegrity.CopyDurably(backup.BackupPath, backup.OriginalPath);
                if (!FileIntegrity.MatchesSha256(backup.OriginalPath, backup.OriginalSha256))
                    throw new IOException($"Metin geri alma doğrulanamadı: {Path.GetFileName(backup.OriginalPath)}.");
            }

            foreach (ManagedFile file in manifest.CreatedFileRecords)
            {
                ct.ThrowIfCancellationRequested();
                if (File.Exists(file.Path) && FileIntegrity.MatchesSha256(file.Path, file.ManagedSha256)) File.Delete(file.Path);
            }

            foreach (string file in manifest.CreatedFiles)
            {
                ct.ThrowIfCancellationRequested();
                if (!manifest.CreatedFileRecords.Any(x => string.Equals(x.Path, file, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) && File.Exists(file))
                    File.Delete(file);
            }

            foreach (string dir in manifest.CreatedDirectories.OrderByDescending(d => d.Length))
            {
                ct.ThrowIfCancellationRequested();
                if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any()) Directory.Delete(dir, false);
            }

            if (manifest.FirewallRuleCreatedByUs && !string.IsNullOrWhiteSpace(manifest.FirewallRuleName))
            {
                bool removed = await _platform.RemoveFirewallRuleAsync(manifest.FirewallRuleName).ConfigureAwait(false);
                if (!removed) throw new IOException("Firewall rollback tamamlanamadı.");
            }
        }

        public async Task<RestoreResult> RestoreGameAsync(
            string gameDir,
            PatchManifest manifest,
            IProgress<(double Percent, string Message)>? reporter,
            CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                using IDisposable processLock = _store.AcquireGameLock(gameDir, TimeSpan.FromSeconds(15));
                return await Task.Run(() => RestoreCoreAsync(gameDir, manifest, reporter, ct), ct).ConfigureAwait(false);
            }
            finally
            {
                _gate.Release();
            }
        }

        private async Task<RestoreResult> RestoreCoreAsync(string gameDir, PatchManifest manifest, IProgress<(double Percent, string Message)>? reporter, CancellationToken ct)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            string root = PathSafety.Canonicalize(gameDir);
            PathSafety.EnsureInside(root, manifest.GameDir, "manifest GameDir");

            // Cross-process lock alındıktan sonra disk üzerindeki en güncel transaction'ı kullan.
            // Böylece başka bir BlueLobby örneğinin stale in-memory manifest'i restore'u ezemez.
            ManifestReadResult latest = _store.ReadManifest(root);
            if (!latest.Exists || !latest.Valid || latest.Manifest == null)
                throw new InvalidDataException($"Restore manifest'i diskten okunamadı: {latest.Error}");
            if (!string.Equals(latest.Manifest.TransactionId, manifest.TransactionId, StringComparison.Ordinal))
                throw new InvalidOperationException("Restore için verilen manifest artık aktif transaction ile eşleşmiyor.");
            manifest = latest.Manifest;
            PreflightRestore(manifest, root);

            manifest.State = PatchTransactionState.Restoring;
            _store.SaveManifest(manifest);
            var result = new RestoreResult();

            try
            {
                int total = Math.Max(1, manifest.Entries.Count);
                int index = 0;
                foreach (PatchEntry entry in manifest.Entries)
                {
                    ct.ThrowIfCancellationRequested();
                    index++;
                    reporter?.Report((10.0 + 45.0 * index / total, $"API geri yükleniyor ({index}/{total})..."));
                    RestorePatchEntry(entry);
                    result.RestoredDllBackups++;
                }

                reporter?.Report((60, "Metin dosyaları geri yükleniyor..."));
                foreach (FileBackup backup in manifest.TextBackups)
                {
                    ct.ThrowIfCancellationRequested();
                    RestoreTextBackup(backup);
                    result.RestoredTextBackups++;
                }

                reporter?.Report((75, "Araç tarafından oluşturulan dosyalar temizleniyor..."));
                RemoveManagedFiles(manifest, result);
                RemoveManagedDirectories(manifest, result);

                if (manifest.FirewallRuleCreatedByUs && !string.IsNullOrWhiteSpace(manifest.FirewallRuleName))
                {
                    reporter?.Report((88, "Firewall kuralı kaldırılıyor..."));
                    result.FirewallRuleRemoved = await _platform.RemoveFirewallRuleAsync(manifest.FirewallRuleName).ConfigureAwait(false);
                    if (!result.FirewallRuleRemoved) throw new IOException("BlueLobby tarafından oluşturulan firewall kuralı kaldırılamadı.");
                }

                VerifyRestoredState(manifest);
                manifest.State = PatchTransactionState.RolledBack;
                _store.SaveManifest(manifest);
                reporter?.Report((95, "Yedekler temizleniyor..."));
                CleanupBackupArtifacts(manifest);
                _store.DeleteManifest(manifest.GameDir);

                result.Message = $"Geri yükleme tamamlandı. API yedeği: {result.RestoredDllBackups}, metin yedeği: {result.RestoredTextBackups}, oluşturulan dosya: {result.RemovedCreatedFiles}.";
                reporter?.Report((100, result.Message));
                return result;
            }
            catch
            {
                if (manifest.State != PatchTransactionState.RolledBack)
                {
                    manifest.State = PatchTransactionState.Restoring;
                    try { _store.SaveManifest(manifest); } catch { }
                }
                throw;
            }
        }

        private static void PreflightRestore(PatchManifest manifest, string root)
        {
            if (manifest.State is not (PatchTransactionState.Committed or PatchTransactionState.Restoring))
                throw new InvalidDataException($"Restore için manifest durumu uygun değil: {manifest.State}.");

            foreach (PatchEntry entry in manifest.Entries)
            {
                PathSafety.EnsureInside(root, entry.DllPath, "DLL yolu");
                PathSafety.EnsureNoReparsePoint(root, entry.DllPath, "DLL yolu");
                PathSafety.EnsureInside(root, entry.BackupPath, "DLL yedek yolu");
                PathSafety.EnsureNoReparsePoint(root, entry.BackupPath, "DLL yedek yolu");
                if (!FileIntegrity.MatchesSha256(entry.BackupPath, entry.BackupSha256))
                    throw new InvalidDataException($"DLL yedeği eksik veya bozuk: {Path.GetFileName(entry.BackupPath)}.");
                EnsureRestoreTargetIsSafe(entry.DllPath, entry.OriginalSha256, entry.PatchedSha256);
            }

            foreach (FileBackup backup in manifest.TextBackups)
            {
                PathSafety.EnsureInside(root, backup.OriginalPath, "metin yolu");
                PathSafety.EnsureNoReparsePoint(root, backup.OriginalPath, "metin yolu");
                PathSafety.EnsureInside(root, backup.BackupPath, "metin yedek yolu");
                PathSafety.EnsureNoReparsePoint(root, backup.BackupPath, "metin yedek yolu");
                if (!FileIntegrity.MatchesSha256(backup.BackupPath, backup.BackupSha256))
                    throw new InvalidDataException($"Metin yedeği eksik veya bozuk: {Path.GetFileName(backup.BackupPath)}.");
                EnsureRestoreTargetIsSafe(backup.OriginalPath, backup.OriginalSha256, backup.ManagedContentSha256);
            }

            foreach (ManagedFile file in manifest.CreatedFileRecords)
            {
                PathSafety.EnsureInside(root, file.Path, "managed created file");
                PathSafety.EnsureNoReparsePoint(root, file.Path, "managed created file");
            }

            foreach (string file in manifest.CreatedFiles)
            {
                PathSafety.EnsureInside(root, file, "created file");
                PathSafety.EnsureNoReparsePoint(root, file, "created file");
            }

            foreach (string dir in manifest.CreatedDirectories)
            {
                PathSafety.EnsureInside(root, dir, "created directory");
                PathSafety.EnsureNoReparsePoint(root, dir, "created directory");
            }
        }

        private static void EnsureRestoreTargetIsSafe(string path, string originalHash, string managedHash)
        {
            if (!File.Exists(path)) return;
            string current = FileIntegrity.Sha256(path);
            if (string.Equals(current, originalHash, StringComparison.OrdinalIgnoreCase)) return;
            if (string.Equals(current, managedHash, StringComparison.OrdinalIgnoreCase)) return;
            throw new InvalidDataException($"Kullanıcı değişikliği tespit edildi: {Path.GetFileName(path)}. Dosya üzerine yazılmadan restore durduruldu.");
        }

        private static void RestorePatchEntry(PatchEntry entry)
        {
            if (!FileIntegrity.MatchesSha256(entry.BackupPath, entry.BackupSha256))
                throw new InvalidDataException($"Yedek değişmiş: {Path.GetFileName(entry.BackupPath)}.");
            FileIntegrity.CopyDurably(entry.BackupPath, entry.DllPath);
            if (!FileIntegrity.MatchesSha256(entry.DllPath, entry.OriginalSha256))
                throw new IOException($"DLL restore doğrulanamadı: {Path.GetFileName(entry.DllPath)}.");
        }

        private static void RestoreTextBackup(FileBackup backup)
        {
            if (!FileIntegrity.MatchesSha256(backup.BackupPath, backup.BackupSha256))
                throw new InvalidDataException($"Metin yedeği değişmiş: {Path.GetFileName(backup.BackupPath)}.");
            FileIntegrity.CopyDurably(backup.BackupPath, backup.OriginalPath);
            if (!FileIntegrity.MatchesSha256(backup.OriginalPath, backup.OriginalSha256))
                throw new IOException($"Metin restore doğrulanamadı: {Path.GetFileName(backup.OriginalPath)}.");
        }

        private static void RemoveManagedFiles(PatchManifest manifest, RestoreResult result)
        {
            foreach (ManagedFile file in manifest.CreatedFileRecords)
            {
                if (File.Exists(file.Path) && FileIntegrity.MatchesSha256(file.Path, file.ManagedSha256))
                {
                    File.Delete(file.Path);
                    result.RemovedCreatedFiles++;
                }
                else if (File.Exists(file.Path))
                {
                    throw new InvalidDataException($"BlueLobby tarafından oluşturulan dosya kullanıcı tarafından değiştirilmiş: {Path.GetFileName(file.Path)}.");
                }
            }

            foreach (string file in manifest.CreatedFiles)
            {
                if (!manifest.CreatedFileRecords.Any(x => string.Equals(x.Path, file, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) && File.Exists(file))
                {
                    File.Delete(file);
                    result.RemovedCreatedFiles++;
                }
            }
        }

        private static void RemoveManagedDirectories(PatchManifest manifest, RestoreResult result)
        {
            foreach (string dir in manifest.CreatedDirectories.OrderByDescending(d => d.Length))
            {
                if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir, false);
                    result.RemovedCreatedDirectories = true;
                }
            }
        }

        private static void VerifyRestoredState(PatchManifest manifest)
        {
            foreach (PatchEntry entry in manifest.Entries)
            {
                if (!FileIntegrity.MatchesSha256(entry.DllPath, entry.OriginalSha256))
                    throw new IOException($"Restore sonrası DLL doğrulanamadı: {Path.GetFileName(entry.DllPath)}.");
            }
            foreach (FileBackup backup in manifest.TextBackups)
            {
                if (!FileIntegrity.MatchesSha256(backup.OriginalPath, backup.OriginalSha256))
                    throw new IOException($"Restore sonrası metin dosyası doğrulanamadı: {Path.GetFileName(backup.OriginalPath)}.");
            }
        }

        private static void CleanupBackupArtifacts(PatchManifest manifest)
        {
            foreach (PatchEntry entry in manifest.Entries)
            {
                FileIntegrity.TryDelete(entry.BackupPath);
                if (File.Exists(entry.BackupPath)) throw new IOException($"Backup temizlenemedi: {Path.GetFileName(entry.BackupPath)}.");
            }
            foreach (FileBackup backup in manifest.TextBackups)
            {
                FileIntegrity.TryDelete(backup.BackupPath);
                if (File.Exists(backup.BackupPath)) throw new IOException($"Metin backup temizlenemedi: {Path.GetFileName(backup.BackupPath)}.");
            }
        }

        public async Task<bool> RecoverInterruptedTransactionAsync(
            string gameDir,
            IProgress<(double Percent, string Message)>? reporter = null,
            CancellationToken ct = default)
        {
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                using IDisposable processLock = _store.AcquireGameLock(gameDir, TimeSpan.FromSeconds(15));
                string root = PathSafety.Canonicalize(gameDir);
                ManifestReadResult read = _store.ReadManifest(root);
                if (!read.Exists) return false;
                if (!read.Valid || read.Manifest == null)
                    throw new InvalidDataException($"Manifest recovery için okunamadı: {read.Error}");

                PatchManifest manifest = read.Manifest;
                switch (manifest.State)
                {
                    case PatchTransactionState.Preparing:
                    case PatchTransactionState.RollingBack:
                        manifest.State = PatchTransactionState.RollingBack;
                        _store.SaveManifest(manifest);
                        await RollbackCoreAsync(manifest, CancellationToken.None, null).ConfigureAwait(false);
                        manifest.State = PatchTransactionState.RolledBack;
                        _store.SaveManifest(manifest);
                        CleanupBackupArtifacts(manifest);
                        _store.DeleteManifest(root);
                        reporter?.Report((100, "Yarım kalan patch transaction geri alındı."));
                        return true;

                    case PatchTransactionState.Restoring:
                        await RestoreCoreAsync(root, manifest, reporter, CancellationToken.None).ConfigureAwait(false);
                        return true;

                    case PatchTransactionState.RolledBack:
                        CleanupBackupArtifacts(manifest);
                        _store.DeleteManifest(root);
                        reporter?.Report((100, "Eski recovery kaydı temizlendi."));
                        return true;

                    default:
                        return false;
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        public async Task RollbackManifestAsync(PatchManifest manifest, CancellationToken ct = default)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            await _gate.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                using IDisposable processLock = _store.AcquireGameLock(manifest.GameDir, TimeSpan.FromSeconds(15));
                ManifestReadResult latest = _store.ReadManifest(manifest.GameDir);
                if (!latest.Exists || !latest.Valid || latest.Manifest == null)
                    throw new InvalidDataException($"Rollback manifest'i diskten okunamadı: {latest.Error}");
                if (!string.Equals(latest.Manifest.TransactionId, manifest.TransactionId, StringComparison.Ordinal))
                    throw new InvalidOperationException("Rollback için verilen manifest artık aktif transaction ile eşleşmiyor.");
                PatchManifest current = latest.Manifest;

                current.State = PatchTransactionState.RollingBack;
                _store.SaveManifest(current);
                await RollbackCoreAsync(current, CancellationToken.None, null).ConfigureAwait(false);
                current.State = PatchTransactionState.RolledBack;
                _store.SaveManifest(current);
                CleanupBackupArtifacts(current);
                _store.DeleteManifest(current.GameDir);
            }
            finally
            {
                _gate.Release();
            }
        }

        private static void ValidateOptions(PatchOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            if (options.AppId <= 0) throw new InvalidDataException("AppID pozitif olmalı.");
            if (options.PlayerName.Length > 256) throw new InvalidDataException("Oyuncu adı 256 karakteri aşamaz.");
            if (options.Language.Length > 64) throw new InvalidDataException("Dil değeri 64 karakteri aşamaz.");
            if (options.BroadcastIp.Length > 64) throw new InvalidDataException("Broadcast IP değeri 64 karakteri aşamaz.");
            if (!string.IsNullOrWhiteSpace(options.BroadcastIp) &&
                (!IPAddress.TryParse(options.BroadcastIp, out IPAddress? ip) || ip.AddressFamily != AddressFamily.InterNetwork))
                throw new InvalidDataException("Broadcast IP geçerli bir IPv4 adresi olmalı.");
        }

        private static string HashText(string content)
        {
            return Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(new UTF8Encoding(false).GetBytes(content))).ToLowerInvariant();
        }
    }
}
