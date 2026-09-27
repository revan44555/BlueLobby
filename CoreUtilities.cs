using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BlueLobby
{
    public static class Core
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

        public static List<ApiDllTarget> FindApiDllTargets(string gameDir)
        {
            var targets = new List<ApiDllTarget>();
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                MatchCasing = MatchCasing.CaseInsensitive
            };

            foreach (string path in Directory.GetFiles(gameDir, "steam_api64.dll", options))
            {
                targets.Add(new ApiDllTarget { Path = path, Is64Bit = true });
            }

            foreach (string path in Directory.GetFiles(gameDir, "steam_api.dll", options))
            {
                targets.Add(new ApiDllTarget { Path = path, Is64Bit = false });
            }

            return targets.OrderBy(t => t.Path, StringComparer.OrdinalIgnoreCase).ToList();
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
