# BlueLobby v4.1 — Deep Review / Verification

## Scope

This review re-opened the v4.1 ZIP and compared it against the v4.0 infrastructure package. The review covers:

- Patch/restore behavior preservation
- Launcher discovery
- MVVM state/cancellation behavior
- target DLL selection
- path/manifest safety
- responsive UI changes
- CI packaging/checksum behavior
- integration tests and static consistency

## Findings fixed during the review

1. **Linux Steam discovery root mismatch**
   `LinuxPlatformServices` exposes `steamapps/common` paths. Discovery now normalizes `common` / `steamapps` candidates to the Steam library root before reading `libraryfolders.vdf` and manifests.

2. **Discovery cancellation race**
   A previous discovery could clear `IsBusy` or overwrite status after a newer scan began. Generation-based ownership now ensures only the current scan publishes state.

3. **Discovery button re-entry**
   The UI discovery button is disabled while the active scan is running.

4. **Runtime DLL discovery compatibility**
   The scanner no longer recursively patches every matching DLL. It still supports bounded, ranked runtime locations such as `Binaries/Win64`, `bin/x64`, and `lib`, while excluding obvious `redist`, `sdk`, `tools`, `backup`, test and installer directories.

5. **Manifest/lock key canonicalization on Windows**
   Windows drive/path casing is normalized before hashing, preventing duplicate manifest/lock identities for the same directory.

6. **Steam metadata bounds and path validation**
   VDF/ACF metadata is size-bounded and install paths are validated to remain inside the Steam `common` root.

7. **GOG false attribution reduced**
   The generic `~/Games` root is no longer labeled as GOG; only explicit GOG-oriented roots remain in this discovery stage.

8. **Responsive window floor**
   The original desktop minimum window size is retained; the compact desktop navigation mode remains available within that supported range.

9. **Touch-mode localization**
   The new touch-mode label uses the existing localization system.

10. **Release checksum verification**
    Windows and Linux checksum files now contain the filename in `sha256sum -c` compatible format. The GitHub release verification step therefore matches the generated checksum files.

## Preserved existing behavior

The following existing user-facing operations remain in the v4.1 flow:

- manual game-folder selection
- drag-and-drop folder selection
- executable selection
- component discovery/copy
- apply/patch
- backup
- restore
- rollback/recovery
- managed text files
- optional firewall rule
- game launch
- VPN status
- friends
- profile sharing/import
- compatibility database
- settings/theme/language/touch mode
- Windows/Linux platform split

Launcher discovery is additive; manual folder selection remains available.

## Verification performed

- Re-unzipped and inspected v4.1 sources.
- Compared v4.1 against v4.0 infrastructure sources.
- Reviewed all modified discovery/MVVM/UI/CI files line by line.
- Added/updated tests for bounded runtime discovery, Steam `common` root normalization and key canonicalization.
- Parsed project files as XML.
- Checked source brace balance and referenced file presence.
- Checked workflow checksum generation/verification consistency.

## Environment limitation

The working environment does not contain the .NET SDK, so a real `dotnet restore/build/test` run could not be executed here. This is intentionally not represented as a passing compiler/test result.

The ZIP is therefore a statically audited and corrected source package; CI is configured to perform the actual .NET 10 build and test run.


### Re-review pass (2026-09-28)

İlk v4.1 denetiminden sonra ikinci bir statik audit yapıldı ve şu düzeltmeler uygulandı:
1. Windows Steam kurulum kökü için registry keşfi eklendi.
2. Linux/SteamOS Steam-root sözleşmesi platform katmanına ayrıldı.
3. Heroic Flatpak/Legendary `installed.json` yolları güncellendi.
4. Lutris YAML parser'ı nested `game.exe`, `working_dir` ve `directory` alanlarını kapsayacak şekilde genişletildi.
5. Responsive breakpoint minimum pencere boyutuyla hizalandı.
6. Heroic/Lutris discovery testleri eklendi.

Güncel statik doğrulama: tüm aktif `.csproj` XML'leri parse ediliyor, workflow YAML parse ediliyor, tüm `ProjectReference` hedefleri mevcut, kullanılan localization key'leri tanımlı ve v4.0'da kullanılan ana patch/restore çağrı zincirleri v4.1'de korunuyor. Gerçek `dotnet build/test` bu ortamda .NET SDK bulunmadığı için çalıştırılamadı.

7. Heroic discovery adayları artık birleşik/boyut-limitli okunuyor; tek bir bozuk veya boş metadata kaynağı diğerlerini gölgelemiyor.
8. Lutris install path çözümlemesinde explicit directory, executable parent ve working directory önceliği netleştirildi.


## Final reviewed package — 2026-09-28

- Product version bumped to 4.2.0 after the re-review fixes.
- ProjectReference targets resolve after normalizing Windows-style separators.
- Localization audit: 42 UI keys used by code, all 42 are defined.
- Lightweight C# delimiter scan: PASS for all active source files; this is a static sanity check, not compiler validation.
- Required project files and CI workflow are present.
- Final ZIP is intended to contain source/project files only; audit workspace files are excluded.

The only validation not possible in this runtime is an actual .NET compiler/test execution because the .NET SDK is not installed here.
