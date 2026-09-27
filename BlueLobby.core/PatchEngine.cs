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
    /// <summary>UI katmanından bağımsız yama/geri yükleme motoru. Windows ve Linux'ta aynen çalışır.</summary>
    public sealed class PatchEngine
    {
        public sealed record PatchOptions(string PlayerName, string Language, string BroadcastIp, bool CreateDlcTemplate, int AppId);

        private readonly IPlatformServices _platform;

        public PatchEngine(IPlatformServices platform)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        }

        public static byte[]? LoadComponentBytes(string appBaseDir, ApiDllTarget target)
        {
            string fileName = GameScanner.GetComponentFileName(target);
            string localPath = Path.Combine(appBaseDir, fileName);
            return File.Exists(localPath) ? File.ReadAllBytes(localPath) : null;
        }

        public void ApplyPatchToTarget(ApiDllTarget target, byte[] componentBytes, PatchOptions options, PatchManifest manifest, Action<string>? log = null)
        {
            string apiPath = target.Path;
            string dir = Path.GetDirectoryName(apiPath) ?? manifest.GameDir;
            string backupPath = apiPath + ".bak";

            if (!File.Exists(backupPath))
            {
                File.Copy(apiPath, backupPath);
                log?.Invoke($"Orijinal yedeklendi: {Path.GetFileName(backupPath)}");
            }

            string tempPath = apiPath + ".tmp";
            File.WriteAllBytes(tempPath, componentBytes);
            File.Copy(tempPath, apiPath, overwrite: true);
            File.Delete(tempPath);

            manifest.Entries.RemoveAll(e => string.Equals(e.DllPath, apiPath, StringComparison.OrdinalIgnoreCase));
            manifest.Entries.Add(new PatchEntry { DllPath = apiPath, BackupPath = backupPath, Is64Bit = target.Is64Bit });

            WriteManagedTextFile(Path.Combine(dir, "steam_appid.txt"), options.AppId.ToString(), manifest);
            string settingsDir = Path.Combine(dir, "steam_settings");
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
                GameScanner.AddUnique(manifest.CreatedDirectories, settingsDir);
            }

            if (!string.IsNullOrWhiteSpace(options.PlayerName))
            {
                WriteManagedTextFile(Path.Combine(settingsDir, "force_account_name.txt"), options.PlayerName, manifest);
            }

            if (!string.IsNullOrWhiteSpace(options.Language))
            {
                WriteManagedTextFile(Path.Combine(settingsDir, "force_language.txt"), options.Language, manifest);
            }

            if (options.CreateDlcTemplate)
            {
                const string template = "# Bu dosyaya yalnızca sahip olduğunuz DLC AppID'lerini satır satır yazın." + "\n" +
                                        "# Otomatik kilit aşma yapılmaz. Örnek: 123456" + "\n";
                WriteManagedTextFile(Path.Combine(settingsDir, "DLC.txt"), template, manifest);
            }

            if (!string.IsNullOrWhiteSpace(options.BroadcastIp) && IPAddress.TryParse(options.BroadcastIp, out IPAddress? ip) && ip.AddressFamily == AddressFamily.InterNetwork)
            {
                WriteManagedTextFile(Path.Combine(settingsDir, "custom_broadcasts.txt"), options.BroadcastIp, manifest);
            }

            log?.Invoke($"{(target.Is64Bit ? "64-bit" : "32-bit")} hedef güncellendi: {Path.GetFileName(apiPath)}");
        }

        public static void WriteManagedTextFile(string path, string content, PatchManifest manifest)
        {
            if (File.Exists(path))
            {
                string backupPath = path + ".pre_slcc";
                if (!File.Exists(backupPath))
                {
                    File.Copy(path, backupPath);
                    manifest.TextBackups.RemoveAll(b => string.Equals(b.OriginalPath, path, StringComparison.OrdinalIgnoreCase));
                    manifest.TextBackups.Add(new FileBackup { OriginalPath = path, BackupPath = backupPath });
                }
            }
            else
            {
                GameScanner.AddUnique(manifest.CreatedFiles, path);
            }

            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Copy(tempPath, path, overwrite: true);
            File.Delete(tempPath);
        }

        public async Task<RestoreResult> RestoreGameAsync(string gameDir, PatchManifest? manifest, IProgress<(double Percent, string Message)> reporter)
        {
            return await Task.Run(async () =>
            {
                var result = new RestoreResult();
                reporter.Report((10, "Geri alma kaydı okunuyor..."));

                if (manifest == null)
                {
                    reporter.Report((40, "Yedekler aranıyor..."));
                    var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
                    var backups = new List<string>();
                    foreach (string path in Directory.EnumerateFiles(gameDir, "*", options))
                    {
                        string fileName = Path.GetFileName(path);
                        if (fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) &&
                            fileName.StartsWith("steam_api", StringComparison.OrdinalIgnoreCase) &&
                            (fileName.Contains(".dll", StringComparison.OrdinalIgnoreCase) || fileName.Contains(".so", StringComparison.OrdinalIgnoreCase)))
                        {
                            backups.Add(path);
                        }
                    }
                    backups.Sort(StringComparer.OrdinalIgnoreCase);

                    int i = 0;
                    foreach (string bakPath in backups)
                    {
                        i++;
                        reporter.Report((40.0 + 50.0 * i / Math.Max(1, backups.Count), $"Yedek geri yükleniyor ({i}/{backups.Count})..."));
                        string original = bakPath.Substring(0, bakPath.Length - 4);
                        File.Copy(bakPath, original, overwrite: true);
                        File.Delete(bakPath);
                        result.RestoredDllBackups++;
                    }

                    result.Message = result.RestoredDllBackups > 0
                        ? $"Manifest yoktu; yalnızca {result.RestoredDllBackups} adet API yedeği geri yüklendi."
                        : "Geri alma kaydı ve uygun yedek bulunamadı.";
                    return result;
                }

                int total = Math.Max(1, manifest.Entries.Count);
                int n = 0;
                foreach (PatchEntry entry in manifest.Entries.ToList())
                {
                    n++;
                    reporter.Report((10.0 + 50.0 * n / total, $"API dosyası geri yükleniyor ({n}/{total})..."));
                    if (File.Exists(entry.BackupPath))
                    {
                        File.Copy(entry.BackupPath, entry.DllPath, overwrite: true);
                        File.Delete(entry.BackupPath);
                        result.RestoredDllBackups++;
                    }
                }

                reporter.Report((65, "Metin dosyaları geri yükleniyor..."));
                foreach (FileBackup backup in manifest.TextBackups.ToList())
                {
                    if (File.Exists(backup.BackupPath))
                    {
                        File.Copy(backup.BackupPath, backup.OriginalPath, overwrite: true);
                        File.Delete(backup.BackupPath);
                        result.RestoredTextBackups++;
                    }
                }

                reporter.Report((80, "Araç dosyaları temizleniyor..."));
                foreach (string createdFile in manifest.CreatedFiles.ToList())
                {
                    if (File.Exists(createdFile))
                    {
                        File.Delete(createdFile);
                        result.RemovedCreatedFiles++;
                    }
                }

                foreach (string createdDirectory in manifest.CreatedDirectories.OrderByDescending(d => d.Length).ToList())
                {
                    if (Directory.Exists(createdDirectory) && !Directory.EnumerateFileSystemEntries(createdDirectory).Any())
                    {
                        Directory.Delete(createdDirectory, recursive: false);
                        result.RemovedCreatedDirectories = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(manifest.FirewallRuleName))
                {
                    reporter.Report((92, "Firewall kuralı kaldırılıyor..."));
                    result.FirewallRuleRemoved = await _platform.RemoveFirewallRuleAsync(manifest.FirewallRuleName);
                }

                result.Message = $"Geri yükleme tamamlandı. API yedeği: {result.RestoredDllBackups}, metin yedeği: {result.RestoredTextBackups}, oluşturulan dosya: {result.RemovedCreatedFiles}.";
                return result;
            });
        }

        public Task RollbackManifestAsync(PatchManifest manifest, CancellationToken ct = default)
        {
            return Task.Run(() =>
            {
                foreach (PatchEntry entry in manifest.Entries)
                {
                    ct.ThrowIfCancellationRequested();
                    if (File.Exists(entry.BackupPath))
                    {
                        File.Copy(entry.BackupPath, entry.DllPath, overwrite: true);
                    }
                }

                foreach (FileBackup backup in manifest.TextBackups)
                {
                    ct.ThrowIfCancellationRequested();
                    if (File.Exists(backup.BackupPath))
                    {
                        File.Copy(backup.BackupPath, backup.OriginalPath, overwrite: true);
                    }
                }

                foreach (string file in manifest.CreatedFiles)
                {
                    ct.ThrowIfCancellationRequested();
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }

                foreach (string dir in manifest.CreatedDirectories.OrderByDescending(d => d.Length))
                {
                    ct.ThrowIfCancellationRequested();
                    if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir, recursive: false);
                    }
                }
            }, ct);
        }
    }
}
