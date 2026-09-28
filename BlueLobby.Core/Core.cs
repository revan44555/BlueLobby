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
            string normalized = PathSafety.Canonicalize(value);
            if (OperatingSystem.IsWindows())
            {
                // Windows yolu case-insensitive olduğu için aynı klasörün farklı
                // casing ile seçilmesi aynı manifest/lock anahtarına gitmelidir.
                normalized = normalized.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar).ToUpperInvariant();
            }
            else
            {
                normalized = normalized.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }
            byte[] hash = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(hash).Substring(0, 16).ToLowerInvariant();
        }

        public static void AddUnique(List<string> list, string value)
        {
            if (!list.Any(item => string.Equals(item, value, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)))
            {
                list.Add(value);
            }
        }

        private static readonly string[] DllTargetNames = { "steam_api64.dll", "steam_api.dll" };
        private static readonly string[] SoTargetNames = { "steam_api64.so", "libsteam_api.so", "steam_api.so" };

        /// <summary>
        /// Yalnızca seçilen çalıştırıcının bulunduğu klasördeki runtime API dosyalarını hedefler.
        /// Hiç hedef bulunamazsa oyun kökünü ikinci ve sınırlı aday olarak dener; recursive tarama yapmaz.
        /// </summary>
        public static List<ApiDllTarget> FindApiDllTargets(string gameDir, string? exePath = null)
        {
            string root = PathSafety.Canonicalize(gameDir);
            if (!Directory.Exists(root)) return new List<ApiDllTarget>();

            BinaryArchitecture executableArchitecture = BinaryArchitecture.Unknown;
            string? exeDir = null;
            if (!string.IsNullOrWhiteSpace(exePath))
            {
                string exe = PathSafety.Canonicalize(exePath);
                if (File.Exists(exe) && PathSafety.IsInside(root, exe))
                {
                    executableArchitecture = BinaryArchitectureDetector.Detect(exe);
                    exeDir = Path.GetDirectoryName(exe);
                }
            }

            // Eski recursive “her DLL'i değiştir” davranışına dönmeden, seçili oyun
            // altında en fazla iki seviye ve yalnızca makul runtime klasörlerini tarıyoruz.
            // Öncelik executable klasörü > oyun kökü > bilinen runtime alt klasörleri.
            var candidates = BuildCandidateDirectories(root, exeDir);
            CandidateResult? best = null;
            foreach ((string dir, int score) in candidates)
            {
                var dirTargets = new List<ApiDllTarget>();
                foreach (string path in EnumerateDirectApiFiles(dir))
                {
                    BinaryArchitecture architecture = BinaryArchitectureDetector.Detect(path);
                    if (architecture == BinaryArchitecture.Unknown) continue;
                    if (executableArchitecture != BinaryArchitecture.Unknown && architecture != executableArchitecture) continue;

                    string fileName = Path.GetFileName(path);
                    dirTargets.Add(new ApiDllTarget
                    {
                        Path = path,
                        Is64Bit = architecture == BinaryArchitecture.X64 || architecture == BinaryArchitecture.Arm64,
                        IsSharedObject = IsSharedObjectName(fileName),
                        Architecture = architecture,
                    });
                }

                if (dirTargets.Count == 0) continue;
                dirTargets = dirTargets
                    .GroupBy(t => PathSafety.Canonicalize(t.Path), OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                    .Select(g => g.First())
                    .ToList();

                if (best == null || score > best.Score)
                    best = new CandidateResult(dir, score, dirTargets);
            }

            return best?.Targets
                .OrderBy(t => t.Architecture)
                .ThenBy(t => t.Path, OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .ToList() ?? new List<ApiDllTarget>();
        }

        private static IReadOnlyList<(string Dir, int Score)> BuildCandidateDirectories(string root, string? exeDir)
        {
            var result = new List<(string Dir, int Score)>();
            var seen = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);

            void Add(string dir, int score)
            {
                if (!Directory.Exists(dir)) return;
                string canonical;
                try { canonical = PathSafety.Canonicalize(dir); } catch { return; }
                if (seen.Add(canonical)) result.Add((canonical, score));
            }

            Add(exeDir ?? string.Empty, 1000);
            Add(root, 900);

            foreach (string baseDir in new[] { exeDir, root }.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                foreach (string child in EnumerateCandidateSubdirectories(baseDir!, 2, 80))
                {
                    string leaf = Path.GetFileName(child);
                    int score = 500;
                    if (IsPreferredRuntimeSegment(leaf)) score += 120;
                    if (IsExcludedRuntimeSegment(child)) score -= 300;
                    if (string.Equals(baseDir, exeDir, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal)) score += 80;
                    Add(child, score);
                }
            }

            return result
                .OrderByDescending(x => x.Score)
                .ThenBy(x => x.Dir, OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
                .ToList();
        }

        private static IEnumerable<string> EnumerateCandidateSubdirectories(string root, int maxDepth, int maxDirectories)
        {
            var queue = new Queue<(string Dir, int Depth)>();
            var seen = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
            queue.Enqueue((root, 0));
            seen.Add(PathSafety.Canonicalize(root));
            int yielded = 0;

            while (queue.Count > 0 && yielded < maxDirectories)
            {
                (string dir, int depth) = queue.Dequeue();
                if (depth >= maxDepth) continue;

                IEnumerable<string> children;
                try
                {
                    children = Directory.EnumerateDirectories(dir, "*", new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true });
                }
                catch
                {
                    continue;
                }

                foreach (string child in children.OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
                {
                    if (IsExcludedRuntimeSegment(child)) continue;
                    string canonical;
                    try { canonical = PathSafety.Canonicalize(child); } catch { continue; }
                    if (!seen.Add(canonical)) continue;
                    yield return canonical;
                    yielded++;
                    if (yielded >= maxDirectories) yield break;
                    queue.Enqueue((canonical, depth + 1));
                }
            }
        }

        private static bool IsPreferredRuntimeSegment(string name)
        {
            return name.Equals("Binaries", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("x64", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("x86", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Win64", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("Win32", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("plugins", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("lib", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsExcludedRuntimeSegment(string path)
        {
            string name = Path.GetFileName(path);
            return name.Equals("redist", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("redistributable", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("sdk", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("tools", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("editor", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("docs", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("documentation", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("samples", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("test", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("tests", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("backup", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("backups", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("installer", StringComparison.OrdinalIgnoreCase) ||
                   name.Equals("installers", StringComparison.OrdinalIgnoreCase);
        }

        private sealed record CandidateResult(string Dir, int Score, List<ApiDllTarget> Targets);

        private static IEnumerable<string> EnumerateDirectApiFiles(string dir)
        {
            if (!Directory.Exists(dir)) yield break;
            IEnumerable<string> files;
            try
            {
                files = Directory.EnumerateFiles(dir, "*", new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true });
            }
            catch
            {
                yield break;
            }

            foreach (string path in files)
            {
                string fileName = Path.GetFileName(path);
                if (fileName.Contains(".bluelobby.", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) || fileName.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (MatchesAny(fileName, DllTargetNames) || MatchesAny(fileName, SoTargetNames))
                    yield return path;
            }
        }

        private static bool IsSharedObjectName(string fileName)
        {
            return MatchesAny(fileName, SoTargetNames);
        }

        private static bool MatchesAny(string fileName, string[] candidates)
        {
            foreach (string candidate in candidates)
            {
                if (fileName.Equals(candidate, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        public static string GetComponentFileName(ApiDllTarget target)
        {
            if (target.IsSharedObject)
            {
                string originalName = Path.GetFileName(target.Path);
                if (originalName.Equals("libsteam_api.so", StringComparison.OrdinalIgnoreCase) ||
                    originalName.Equals("steam_api.so", StringComparison.OrdinalIgnoreCase) ||
                    originalName.Equals("steam_api64.so", StringComparison.OrdinalIgnoreCase))
                {
                    return originalName;
                }
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
                    "İzin yetmiyor. Dosya erişim izinlerini kontrol edin veya Windows'ta gerekirse yönetici olarak yeniden deneyin.",
                DirectoryNotFoundException =>
                    "Seçilen klasör artık bulunamıyor. Oyun klasörünü yeniden seçin.",
                PathTooLongException =>
                    "Dosya yollarından biri çok uzun. Oyunu daha kısa bir klasöre kurmayı düşünün.",
                InvalidDataException => ex.Message,
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
