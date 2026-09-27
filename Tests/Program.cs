using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using BlueLobby.Core;
using BlueLobby.Platform;

int passed = 0;
int failed = 0;

void Check(string name, bool condition)
{
    if (condition)
    {
        passed++;
        Console.WriteLine($"PASS  {name}");
    }
    else
    {
        failed++;
        Console.WriteLine($"FAIL  {name}");
    }
}

// 1) ComputeKey: kararlı ve ayırt edici
string k1 = GameScanner.ComputeKey(@"C:\Games\Example");
string k2 = GameScanner.ComputeKey(@"C:\Games\Example");
string k3 = GameScanner.ComputeKey(@"C:\Games\Other");
Check("ComputeKey aynı girdi aynı çıktı", k1 == k2 && k1.Length == 16);
Check("ComputeKey farklı girdi farklı çıktı", k1 != k3);

// 2) Manifest JSON roundtrip
var manifest = new PatchManifest
{
    GameDir = @"C:\Games\Example",
    CreatedUtc = DateTime.UtcNow.ToString("O"),
    Entries = { new PatchEntry { DllPath = @"C:\Games\Example\steam_api64.dll", BackupPath = @"C:\Games\Example\steam_api64.dll.bak", Is64Bit = true } },
    CreatedFiles = { @"C:\Games\Example\steam_settings\force_language.txt" },
    CreatedDirectories = { @"C:\Games\Example\steam_settings" },
    TextBackups = { new FileBackup { OriginalPath = "a.txt", BackupPath = "a.txt.pre_slcc" } },
    FirewallRuleName = "BlueLobby_test"
};
string json = JsonSerializer.Serialize(manifest);
var back = JsonSerializer.Deserialize<PatchManifest>(json);
Check("Manifest roundtrip", back != null && back.Entries.Count == 1 && back.FirewallRuleName == "BlueLobby_test" && back.TextBackups.Count == 1);

