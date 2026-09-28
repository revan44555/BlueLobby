# BlueLobby v4 — Altyapı Yeniden Tasarım Raporu

## Amaç

Bu revizyon, patch/restore akışını UI merkezli dosya kopyalama yaklaşımından çıkarıp crash-safe, hash-doğrulamalı ve recovery destekli bir transaction altyapısına taşır.

## Yeni sınırlar

```text
Avalonia UI
   │
   ├── View / ViewModel adayı
   │
   ▼
PatchEngine (Application/Core)
   │
   ├── PatchManifestStore
   ├── FileIntegrity
   ├── PathSafety
   ├── BinaryArchitectureDetector
   └── GameScanner
   │
   ▼
IPlatformServices
   ├── Windows
   └── Linux
```

UI doğrudan oyun DLL'i, backup veya firewall üzerinde mutation yapmaz.

## Transaction yaşam döngüsü

```text
        ┌──────────────┐
        │  Preparing   │
        └──────┬───────┘
               │ journal + backup + mutation
               ▼
        ┌──────────────┐
        │  Committed   │
        └──────┬───────┘
               │ restore
               ▼
        ┌──────────────┐
        │  Restoring   │
        └──────┬───────┘
               │ verify + cleanup
               ▼
        ┌──────────────┐
        │ RolledBack   │
        └──────────────┘

Hata:
Preparing/Restoring/RollingBack -> recovery
```

## Temel invariants

1. Native binary architecture doğrulanmadan patch yapılmaz.
2. Her mutation için manifest önce yazılır.
3. Binary/text backup bütünlüğü SHA-256 ile doğrulanır.
4. Restore öncesi kullanıcı değişikliği tespit edilirse dosyanın üzerine sessizce yazılmaz.
5. Backup cleanup ancak başarılı restore/rollback ve doğrulamadan sonra yapılır.
6. Manifest'teki bütün yollar seçilen oyun klasörünün içinde kalmalıdır.
7. Symbolic link/junction/reparse path üzerinden mutation reddedilir.
8. Aynı oyun için farklı BlueLobby süreçleri OS-level lock ile seri hale getirilir.
9. Firewall yalnızca bu transaction tarafından oluşturulduğu açıkça journal'lanmışsa kaldırılır.
10. Cross-process işlem başlatıldığında disk üzerindeki güncel manifest tekrar okunur; stale in-memory state ile mutation yapılmaz.

## Hedef çözümleme

Eski yaklaşım olan `gameDir` altında recursive aynı isimli bütün DLL'leri değiştirme kaldırıldı.

Yeni akış:

1. Kullanıcının seçtiği executable belirlenir.
2. Executable PE/ELF header mimarisi okunur.
3. Önce executable'ın bulunduğu runtime klasörü taranır.
4. Gerekirse oyun kökü ikinci aday olarak kullanılır.
5. Yalnızca gerçek binary formatı okunabilen ve executable mimarisiyle eşleşen API dosyaları hedeflenir.

Dosya adından 32/64-bit çıkarımı yapılmaz.

## Atomicity

Dosya yazma işlemleri aynı dizinde benzersiz temporary dosya kullanır, diske flush eder ve ardından replace/move yapar. Manifest ve settings de aynı durable write mekanizmasını kullanır.

## Recovery

Olası crash noktaları journal'daki intent kayıtlarıyla korunur. Backup kopyalanmadan önce backup yolu manifest'e yazılır. Böylece process kopyalama sırasında ölse bile sonraki recovery hangi backup'ı kontrol edeceğini bilir.

## UI

- Background mutation doğrudan Avalonia kontrollerine yazmaz.
- `Progress<T>` / UI Dispatcher ile thread-safe ilerleme güncellenir.
- Status refresh debounce + cancellation kullanır.
- Dar pencere genişliklerinde sidebar compact navigation'a dönüşür ve ana içerik scrollable hale gelir.
- Temel theme geçişinde görsel tree yeniden oluşturulur.

Bu, yalnızca Windows/Linux desktop UI için responsive bir temel sağlar.

## CI / release

- .NET 10 baseline
- Avalonia 11.3.22 baseline
- Windows x64 single-file package
- Linux x64 single-file package
- SHA-256 checksum
- Tek release job ile iki platform asset'inin aynı GitHub Release içinde yayınlanması

## Test kapsamı

Test projesine architecture detection, scanner filtering, manifest corruption, profile/token limits, full apply/restore transaction, restore conflict ve wrong-architecture mutation rejection senaryoları eklendi.

## Bu çalışma ortamındaki doğrulama sınırı

Bu çalışma ortamında `.NET SDK` mevcut değildi. Bu nedenle `dotnet restore/build/run` yerel olarak çalıştırılamadı. Buna rağmen:

- bütün `.csproj` XML dosyaları parse edildi,
- CI workflow YAML parse edildi,
- aktif C# dosyalarında delimiter/static syntax taraması yapıldı,
- aktif referanslar ve interface implementasyonları çapraz kontrol edildi.

Gerçek compile/test sonucu için repository CI veya .NET 10 SDK'lı bir ortam gereklidir.

## v4.1 — Product Architecture Stage

### Launcher-aware discovery
- Added bounded Steam `libraryfolders.vdf` + `appmanifest_*.acf` discovery.
- Normalizes `steamapps/common` candidates emitted by Linux platform paths to the actual Steam root.
- Adds metadata size limits and validates Steam install paths inside the library `common` root.
- Added Heroic installed-game discovery through the existing metadata reader.
- Added Lutris `.yml` discovery for installed game directories.
- Added bounded GOG-oriented roots without drive-wide recursive scanning; generic `~/Games` is not mislabeled as GOG.
- Discovery is read-only, cancellation-aware, and stale-result protected.

### MVVM foundation
- Added `MainViewModel` implementing `INotifyPropertyChanged`.
- Added reusable `AsyncCommand`.
- Added generation-based cancellation so an older discovery cannot clear the busy state or overwrite a newer result.
- Existing code-behind flows remain intact for backward compatibility; new discovery UI consumes the VM.

### Responsive desktop/Steam Deck shell
- Compact navigation remains available for narrow desktop/Steam Deck windows.
- Original desktop minimum window size is retained; compact navigation remains available while resizing down to that supported floor.
- Touch-mode label is localized.
- Game screen now exposes launcher discovery alongside manual folder selection.
- Existing manual folder selection and patch/restore actions remain available.

### v4.1 review fixes
- Windows manifest/lock key normalization is now case-insensitive like the filesystem.
- API target resolution supports bounded, prioritized runtime subdirectories while excluding obvious redistributable/tool/test folders.
- CI checksum files are now generated in `sha256sum -c` compatible `hash  filename` format.


## v4.1 re-review — launcher discovery hardening

- Steam root discovery artık Windows registry'deki Valve SteamPath değerlerini de kullanır; Linux/SteamOS'ta ayrı Steam-root sağlayıcısı kullanılır.
- Heroic discovery normal Legendary, Heroic iç kopya ve Flatpak Legendary `installed.json` yollarını kontrol eder.
- Lutris discovery gerçek oyun yapılandırmalarındaki nested `game.exe`, `working_dir` ve `directory` alanlarını destekler; YAML dosyaları boyut limitlidir.
- Responsive breakpoint 820 px ile minimum pencere boyutuyla uyumlu hale getirildi.
- Re-review sonrası yeni launcher keşif testleri eklendi.

- Heroic metadata adayları boyut limiti ile okunur ve bir adaydaki kayıtlar diğer geçerli adayları engellemez.
- Lutris discovery'de explicit directory > absolute executable parent > working_dir önceliği kullanılır.
