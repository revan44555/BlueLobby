using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlueLobby.Core
{
    public static class GameScanner
    {
        public static string ComputeKey(string value)
        {
            byte[] hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(value.ToLowerInvariant()));
            return Convert.ToHexString(hash).Substring(0, 16).ToLowerInvariant();
        }

        public static void AddUnique(List<string> list, string value)
        {
            if (!list.Any(item => string.Equals(item, value, StringComparison.OrdinalIgnoreCase)))
            {
                list.Add(value);
            }
        }

        // Windows oyunları DLL, Linux oyunları .so kullanır. Case-sensitivity:
        // Linux dosya sisteminde MatchCasing dikkate alınmadığı için manuel eşleşme yapılır.
        private static readonly string[] DllTargetNames = { "steam_api64.dll", "steam_api.dll" };
        private static readonly string[] SoTargetNames = { "steam_api64.so", "libsteam_api.so", "steam_api.so" };

        public static List<ApiDllTarget> FindApiDllTargets(string gameDir)
        {
            var targets = new List<ApiDllTarget>();
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true
            };

            foreach (string path in Directory.EnumerateFiles(gameDir, "*", options))
            {
                string fileName = Path.GetFileName(path);
                bool is64;
                bool isSo;

                if (MatchesAny(fileName, DllTargetNames)) { isSo = false; is64 = fileName.Equals("steam_api64.dll", StringComparison.OrdinalIgnoreCase); }
                else if (MatchesAny(fileName, SoTargetNames)) { isSo = true; is64 = !fileName.StartsWith("steam_api.", StringComparison.OrdinalIgnoreCase); }
                else { continue; }

                targets.Add(new ApiDllTarget { Path = path, Is64Bit = is64, IsSharedObject = isSo });
            }

            return targets.OrderBy(t => t.Path, StringComparer.OrdinalIgnoreCase).ToList();
        }

        private static bool MatchesAny(string fileName, string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                if (fileName.Equals(candidate, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Hedef API dosyası için çalışma zamanı bileşen dosyasının adı.
        /// Windows: steam_api64.dll / steam_api.dll; Linux: steam_api64.so / libsteam_api.so
        /// </summary>
        public static string GetComponentFileName(ApiDllTarget target)
        {
            if (target.IsSharedObject)
            {
                return target.Is64Bit ? "steam_api64.so" : "libsteam_api.so";
            }
            return target.Is64Bit ? "steam_api64.dll" : "steam_api.dll";
        }
    }

    public static class ErrorDoctor
    {
        public static string Explain(Exception ex)
        {
            return ex switch
            {
                UnauthorizedAccessException =>
                    "İzin yetmiyor. Programı sağ tık → 'Yönetici olarak çalıştır' ile açmayı deneyin.",
                DirectoryNotFoundException =>
                    "Seçilen klasör artık bulunamıyor. Oyun klasörünü yeniden seçin.",
                PathTooLongException =>
                    "Dosya yollarından biri çok uzun. Oyunu daha kısa bir klasöre kurmayı düşünün.",
                IOException io when IsFileLock(io) =>
                    "Bir dosya kullanılıyor olabilir. Oyunun ve ilgili platformun tamamen kapalı olduğundan emin olun.",
                IOException io =>
                    $"Dosya işlemi tamamlanamadı: {io.Message}",
                _ => ex.Message
            };
        }

        public static bool IsFileLock(IOException io)
        {
            const int ERROR_SHARING_VIOLATION = unchecked((int)0x80070020);
            const int ERROR_LOCK_VIOLATION = unchecked((int)0x80070021);
            return io.HResult == ERROR_SHARING_VIOLATION || io.HResult == ERROR_LOCK_VIOLATION;
        }
    }
}
