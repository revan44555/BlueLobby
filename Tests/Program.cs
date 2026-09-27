using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using BlueLobby;

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
string k1 = Core.ComputeKey(@"C:\Games\Example");
string k2 = Core.ComputeKey(@"C:\Games\Example");
string k3 = Core.ComputeKey(@"C:\Games\Other");
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

var targets = Core.FindApiDllTargets(temp);
Check("FindApiDllTargets 3 hedef buldu", targets.Count == 3);
Check("FindApiDllTargets 64-bit işareti doğru", targets.Count(t => t.Is64Bit) == 2);
Directory.Delete(temp, recursive: true);

// 4) ErrorDoctor: dosya kilidi mesajı kullanıcı dilinde
var lockEx = new IOException("sharing violation") { HResult = unchecked((int)0x80070020) };
string explained = ErrorDoctor.Explain(lockEx);
Check("ErrorDoctor kilit hatası anlaşılır", explained.Contains("kullanılıyor") || explained.Contains("kapalı"));
Check("ErrorDoctor izin hatası öneri veriyor", ErrorDoctor.Explain(new UnauthorizedAccessException()).Contains("Yönetici"));

Console.WriteLine();
Console.WriteLine($"Sonuç: {passed} geçti, {failed} kaldı.");
return failed == 0 ? 0 : 1;
