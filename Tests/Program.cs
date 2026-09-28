using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BlueLobby.Core;
using BlueLobby.Platform;

namespace BlueLobby.Tests
{
    internal sealed class FakePaths : IPlatformPaths
    {
        private readonly string _root;
        private readonly string _searchRoot;
        public FakePaths(string root, string? searchRoot = null)
        {
            _root = root;
            _searchRoot = searchRoot ?? Path.Combine(_root, "games");
        }
        public string RoamingDataDir => Path.Combine(_root, "config");
        public string LocalDataDir => Path.Combine(_root, "local");
        public IReadOnlyList<string> GetGameSearchRoots() => new[] { _searchRoot };
        public IReadOnlyList<string> GetSteamRoots() => new[] { _searchRoot };
    }

    internal sealed class FakePlatform : IPlatformServices
    {
        public bool SupportsFirewall { get; init; }
        public bool IsElevated { get; init; } = true;
        public FakePaths PathImpl { get; }
        public int FirewallCreateCount { get; private set; }
        public int FirewallDeleteCount { get; private set; }

        public FakePlatform(string root, string? searchRoot = null) => PathImpl = new FakePaths(root, searchRoot);
        public IPlatformPaths Paths => PathImpl;
        public string VpnDownloadUrl => string.Empty;
        public Task<bool> IsGameRunningAsync(string exePath) => Task.FromResult(false);
        public Task<bool> IsVpnActiveAsync() => Task.FromResult(false);
        public string? GetVpnIpv4() => null;
        public IReadOnlyList<string> GetVpnInterfaceNames() => Array.Empty<string>();
        public Task LaunchGameAsync(string exePath, string gameDir, int? steamAppId = null) => Task.CompletedTask;
        public Task<bool> EnsureFirewallRuleAsync(string exePath, string ruleName)
        {
            FirewallCreateCount++;
            return Task.FromResult(true);
        }
        public Task<bool> RemoveFirewallRuleAsync(string ruleName)
        {
            FirewallDeleteCount++;
            return Task.FromResult(true);
        }
    }

    internal static class Program
    {
        private static int _passed;
        private static int _failed;

        private static void Check(string name, bool ok)
        {
            if (ok) { _passed++; Console.WriteLine($"PASS: {name}"); }
            else { _failed++; Console.WriteLine($"FAIL: {name}"); }
        }

