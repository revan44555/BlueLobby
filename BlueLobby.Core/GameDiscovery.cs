using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BlueLobby.Platform;

namespace BlueLobby.Core
{
    public enum GameLibrarySource
    {
        Steam,
        Heroic,
        Lutris,
        GOG,
        Filesystem,
    }

    public sealed record DiscoveredGame
    {
        public int? AppId { get; init; }
        public string Name { get; init; } = string.Empty;
        public string InstallPath { get; init; } = string.Empty;
        public string? ExecutablePath { get; init; }
        public GameLibrarySource Source { get; init; }
        public string? LauncherId { get; init; }

        public override string ToString() => string.IsNullOrWhiteSpace(Name) ? InstallPath : Name;
    }

    /// <summary>
    /// Launcher-aware game discovery. Discovery never mutates a game directory and never
    /// recursively scans an entire drive. Only known launcher metadata and bounded roots are read.
    /// </summary>
    public sealed class GameDiscoveryService
    {
        private readonly IPlatformServices _platform;

        public GameDiscoveryService(IPlatformServices platform)
        {
            _platform = platform;
        }

        public Task<IReadOnlyList<DiscoveredGame>> DiscoverAsync(CancellationToken ct = default)
        {
            return Task.Run(() => Discover(ct), ct);
        }

