# Yol haritası — v4 sonrası

## v4.0 çekirdek implementasyonu

- [x] Transactional patch engine
- [x] Crash/restart recovery
- [x] SHA-256 integrity model
- [x] PE/ELF architecture detection
- [x] Restricted target resolution
- [x] Atomic manifest/settings writes
- [x] Restore conflict detection
- [x] Firewall ownership
- [x] .NET 10 + Avalonia 11.3 baseline
- [x] Windows/Linux package + checksum CI
- [x] Oyun-başı cross-process transaction lock

## Ürün kapsamı

- Aktif hedef platformlar: **Windows x64 ve Linux x64**.

## v4.1 tamamlanan altyapı parçaları

> Not: Çalışma ortamında .NET 10 SDK olmadığı için yerel derleme/test bu ortamda doğrulanamadı; CI bu doğrulamanın kaynağıdır.

1. **Launcher-aware game discovery — kısmi tamamlandı**
   - [x] Steam library / libraryfolders.vdf
   - [x] Heroic metadata
   - [x] Lutris metadata
   - [x] Bounded GOG-root discovery
   - [ ] Proton prefix tespiti

2. **MVVM foundation — başlangıç tamamlandı**
   - [x] MainViewModel
   - [x] AsyncCommand
   - [x] Discovery cancellation/stale-result guard
   - [ ] GameViewModel
   - [ ] FriendsViewModel
   - [ ] SettingsViewModel
   - [ ] WizardViewModel
   - [ ] PatchOperationService facade

3. **Responsive UI — temel tamamlandı**
   - [x] desktop sidebar
   - [x] compact narrow-window / Steam Deck layout
   - [x] compact bottom navigation for narrow desktop/Steam Deck windows
   - [x] ScrollViewer + dynamic width breakpoint
   - [x] touch-mode hit target altyapısı
   - [ ] keyboard accessibility audit

4. **Compatibility schema v2**
   - oyun sürümü
   - platform
   - test tarihi
   - yöntem sürümü
   - kanıt/not

5. **Release security**
   - Windows code signing
   - SBOM
   - reproducible build metadata
   - signed checksums

## Sonraki adım

1. **MVVM'i gerçek ekranlara yayma**
   - GameView / GameViewModel
   - FriendsViewModel
   - SettingsViewModel
   - WizardViewModel
   - PatchOperationService facade

2. **Discovery doğruluğunu genişletme**
   - Steam library path variations
   - GOG Galaxy registry/manifest doğrulaması
   - Proton prefix mapping
   - executable selection quality scoring

3. **Release security**
   - Windows code signing
   - SBOM
   - reproducible build metadata
   - signed checksums

## Daha sonra

- Oyun keşfi ve son oyunlar
- QR ile session profile
- Lobi ağı / senkron başlatma
- Ayrı sync service
- Gelecekte değerlendirilebilir: Windows/Linux dışındaki platformlar ürün kapsamı dışındadır.

## Kırmızı çizgiler

- DRM bypass / crack yok
- Yetkisiz erişim yok
- DLC kilit aşma yok
- Kaynağı/lisansı belirsiz üçüncü taraf binary repo içine gömülmez

- [x] Windows/Linux desktop UI polish and visual system refresh
