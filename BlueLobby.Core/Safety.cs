using System;
using System.Collections.Generic;
using System.IO;

namespace BlueLobby.Core
{
    public static class GameVerifier
    {
        public sealed class Result
        {
            public bool LooksLikeGame { get; set; }
            public string Reason { get; set; } = string.Empty;
            public int TargetCount { get; set; }
            public int ExeCount { get; set; }
        }

        public static Result VerifyGameFolder(string dir, string? exePath = null)
        {
            var result = new Result();
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                result.Reason = "Klasör bulunamadı.";
                return result;
            }

            try
            {
                string root = PathSafety.Canonicalize(dir);
                result.TargetCount = GameScanner.FindApiDllTargets(root, exePath).Count;

                if (!string.IsNullOrWhiteSpace(exePath) && File.Exists(exePath) && PathSafety.IsInside(root, exePath))
                {
                    result.ExeCount = 1;
                }
                else
                {
                    foreach (string f in Directory.EnumerateFiles(root, "*", new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = false }))
                    {
                        string ext = Path.GetExtension(f).ToLowerInvariant();
                        if (ext is ".exe" or ".sh" or ".x86_64" or ".x86") result.ExeCount++;
                        else if (ext == string.Empty && !Path.GetFileName(f).Contains(".")) result.ExeCount++;
                    }
                }

                result.LooksLikeGame = result.TargetCount > 0 && result.ExeCount > 0;
                result.Reason = result.LooksLikeGame
                    ? string.Empty
                    : result.TargetCount == 0 ? "Seçilen çalıştırıcıyla aynı klasörde API hedefi bulunamadı."
                    : result.ExeCount == 0 ? "Çalıştırılabilir dosya bulunamadı."
                    : "Klasör doğrulaması tamamlanamadı.";
                return result;
            }
            catch (Exception ex)
            {
                result.Reason = "Klasör taranamadı: " + ex.Message;
                return result;
            }
        }
    }

    public static class BackupValidator
    {
        public sealed class Result
        {
            public bool Valid { get; set; }
            public List<string> Problems { get; } = new();
        }

        public static Result Validate(PatchManifest manifest)
        {
            var result = new Result();
            foreach (PatchEntry entry in manifest.Entries)
            {
                if (!File.Exists(entry.BackupPath))
                {
                    result.Problems.Add($"Yedek eksik: {entry.BackupPath}");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(entry.BackupSha256) || !FileIntegrity.MatchesSha256(entry.BackupPath, entry.BackupSha256))
                    result.Problems.Add($"Yedek bütünlüğü bozuk: {entry.BackupPath}");
            }

            foreach (FileBackup backup in manifest.TextBackups)
            {
                if (!File.Exists(backup.BackupPath))
                {
                    result.Problems.Add($"Metin yedeği eksik: {backup.BackupPath}");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(backup.BackupSha256) || !FileIntegrity.MatchesSha256(backup.BackupPath, backup.BackupSha256))
                    result.Problems.Add($"Metin yedeği bütünlüğü bozuk: {backup.BackupPath}");
            }

            result.Valid = result.Problems.Count == 0;
            return result;
        }

        public static bool IsPatchStale(PatchManifest manifest)
        {
            if (manifest.State != PatchTransactionState.Committed) return true;

            foreach (PatchEntry entry in manifest.Entries)
            {
                if (!File.Exists(entry.DllPath) || !File.Exists(entry.BackupPath)) return true;
                if (!FileIntegrity.MatchesSha256(entry.BackupPath, entry.BackupSha256)) return true;

                string current = FileIntegrity.Sha256(entry.DllPath);
                if (string.Equals(current, entry.PatchedSha256, StringComparison.OrdinalIgnoreCase)) continue;
                if (string.Equals(current, entry.OriginalSha256, StringComparison.OrdinalIgnoreCase)) return true;
                return true;
            }

            foreach (FileBackup backup in manifest.TextBackups)
            {
                if (!File.Exists(backup.OriginalPath)) return true;
                string current = FileIntegrity.Sha256(backup.OriginalPath);
                if (!string.Equals(current, backup.ManagedContentSha256, StringComparison.OrdinalIgnoreCase)) return true;
            }

            return false;
        }
    }
}