        private IReadOnlyList<DiscoveredGame> Discover(CancellationToken ct)
        {
            var results = new List<DiscoveredGame>();
            var seen = new HashSet<string>(PathComparer);

            foreach (string root in GetSteamRoots())
            {
                ct.ThrowIfCancellationRequested();
                foreach (DiscoveredGame game in DiscoverSteam(root, ct)) Add(results, seen, game);
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
            {
                ct.ThrowIfCancellationRequested();
                foreach (HeroicGame game in GameLibrary.FindHeroicGames(home))
                {
                    Add(results, seen, new DiscoveredGame
                    {
                        Name = game.Title,
                        InstallPath = game.InstallPath,
                        Source = GameLibrarySource.Heroic,
                        LauncherId = game.AppName,
                    });
                }

                foreach (DiscoveredGame game in DiscoverLutris(home, ct)) Add(results, seen, game);
            }

            foreach (string root in GetGogRoots())
            {
                ct.ThrowIfCancellationRequested();
                foreach (DiscoveredGame game in DiscoverGogRoot(root, ct)) Add(results, seen, game);
            }

            return results
                .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .ThenBy(g => g.InstallPath, PathComparer)
                .ToList();
        }

        private IEnumerable<string> GetSteamRoots()
        {
            var roots = new List<string>();
            foreach (string root in _platform.Paths.GetSteamRoots())
            {
                AddUnique(roots, NormalizeSteamRoot(root));
            }

            // Geriye dönük platform sağlayıcıları için eski kök sözleşmesi de korunur.
            foreach (string root in _platform.Paths.GetGameSearchRoots())
            {
                AddUnique(roots, NormalizeSteamRoot(root));
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
            {
                AddUnique(roots, Path.Combine(home, ".steam", "steam"));
                AddUnique(roots, Path.Combine(home, ".steam", "root"));
                AddUnique(roots, Path.Combine(home, ".local", "share", "Steam"));
                string? xdgData = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
                if (!string.IsNullOrWhiteSpace(xdgData)) AddUnique(roots, Path.Combine(xdgData, "Steam"));
                AddUnique(roots, Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"));
            }

            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            AddUnique(roots, Path.Combine(programFilesX86, "Steam"));
            AddUnique(roots, Path.Combine(programFiles, "Steam"));
            return roots.Where(Directory.Exists);
        }

        private IEnumerable<string> GetGogRoots()
        {
            var roots = new List<string>();
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
            {
                AddUnique(roots, Path.Combine(home, "GOG Games"));
            }
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            AddUnique(roots, Path.Combine(programFiles, "GOG Galaxy", "Games"));
            AddUnique(roots, Path.Combine(programFilesX86, "GOG Galaxy", "Games"));
            return roots.Where(Directory.Exists);
        }

        private IEnumerable<DiscoveredGame> DiscoverSteam(string steamRoot, CancellationToken ct)
        {
            string steamApps = Path.Combine(steamRoot, "steamapps");
            string libraryFile = Path.Combine(steamApps, "libraryfolders.vdf");
            var libraries = new List<string>();
            if (Directory.Exists(steamApps)) libraries.Add(steamRoot);

            if (File.Exists(libraryFile))
            {
                try
                {
                    if (new FileInfo(libraryFile).Length <= 2 * 1024 * 1024)
                    {
                        foreach (string path in ParseVdfValues(File.ReadAllText(libraryFile), "path"))
                        {
                            string libraryRoot = NormalizeSteamRoot(path);
                            if (Directory.Exists(libraryRoot)) libraries.Add(libraryRoot);
                        }
                    }
                }
                catch
                {
                    // Bozuk/erişilemeyen Steam metadata'sı diğer launcherlardan keşfi engellemez.
                }
            }

            foreach (string library in libraries.Distinct(PathComparer))
            {
                ct.ThrowIfCancellationRequested();
                string apps = Path.Combine(library, "steamapps");
                if (!Directory.Exists(apps)) continue;
                IEnumerable<string> manifests;
                try { manifests = Directory.EnumerateFiles(apps, "appmanifest_*.acf", new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true }); }
                catch { continue; }

                foreach (string manifest in manifests)
                {
                    ct.ThrowIfCancellationRequested();
                    string text;
                    try
                    {
                        if (new FileInfo(manifest).Length > 2 * 1024 * 1024) continue;
                        text = File.ReadAllText(manifest);
                    }
                    catch { continue; }
                    string? appIdText = FirstVdfValue(text, "appid");
                    string? name = FirstVdfValue(text, "name");
                    string? installDir = FirstVdfValue(text, "installdir");
                    if (!int.TryParse(appIdText, out int appId) || string.IsNullOrWhiteSpace(installDir)) continue;
                    string commonRoot = Path.Combine(apps, "common");
                    string path = Path.Combine(commonRoot, installDir);
                    if (!Directory.Exists(path)) continue;
                    try
                    {
                        if (!PathSafety.IsInside(commonRoot, path)) continue;
                    }
                    catch
                    {
                        continue;
                    }
                    yield return new DiscoveredGame
                    {
                        AppId = appId,
                        Name = string.IsNullOrWhiteSpace(name) ? installDir : name,
                        InstallPath = path,
                        Source = GameLibrarySource.Steam,
                        LauncherId = appId.ToString(),
                        ExecutablePath = FindLikelyExecutable(path),
                    };
                }
            }
        }

        private static IEnumerable<DiscoveredGame> DiscoverLutris(string home, CancellationToken ct)
        {
            var candidateList = new List<string>
            {
                Path.Combine(home, ".config", "lutris", "games"),
                Path.Combine(home, ".local", "share", "lutris", "games"),
                Path.Combine(home, ".var", "app", "net.lutris.Lutris", "config", "lutris", "games"),
                Path.Combine(home, ".var", "app", "net.lutris.Lutris", "data", "lutris", "games"),
            };

            string xdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(xdgConfig))
                candidateList.Add(Path.Combine(xdgConfig, "lutris", "games"));

            foreach (string dir in candidateList.Distinct(StringComparer.Ordinal))
            {
                if (!Directory.Exists(dir)) continue;
                IEnumerable<string> files;
                try { files = Directory.EnumerateFiles(dir, "*.yml", new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true }); }
                catch { continue; }
                foreach (string file in files)
                {
                    ct.ThrowIfCancellationRequested();
                    string text;
                    try
                    {
                        if (new FileInfo(file).Length > 2 * 1024 * 1024) continue;
                        text = File.ReadAllText(file);
                    }
                    catch { continue; }

                    string? name = FirstYamlScalar(text, "name");
                    string? directory = FirstYamlScalar(text, "directory");
                    string? workingDir = FirstYamlScalar(text, "working_dir");
                    string? exe = FirstYamlScalarInSection(text, "game", "exe");

                    string? installPath = ResolveLutrisInstallPath(home, directory, workingDir, exe);
                    if (string.IsNullOrWhiteSpace(installPath) || !Directory.Exists(installPath)) continue;
                    yield return new DiscoveredGame
                    {
                        Name = string.IsNullOrWhiteSpace(name) ? Path.GetFileNameWithoutExtension(file) : name,
                        InstallPath = installPath,
                        Source = GameLibrarySource.Lutris,
                        LauncherId = Path.GetFileNameWithoutExtension(file),
                        ExecutablePath = ResolveLutrisExecutable(home, directory, workingDir, exe),
                    };
                }
            }
        }