        public static async Task<int> Main()
        {
            string temp = Path.Combine(Path.GetTempPath(), "BlueLobbyTests-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(temp);

            try
            {
                TestPathSafety(temp);
                TestBinaryArchitecture(temp);
                TestGameScanner(temp);
                TestGameDiscovery(temp);
                TestManifestStore(temp);
                TestProfilesAndTokens(temp);
                await TestPatchTransactionAsync(temp);
                await TestInterruptedTransactionRecoveryAsync(temp);
                await TestWrongArchitectureDoesNotMutateAsync(temp);
                await TestLinuxSteamLaunchAsync(temp);
                TestCompatDatabase();
            }
            catch (Exception ex)
            {
                _failed++;
                Console.WriteLine("UNHANDLED: " + ex);
            }
            finally
            {
                try { Directory.Delete(temp, recursive: true); } catch { }
            }

            Console.WriteLine($"{_passed}/{_passed + _failed} PASS");
            return _failed == 0 ? 0 : 1;
        }

        private static void TestPathSafety(string temp)
        {
            string root = Path.Combine(temp, "Game");
            Directory.CreateDirectory(root);
            string inside = Path.Combine(root, "bin", "game.exe");
            string outside = Path.Combine(temp, "Outside", "evil.dll");
            Check("PathSafety root korunur", PathSafety.Canonicalize(Path.GetPathRoot(root)!) == Path.GetPathRoot(root));
            Check("PathSafety inside true", PathSafety.IsInside(root, inside));
            Check("PathSafety outside false", !PathSafety.IsInside(root, outside));
            Check("Manifest key canonicalizasyonu", GameScanner.ComputeKey(root) == GameScanner.ComputeKey(Path.Combine(root, ".")));
        }

        private static void TestBinaryArchitecture(string temp)
        {
            string x64 = Path.Combine(temp, "x64.bin");
            string x86 = Path.Combine(temp, "x86.bin");
            string elf64 = Path.Combine(temp, "elf64.so");
            File.WriteAllBytes(x64, BuildPe(0x8664));
            File.WriteAllBytes(x86, BuildPe(0x014c));
            File.WriteAllBytes(elf64, BuildElf(0x3E));
            Check("PE x64 tespiti", BinaryArchitectureDetector.Detect(x64) == BinaryArchitecture.X64);
            Check("PE x86 tespiti", BinaryArchitectureDetector.Detect(x86) == BinaryArchitecture.X86);
            Check("ELF x64 tespiti", BinaryArchitectureDetector.Detect(elf64) == BinaryArchitecture.X64);
        }

        private static void TestGameScanner(string temp)
        {
            string root = Path.Combine(temp, "ScanGame");
            Directory.CreateDirectory(root);
            string exe = Path.Combine(root, "Game.exe");
            string dll = Path.Combine(root, "steam_api64.dll");
            string wrongArch = Path.Combine(root, "steam_api.dll");
            string nested = Path.Combine(root, "redist", "steam_api64.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(nested)!);
            File.WriteAllBytes(exe, BuildPe(0x8664));
            File.WriteAllBytes(dll, BuildPe(0x8664));
            File.WriteAllBytes(wrongArch, BuildPe(0x014c));
            File.WriteAllBytes(nested, BuildPe(0x8664));

            var targets = GameScanner.FindApiDllTargets(root, exe);
            Check("Scanner yalnızca runtime klasörünü seçer", targets.Count == 1);
            Check("Scanner executable mimarisiyle eşleştirir", targets.Count == 1 && targets[0].Architecture == BinaryArchitecture.X64);
            Check("Component adı hedef adıyla uyumlu", GameScanner.GetComponentFileName(targets[0]) == "steam_api64.dll");

            string nestedRoot = Path.Combine(temp, "NestedScanGame");
            string nestedExe = Path.Combine(nestedRoot, "Game.exe");
            string nestedDll = Path.Combine(nestedRoot, "Binaries", "Win64", "steam_api64.dll");
            Directory.CreateDirectory(Path.GetDirectoryName(nestedDll)!);
            File.WriteAllBytes(nestedExe, BuildPe(0x8664));
            File.WriteAllBytes(nestedDll, BuildPe(0x8664, 4));
            var nestedTargets = GameScanner.FindApiDllTargets(nestedRoot, nestedExe);
            Check("Scanner sınırlı runtime alt dizinlerini destekler", nestedTargets.Count == 1 && nestedTargets[0].Path == nestedDll);
        }

        private static void TestGameDiscovery(string temp)
        {
            string steam = Path.Combine(temp, "Steam");
            string apps = Path.Combine(steam, "steamapps");
            string common = Path.Combine(apps, "common");
            string game = Path.Combine(common, "Example Game");
            Directory.CreateDirectory(game);
            File.WriteAllText(Path.Combine(apps, "libraryfolders.vdf"), "\"libraryfolders\" { \"0\" { \"path\" \"" + steam.Replace("\\", "\\\\") + "\" } }");
            File.WriteAllText(Path.Combine(apps, "appmanifest_480.acf"), "\"AppState\" { \"appid\" \"480\" \"name\" \"Example \\\"Game\" \"installdir\" \"Example Game\" }");

            // Gerçek Linux PlatformPaths common klasörünü verdiği için discovery bunu Steam köküne normalize edebilmelidir.
            var platform = new FakePlatform(temp, common);
            var discovery = new GameDiscoveryService(platform);
            IReadOnlyList<DiscoveredGame> games = discovery.DiscoverAsync().GetAwaiter().GetResult();
            Check("Steam discovery metadata kullanır", games.Any(g => g.AppId == 480 && g.Source == GameLibrarySource.Steam));
            Check("Discovery oyun klasörünü doğrular", games.All(g => Directory.Exists(g.InstallPath)));
            Check("Steam common kökü doğru normalize edilir", games.Any(g => Path.GetFullPath(g.InstallPath) == Path.GetFullPath(game)));
            Check("Steam VDF escaped value çözülür", games.Any(g => g.Name == "Example \"Game"));

            string xdgConfig = Path.Combine(temp, ".config");
            string heroicConfig = Path.Combine(xdgConfig, "legendary");
            Directory.CreateDirectory(heroicConfig);
            string heroicGame = Path.Combine(temp, "Heroic Game");
            Directory.CreateDirectory(heroicGame);
            File.WriteAllText(Path.Combine(heroicConfig, "installed.json"), "{\"heroic.app\":{\"title\":\"Heroic Game\",\"install_path\":\"" + heroicGame.Replace("\\", "\\\\") + "\"}}");

            string lutrisDir = Path.Combine(xdgConfig, "lutris", "games");
            Directory.CreateDirectory(lutrisDir);
            string lutrisGame = Path.Combine(temp, "Lutris Game");
            Directory.CreateDirectory(lutrisGame);
            string lutrisExe = Path.Combine(lutrisGame, "game.exe");
            File.WriteAllText(lutrisExe, "placeholder");
            File.WriteAllText(Path.Combine(lutrisDir, "lutris-game.yml"), "name: Lutris Game\ngame:\n  exe: \"$GAMEDIR/game.exe\"\n  working_dir: $GAMEDIR\n  directory: \"" + lutrisGame.Replace("\\", "\\\\") + "\"\n");

            string? previousXdgConfig = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");
            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", xdgConfig);

            IReadOnlyList<DiscoveredGame> launcherGames = new GameDiscoveryService(new FakePlatform(temp, common)).DiscoverAsync().GetAwaiter().GetResult();
            Check("Heroic installed.json keşfi", launcherGames.Any(g => g.Source == GameLibrarySource.Heroic && g.Name == "Heroic Game"));
            Check("Lutris nested game.exe keşfi", launcherGames.Any(g => g.Source == GameLibrarySource.Lutris && g.Name == "Lutris Game"));
            Check("Lutris executable metadata çözülür", launcherGames.Any(g => g.Source == GameLibrarySource.Lutris && g.ExecutablePath == lutrisExe));
            Environment.SetEnvironmentVariable("XDG_CONFIG_HOME", previousXdgConfig);
        }

        private static void TestManifestStore(string temp)
        {
            string data = Path.Combine(temp, "Store");
            Directory.CreateDirectory(data);
            var store = new PatchManifestStore(
                Path.Combine(data, "config", "settings.json"),
                Path.Combine(data, "local", "manifests"),
                Path.Combine(data, "local", "app.log"));
            string game = Path.Combine(temp, "ManifestGame");
            Directory.CreateDirectory(game);
            var manifest = new PatchManifest { SchemaVersion = 2, GameDir = game, State = PatchTransactionState.Committed };
            store.SaveManifest(manifest);
            ManifestReadResult read = store.ReadManifest(game);
            Check("Manifest atomic save/read", read.Valid && read.Manifest?.State == PatchTransactionState.Committed);
            File.WriteAllText(store.GetManifestPath(game), "{broken");
            read = store.ReadManifest(game);
            Check("Bozuk manifest güvenli biçimde raporlanır", read.Exists && !read.Valid);
            string settingsPath = Path.Combine(data, "config", "settings.json");
            Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
            File.WriteAllText(settingsPath, "{\"GameDir\":\"" + game.Replace("\\", "\\\\") + "\"}");
            AppSettings migrated = store.LoadSettings();
            Check("Ayar schema migration", migrated.SchemaVersion == 2);
            store.DeleteManifest(game);
        }

        private static void TestProfilesAndTokens(string temp)
        {
            string data = Path.Combine(temp, "ProfileData");
            Directory.CreateDirectory(data);
            string game = Path.Combine(temp, "ProfileGame");
            Directory.CreateDirectory(game);
            var store = new ProfileStore(data);
            var profile = new GameProfile
            {
                GameDir = game,
                PlayerName = "Tester",
                AppId = "480",
                Language = "english",
                BroadcastOn = true,
                DlcTemplateOn = false,
            };
            store.Save(profile);
            GameProfile? loaded = store.Load(game);
            Check("Profil atomic save/load", loaded?.PlayerName == "Tester" && loaded?.AppId == "480");

            string token = SessionProfileCodec.Encode(profile);
            GameProfile? decoded = SessionProfileCodec.Decode(token);
            Check("Session token encode/decode", decoded?.PlayerName == profile.PlayerName && decoded?.AppId == profile.AppId);
            Check("Session token path taşımaz", !token.Contains(game, StringComparison.Ordinal));
            Check("Session token limit", SessionProfileCodec.Decode(token + new string('A', 40000)) == null);
        }

        private static async Task TestPatchTransactionAsync(string temp)
        {
            string appBase = Path.Combine(temp, "App");
            string data = Path.Combine(temp, "Data");
            string game = Path.Combine(temp, "TransactionGame");
            Directory.CreateDirectory(appBase);
            Directory.CreateDirectory(game);

            string exe = Path.Combine(game, "Game.exe");
            string target = Path.Combine(game, "steam_api64.dll");
            byte[] original = BuildPe(0x8664, 1);
            byte[] component = BuildPe(0x8664, 2);
            File.WriteAllBytes(exe, BuildPe(0x8664, 3));
            File.WriteAllBytes(target, original);
            File.WriteAllBytes(Path.Combine(appBase, "steam_api64.dll"), component);

            var platform = new FakePlatform(data) { SupportsFirewall = true, IsElevated = true };
            var store = PatchManifestStore.CreateDefault(platform.Paths);
            var engine = new PatchEngine(platform, store);
            var targetInfo = GameScanner.FindApiDllTargets(game, exe).Single();
            byte[] loaded = PatchEngine.LoadComponentBytes(appBase, targetInfo)!;

            PatchManifest manifest = await engine.ApplyAsync(
                game,
                exe,
                new[] { (targetInfo, loaded) },
                new PatchEngine.PatchOptions("Tester", "english", string.Empty, true, 480),
                addFirewallRule: true);

            Check("Apply transaction commit", manifest.State == PatchTransactionState.Committed);
            Check("Backup hash doğru", FileIntegrity.MatchesSha256(manifest.Entries[0].BackupPath, manifest.Entries[0].BackupSha256));
            Check("Patched hash doğru", FileIntegrity.MatchesSha256(target, manifest.Entries[0].PatchedSha256));
            Check("Firewall ownership kaydı", manifest.FirewallRuleCreatedByUs && platform.FirewallCreateCount == 1);

            // Kullanıcının patch sonrası değişikliğini korumak için restore engellenmeli.
            File.AppendAllText(target, "user-change");
            bool conflictRejected = false;
            try { await engine.RestoreGameAsync(game, manifest, null); }
            catch (InvalidDataException) { conflictRejected = true; }
            Check("Post-patch kullanıcı değişikliği üzerine yazılmaz", conflictRejected);

            File.WriteAllBytes(target, component);
            RestoreResult result = await engine.RestoreGameAsync(game, manifest, null);
            Check("Restore original içeriği geri getirir", File.ReadAllBytes(target).SequenceEqual(original));
            Check("Restore backup'ı temizler", !File.Exists(manifest.Entries[0].BackupPath));
            Check("Restore oluşturulan dosyaları temizler",
                !File.Exists(Path.Combine(game, "steam_appid.txt")) &&
                !File.Exists(Path.Combine(game, "steam_settings", "force_account_name.txt")) &&
                !Directory.Exists(Path.Combine(game, "steam_settings")));
            Check("Restore firewall'ı kaldırır", result.FirewallRuleRemoved && platform.FirewallDeleteCount == 1);
            Check("Restore manifest'i temizler", !File.Exists(store.GetManifestPath(game)));
        }

        private static async Task TestInterruptedTransactionRecoveryAsync(string temp)
        {
            string data = Path.Combine(temp, "RecoveryData");
            string game = Path.Combine(temp, "RecoveryGame");
            Directory.CreateDirectory(game);

            string exe = Path.Combine(game, "Game.exe");
            string target = Path.Combine(game, "steam_api64.dll");
            string backup = target + ".bluelobby.recovery.bak";
            byte[] original = BuildPe(0x8664, 21);
            byte[] patched = BuildPe(0x8664, 22);
            File.WriteAllBytes(exe, BuildPe(0x8664, 23));
            File.WriteAllBytes(target, patched);
            File.WriteAllBytes(backup, original);

            var platform = new FakePlatform(data) { SupportsFirewall = false, IsElevated = false };
            var store = PatchManifestStore.CreateDefault(platform.Paths);
            var manifest = new PatchManifest
            {
                SchemaVersion = 2,
                State = PatchTransactionState.Preparing,
                GameDir = game,
                ExePath = exe,
                CreatedUtc = DateTime.UtcNow.ToString("O"),
            };
            manifest.Entries.Add(new PatchEntry
            {
                DllPath = target,
                BackupPath = backup,
                Is64Bit = true,
                Architecture = BinaryArchitecture.X64,
                OriginalSha256 = FileIntegrity.Sha256(backup),
                BackupSha256 = FileIntegrity.Sha256(backup),
                PatchedSha256 = FileIntegrity.Sha256(target),
            });
            store.SaveManifest(manifest);

            var engine = new PatchEngine(platform, store);
            bool recovered = await engine.RecoverInterruptedTransactionAsync(game);
            Check("Yarım kalan transaction recovery çalışır", recovered);
            Check("Recovery orijinal DLL'i geri getirir", File.ReadAllBytes(target).SequenceEqual(original));
            Check("Recovery manifesti temizler", !File.Exists(store.GetManifestPath(game)));
            Check("Recovery backup'ı temizler", !File.Exists(backup));
        }

        private static async Task TestWrongArchitectureDoesNotMutateAsync(string temp)
        {
            string appBase = Path.Combine(temp, "WrongArchApp");
            string data = Path.Combine(temp, "WrongArchData");
            string game = Path.Combine(temp, "WrongArchGame");
            Directory.CreateDirectory(appBase);
            Directory.CreateDirectory(game);

            string exe = Path.Combine(game, "Game.exe");
            string target = Path.Combine(game, "steam_api64.dll");
            byte[] original = BuildPe(0x8664, 7);
            byte[] wrong = BuildPe(0x014c, 8);
            File.WriteAllBytes(exe, BuildPe(0x8664, 9));
            File.WriteAllBytes(target, original);
            File.WriteAllBytes(Path.Combine(appBase, "steam_api64.dll"), wrong);

            var platform = new FakePlatform(data) { SupportsFirewall = false, IsElevated = true };
            var store = PatchManifestStore.CreateDefault(platform.Paths);
            var engine = new PatchEngine(platform, store);
            var targetInfo = GameScanner.FindApiDllTargets(game, exe).Single();
            byte[] loaded = PatchEngine.LoadComponentBytes(appBase, targetInfo)!;
            Check("Yanlış mimarili component kabul edilmez", loaded == null);

            bool rejected = false;
            try
            {
                await engine.ApplyAsync(
                    game,
                    exe,
                    new[] { (targetInfo, wrong) },
                    new PatchEngine.PatchOptions("Tester", "english", string.Empty, false, 480),
                    false);
            }
            catch (InvalidDataException)
            {
                rejected = true;
            }

            Check("Yanlış mimari patch mutation'a ulaşmadan reddedilir", rejected && File.ReadAllBytes(target).SequenceEqual(original));
        }

        private static async Task TestLinuxSteamLaunchAsync(string temp)
        {
            if (!OperatingSystem.IsLinux()) return;

            string bin = Path.Combine(temp, "fake-bin");
            Directory.CreateDirectory(bin);
            string marker = Path.Combine(temp, "steam-launch.txt");
            string steam = Path.Combine(bin, "steam");
            File.WriteAllText(steam, "#!/bin/sh\nprintf '%s' \"$*\" > \"" + marker.Replace("\\", "\\\\") + "\"\n");
            File.SetUnixFileMode(steam, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
            string oldPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            Environment.SetEnvironmentVariable("PATH", bin + Path.PathSeparator + oldPath);
            try
            {
                string exe = Path.Combine(temp, "ProtonGame.exe");
                File.WriteAllBytes(exe, BuildPe(0x8664));
                var service = new BlueLobby.Platform.Linux.LinuxPlatformServices();
                await service.LaunchGameAsync(exe, temp, 480);
                // CI runner'larda Process.Start sonrası shell shim'inin başlaması 500 ms'yi aşabilir.
                // Launcher davranışını test ederken kısa süreli timing flakiness oluşturmamak için
                // marker dosyasını makul bir timeout içinde bekliyoruz.
                for (int i = 0; i < 100 && !File.Exists(marker); i++) await Task.Delay(50);
                bool markerOk = File.Exists(marker) &&
                    File.ReadAllText(marker).Contains("-applaunch 480", StringComparison.Ordinal);
                Check("Linux Proton launch Steam AppID kullanır", markerOk);
            }
            finally
            {
                Environment.SetEnvironmentVariable("PATH", oldPath);
            }
        }

        private static void TestCompatDatabase()
        {
            var db = new CompatDatabase();
            var before = db.All().Count;
            db.LoadFromJson("not-json");
            Check("Bozuk compatibility JSON mevcut veriyi silmez", db.All().Count == before);
        }

        private static byte[] BuildPe(ushort machine, byte salt = 0)
        {
            byte[] data = new byte[512];
            data[0] = (byte)'M';
            data[1] = (byte)'Z';
            BitConverter.GetBytes(0x80).CopyTo(data, 0x3C);
            data[0x80] = (byte)'P';
            data[0x81] = (byte)'E';
            BitConverter.GetBytes(machine).CopyTo(data, 0x84);
            data[100] = salt;
            return data;
        }

        private static byte[] BuildElf(ushort machine)
        {
            byte[] data = new byte[128];
            data[0] = 0x7F; data[1] = (byte)'E'; data[2] = (byte)'L'; data[3] = (byte)'F';
            data[4] = 2; // ELF64
            data[5] = 1; // little endian
            BitConverter.GetBytes(machine).CopyTo(data, 18);
            return data;
        }
    }
}