// 3) FindApiDllTargets: iç içe klasörde bulur, exe'yi karıştırmaz
string temp = Path.Combine(Path.GetTempPath(), "slcc_test_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(Path.Combine(temp, "Binaries", "Win64"));
Directory.CreateDirectory(Path.Combine(temp, "redist"));
File.WriteAllBytes(Path.Combine(temp, "Binaries", "Win64", "steam_api64.dll"), new byte[] { 1, 2, 3 });
File.WriteAllBytes(Path.Combine(temp, "steam_api.dll"), new byte[] { 1 });
File.WriteAllBytes(Path.Combine(temp, "redist", "steam_api64.dll"), new byte[] { 1 });
File.WriteAllBytes(Path.Combine(temp, "game.exe"), new byte[] { 1 });

var targets = GameScanner.FindApiDllTargets(temp);
Check("FindApiDllTargets 3 hedef buldu", targets.Count == 3);
Check("FindApiDllTargets 64-bit işareti doğru", targets.Count(t => t.Is64Bit) == 2);
Directory.Delete(temp, recursive: true);

// 4) ErrorDoctor: dosya kilidi mesajı kullanıcı dilinde
var lockEx = new IOException("sharing violation") { HResult = unchecked((int)0x80070020) };
string explained = ErrorDoctor.Explain(lockEx);
Check("ErrorDoctor kilit hatası anlaşılır", explained.Contains("kullanılıyor") || explained.Contains("kapalı"));
Check("ErrorDoctor izin hatası öneri veriyor", ErrorDoctor.Explain(new UnauthorizedAccessException()).Contains("Yönetici"));

// 5) Linux .so hedefleri tespit edilir
string tempSo = Path.Combine(Path.GetTempPath(), "slcc_so_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempSo);
File.WriteAllBytes(Path.Combine(tempSo, "steam_api64.so"), new byte[] { 1 });
File.WriteAllBytes(Path.Combine(tempSo, "libsteam_api.so"), new byte[] { 1 });
File.WriteAllBytes(Path.Combine(tempSo, "steam_api64.dll"), new byte[] { 1 });
var soTargets = GameScanner.FindApiDllTargets(tempSo);
Check("FindApiDllTargets .so hedefleri buldu", soTargets.Count == 3 && soTargets.Count(t => t.IsSharedObject) == 2);
Check("GetComponentFileName platform adı doğru",
    GameScanner.GetComponentFileName(new ApiDllTarget { Is64Bit = true, IsSharedObject = true }) == "steam_api64.so" &&
    GameScanner.GetComponentFileName(new ApiDllTarget { Is64Bit = false }) == "steam_api.dll");
Directory.Delete(tempSo, recursive: true);

// 6) Platform servisleri doğru platformu seçiyor
IPlatformServices services = PlatformServiceFactory.Create();
bool isLinux = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
Check("Platform servisi Linux'ta firewall'sız", !isLinux || !services.SupportsFirewall);
Check("Platform servisi Linux'ta XDG yolları", !isLinux || services.Paths.RoamingDataDir.Contains(".config"));

// 7) Oyun çalışıyor kontrolü: var olmayan çalıştırılabilir için false
bool notRunning = !await services.IsGameRunningAsync("/tmp/bluelobby-test-does-not-exist/game_xyz_q.exe");
Check("IsGameRunningAsync var olmayan oyun için false", notRunning);
Check("IsElevated tanımlı", services.IsElevated || !services.IsElevated);


// ==================== Yeni altyapı testleri ====================

// 8) Oyun başına profil: kaydet/yükle
var profileStore = new ProfileStore(Path.Combine(Path.GetTempPath(), "bl_prof_" + Guid.NewGuid().ToString("N")));
var profile = new GameProfile { GameDir = @"C:\Games\TestGame", PlayerName = "MaviKurt", AppId = "480", Language = "turkish" };
profileStore.Save(profile);
var loaded = profileStore.Load(@"C:\Games\TestGame");
Check("ProfileStore kaydet/yükle", loaded != null && loaded.PlayerName == "MaviKurt" && loaded.AppId == "480");

// 9) Paylaşılabilir profil: encode/decode round-trip (madde 5 altyapısı)
string token = SessionProfileCodec.Encode(profile);
var decoded = SessionProfileCodec.Decode(token);
Check("SessionProfileCodec round-trip", decoded != null && decoded.PlayerName == "MaviKurt"
    && decoded.AppId == "480" && decoded.BroadcastOn && decoded.Language == "turkish");
Check("Codec token prefix dogru", token.StartsWith("BL1."));
Check("Codec bozuk token reddeder", SessionProfileCodec.Decode("BL1.garbage!!!") == null);
Check("Codec yanlis prefix reddeder", SessionProfileCodec.Decode("XX1.abc") == null);
// Token makineye özel yol içermez
Check("Codec GameDir tasimaz", !token.Contains("C:") && !token.Contains("Games"));

// 10) Oyun klasörü doğrulama (madde C altyapısı)
string tempGame = Path.Combine(Path.GetTempPath(), "bl_game_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempGame);
Check("Verifier bos klasoru reddeder", !GameVerifier.VerifyGameFolder(tempGame).LooksLikeGame);
File.WriteAllBytes(Path.Combine(tempGame, "steam_api64.dll"), new byte[] { 1 });
File.WriteAllBytes(Path.Combine(tempGame, "game.exe"), new byte[] { 1 });
for (int i = 0; i < 12; i++) File.WriteAllText(Path.Combine(tempGame, $"data{i}.txt"), "x");
var v = GameVerifier.VerifyGameFolder(tempGame);
Check("Verifier gercek oyun klasorunu kabul eder", v.LooksLikeGame && v.TargetCount == 1 && v.ExeCount == 1);
Directory.Delete(tempGame, true);

// 11) Yedek doğrulama + tazelik (madde C altyapısı)
string tempBak = Path.Combine(Path.GetTempPath(), "bl_bak_" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(tempBak);
string dll = Path.Combine(tempBak, "steam_api64.dll");
string bak = dll + ".bak";
File.WriteAllBytes(dll, new byte[] { 9, 9, 9 });   // yamali (farkli boyut)
File.WriteAllBytes(bak, new byte[] { 1 });          // orijinal yedek
var m = new PatchManifest { GameDir = tempBak };
m.Entries.Add(new PatchEntry { DllPath = dll, BackupPath = bak, Is64Bit = true });
Check("BackupValidator saglam yedegi kabul eder", BackupValidator.Validate(m).Valid);
File.Delete(bak);
Check("BackupValidator eksik yedegi reddeder", !BackupValidator.Validate(m).Valid);
File.WriteAllBytes(bak, new byte[] { 1 });
Check("IsPatchStale yamali dosyada false", !BackupValidator.IsPatchStale(m));
File.WriteAllBytes(dll, new byte[] { 1 });          // Steam dogrulamasi orijinali geri getirdi (boyut esit)
Check("IsPatchStale guncelleme sonrasi true", BackupValidator.IsPatchStale(m));
Directory.Delete(tempBak, true);

// 12) Uyumluluk listesi (madde B altyapısı)
var compat = new CompatDatabase();
Check("Compat L4D2 Works", compat.Lookup(550).Status == CompatStatus.Works);
Check("Compat Dota2 Unsupported", compat.Lookup(570).Status == CompatStatus.Unsupported);
Check("Compat HL2 NeedsConfig + not", compat.Lookup(220).Status == CompatStatus.NeedsConfig && compat.Lookup(220).Note.Contains("connect"));
Check("Compat bilinmeyen oyun", compat.Lookup(999999).Status == CompatStatus.Unknown);
Check("Compat liste dolu", compat.All().Count >= 10);


// 13) Heroic kütüphanesi (madde D)
string tempHeroic = Path.Combine(Path.GetTempPath(), "bl_heroic_" + Guid.NewGuid().ToString("N"));
string legDir = Path.Combine(tempHeroic, ".config", "heroic", "legendaryConfig", "legendary");
Directory.CreateDirectory(legDir);
string gameDir = Path.Combine(tempHeroic, "Games", "TestGame");
Directory.CreateDirectory(gameDir);
File.WriteAllText(Path.Combine(legDir, "installed.json"),
    "{\"TestGame\": {\"title\": \"Test Game\", \"install_path\": \"" + gameDir.Replace("\\", "/") + "\"}}");
var heroicGames = GameLibrary.FindHeroicGames(tempHeroic);
Check("Heroic kurulu oyun buldu", heroicGames.Count == 1 && heroicGames[0].Title == "Test Game" && heroicGames[0].InstallPath == gameDir);
Check("Heroic yoksa bos liste", GameLibrary.FindHeroicGames(Path.Combine(tempHeroic, "yok")) .Count == 0);
Directory.Delete(tempHeroic, true);

// 14) Proton algilama (madde D)
string tempProton = Path.Combine(Path.GetTempPath(), "bl_proton_" + Guid.NewGuid().ToString("N"));
string ctd = Path.Combine(tempProton, ".steam", "steam", "compatibilitytools.d", "GE-Proton9-5");
Directory.CreateDirectory(ctd);
File.WriteAllText(Path.Combine(ctd, "compatibilitytool.vdf"), "\"compatibilitytools\" {}");
string protonCommon = Path.Combine(tempProton, ".steam", "steam", "steamapps", "common", "Proton 8.0");
Directory.CreateDirectory(protonCommon);
var protons = GameLibrary.FindProtonVersions(tempProton);
Check("Proton surumleri bulundu", protons.Count == 2 && protons.Contains("GE-Proton9-5") && protons.Contains("Proton 8.0"));
Directory.Delete(tempProton, true);

Console.WriteLine();
Console.WriteLine($"Sonuç: {passed} geçti, {failed} kaldı.");
return failed == 0 ? 0 : 1;