        private static string? ResolveLutrisInstallPath(string home, string? directory, string? workingDir, string? exe)
        {
            string? explicitDirectory = ExpandLauncherPath(home, directory);
            if (!string.IsNullOrWhiteSpace(explicitDirectory) && Directory.Exists(explicitDirectory))
                return PathSafety.Canonicalize(explicitDirectory);

            string? explicitWorkingDir = ExpandLauncherPath(home, workingDir);
            if (!string.IsNullOrWhiteSpace(explicitWorkingDir) && Directory.Exists(explicitWorkingDir))
                return PathSafety.Canonicalize(explicitWorkingDir);

            string? executable = ExpandLauncherPath(home, exe, explicitDirectory ?? explicitWorkingDir);
            if (!string.IsNullOrWhiteSpace(executable) && File.Exists(executable))
                return Path.GetDirectoryName(PathSafety.Canonicalize(executable));

            return null;
        }

        private static string? ExpandLauncherPath(string home, string? value, string? gameDir = null)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            string path = value.Trim().Trim('"', '\'');
            if (path.StartsWith("$GAMEDIR", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(gameDir)) return null;
                path = Path.Combine(gameDir, path.Substring("$GAMEDIR".Length).TrimStart('/', '\\'));
            }
            else if (path.StartsWith("~/", StringComparison.Ordinal))
            {
                path = Path.Combine(home, path.Substring(2));
            }
            if (!Path.IsPathRooted(path)) return null;
            try { return PathSafety.Canonicalize(path); } catch { return null; }
        }

