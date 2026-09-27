using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlueLobby.Core
{
    /// <summary>Madde C (güvenlik ağı) altyapısı: uygulama öncesi/sonrası doğrulamalar.</summary>
    public static class GameVerifier
    {
        public sealed class Result
        {
            public bool LooksLikeGame { get; set; }
            public string Reason { get; set; } = string.Empty;
            public int TargetCount { get; set; }
            public int ExeCount { get; set; }
        }

        /// <summary>Klasörün gerçekten bir oyun kurulumu gibi görünüp görünmediğini denetler.</summary>
        public static Result VerifyGameFolder(string dir)
        {
            var result = new Result();
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
            {
                result.Reason = "Klasör bulunamadı.";
                return result;
            }

            try
            {
                var options = new EnumerationOptions { IgnoreInaccessible = true, RecurseSubdirectories = true };
                result.TargetCount = GameScanner.FindApiDllTargets(dir).Count;

                // Çalıştırılabilir adayları kökte say (exe / sh / uzantısız)
                foreach (string f in Directory.EnumerateFiles(dir, "*", new EnumerationOptions { IgnoreInaccessible = true }))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    if (ext == ".exe" || ext == ".sh" || ext == ".x86_64" ||
                        (ext == string.Empty && !Path.GetFileName(f).Contains(".")))
                    {
                        result.ExeCount++;
                    }
                }

                // Dosya sayısı anormal düşükse (boş/yarım klasör) şüphelen
                int fileCount = Directory.EnumerateFiles(dir, "*", options).Count();
                result.LooksLikeGame = result.TargetCount > 0 && result.ExeCount > 0 && fileCount > 10;
                result.Reason = result.LooksLikeGame
                    ? string.Empty
                    : result.TargetCount == 0 ? "API hedefi bulunamadı — oyun kurulumu olmayabilir."
                    : result.ExeCount == 0 ? "Çalıştırılabilir dosya bulunamadı."
                    : "Klasör çok boş görünüyor — yanlış klasör olabilir.";
                return result;
            }
            catch (Exception ex)
            {
                result.Reason = "Klasör taranamadı: " + ex.Message;
                return result;
            }
        }
    }

    /// <summary>Geri yükleme öncesi yedek sağlamlığı denetimi.</summary>
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
                var info = new FileInfo(entry.BackupPath);
                if (info.Length == 0)
                {
                    result.Problems.Add($"Yedek boş: {entry.BackupPath}");
                }
            }

            result.Valid = result.Problems.Count == 0;
            return result;
        }

        /// <summary>
        /// Yamanın hâlâ yerinde olup olmadığını denetler. Steam güncellemesi veya
        /// "dosya bütünlüğü doğrula" işlemi orijinali geri getirdiyse true döner.
        /// </summary>
        public static bool IsPatchStale(PatchManifest manifest)
        {
            foreach (PatchEntry entry in manifest.Entries)
            {
                if (!File.Exists(entry.DllPath) || !File.Exists(entry.BackupPath))
                {
                    return true;
                }

                try
                {
                    var patched = new FileInfo(entry.DllPath);
                    var backup = new FileInfo(entry.BackupPath);
                    // Güncelleme sonrası dll yedeğin kopyasıysa (boyut eşit) yama gitmiş demektir.
                    if (patched.Length == backup.Length)
                    {
                        return true;
                    }
                }
                catch
                {
                    return true;
                }
            }
            return false;
        }
    }
}