        private static IEnumerable<DiscoveredGame> DiscoverGogRoot(string root, CancellationToken ct)
        {
            IEnumerable<string> dirs;
            try { dirs = Directory.EnumerateDirectories(root, "*", new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true }); }
            catch { yield break; }
            foreach (string dir in dirs)
            {
                ct.ThrowIfCancellationRequested();
                if (!LooksLikeGameDirectory(dir)) continue;
                yield return new DiscoveredGame
                {
                    Name = Path.GetFileName(dir),
                    InstallPath = dir,
                    Source = GameLibrarySource.GOG,
                    ExecutablePath = FindLikelyExecutable(dir),
                };
            }
        }

        private static string? ResolveLutrisExecutable(string home, string? directory, string? workingDir, string? exe)
        {
            string? baseDir = ExpandLauncherPath(home, directory) ?? ExpandLauncherPath(home, workingDir);
            string? resolved = ExpandLauncherPath(home, exe, baseDir);
            return !string.IsNullOrWhiteSpace(resolved) && File.Exists(resolved) ? resolved : FindLikelyExecutable(baseDir ?? string.Empty);
        }

        private static string? FindLikelyExecutable(string installPath)
        {
            if (string.IsNullOrWhiteSpace(installPath) || !Directory.Exists(installPath)) return null;
            try
            {
                foreach (string file in Directory.EnumerateFiles(installPath, "*", new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true }))
                {
                    string ext = Path.GetExtension(file);
                    if (ext.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                        ext.Equals(".x86_64", StringComparison.OrdinalIgnoreCase) ||
                        ext.Equals(".x86", StringComparison.OrdinalIgnoreCase) ||
                        ext.Equals(".bin", StringComparison.OrdinalIgnoreCase))
                        return file;
                }
            }
            catch { }
            return null;
        }

        private static bool LooksLikeGameDirectory(string dir)
        {
            try
            {
                foreach (string file in Directory.EnumerateFiles(dir, "*", new EnumerationOptions { RecurseSubdirectories = false, IgnoreInaccessible = true }))
                {
                    string ext = Path.GetExtension(file);
                    if (ext.Equals(".exe", StringComparison.OrdinalIgnoreCase) || ext.Equals(".x86_64", StringComparison.OrdinalIgnoreCase) || ext.Equals(".x86", StringComparison.OrdinalIgnoreCase)) return true;
                }
            }
            catch { }
            return false;
        }

        private static string? FirstVdfValue(string text, string key)
        {
            return SteamVdf.ValuesForKey(text, key).FirstOrDefault();
        }

        private static IEnumerable<string> ParseVdfValues(string text, string key)
        {
            return SteamVdf.ValuesForKey(text, key);
        }

        private static string? FirstYamlScalar(string text, string key)
        {
            Match match = Regex.Match(text, "(?m)^\\s*" + Regex.Escape(key) + "\\s*:\\s*[\\\"]?([^\\\"\\r\\n]+?)[\\\"]?\\s*$", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : null;
        }

        private static string? FirstYamlScalarInSection(string text, string section, string key)
        {
            Match sectionMatch = Regex.Match(text, "(?ms)^\\s*" + Regex.Escape(section) + "\\s*:\\s*\\n(?<body>(?:^[ \t]+.*\\n?)+)", RegexOptions.IgnoreCase);
            if (!sectionMatch.Success) return null;
            return FirstYamlScalar(sectionMatch.Groups["body"].Value, key);
        }

        private static void Add(List<DiscoveredGame> results, HashSet<string> seen, DiscoveredGame game)
        {
            if (string.IsNullOrWhiteSpace(game.InstallPath) || !Directory.Exists(game.InstallPath)) return;
            string path;
            try { path = PathSafety.Canonicalize(game.InstallPath); } catch { return; }
            if (!seen.Add(path)) return;
            results.Add(game with { InstallPath = path });
        }

        private static void AddUnique(List<string> list, string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return;
            try { value = PathSafety.Canonicalize(value); } catch { return; }
            if (!list.Any(x => PathComparer.Equals(x, value))) list.Add(value);
        }


        private static string NormalizeSteamRoot(string candidate)
        {
            string root;
            try { root = PathSafety.Canonicalize(candidate); } catch { return string.Empty; }
            if (string.IsNullOrWhiteSpace(root)) return string.Empty;

            string name = Path.GetFileName(root);
            if (name.Equals("common", StringComparison.OrdinalIgnoreCase))
            {
                string? steamApps = Path.GetDirectoryName(root);
                if (!string.IsNullOrWhiteSpace(steamApps) && Path.GetFileName(steamApps).Equals("steamapps", StringComparison.OrdinalIgnoreCase))
                {
                    string? steamRoot = Path.GetDirectoryName(steamApps);
                    if (!string.IsNullOrWhiteSpace(steamRoot)) return PathSafety.Canonicalize(steamRoot);
                }
            }
            else if (name.Equals("steamapps", StringComparison.OrdinalIgnoreCase))
            {
                string? steamRoot = Path.GetDirectoryName(root);
                if (!string.IsNullOrWhiteSpace(steamRoot)) return PathSafety.Canonicalize(steamRoot);
            }

            return root;
        }

        private static StringComparer PathComparer => OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
    }
}
