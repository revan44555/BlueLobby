# BlueLobby

BlueLobby, sahip olduğunuz oyun kurulumlarını yerel ağ/VPN oturumuna hazırlamak için dosya yedekleme, kontrollü değişiklik ve geri yükleme sunan Windows/Linux masaüstü yardımcı uygulamasıdır. Uygulama, kullanıcı tarafından getirilen üçüncü taraf bileşenleri yönetir; bunları projeye gömmez.

## Tasarım ilkesi

Ana akış:

`klasör seç → çalıştırıcıyı doğrula → hedefi belirle → backup + journal → patch → hash doğrula → commit`

Geri alma:

`manifest doğrula → backup hash doğrula → kullanıcı değişikliği kontrolü → restore → doğrula → cleanup`

Dosya değişiklikleri UI katmanında yapılmaz. `BlueLobby.Core/PatchEngine.cs` transaction mantığını yürütür; `PatchManifestStore` kalıcı journal tutar; `BlueLobby.Platform` Windows/Linux yan etkilerini yönetir.

## Güvenlik ve recovery

- Yama öncesi transaction manifesti disk'e yazılır.
- Her değiştirilen binary için SHA-256 original/backup/patched hashleri tutulur.
- Managed metin dosyaları için de hash tabanlı bütünlük kontrolü yapılır.
- Backup'lar başarılı restore doğrulanana kadar silinmez.
- Restore, patch sonrası kullanıcı tarafından değiştirilmiş dosyanın üstüne sessizce yazmaz; transaction'ı durdurur.
- Manifest path'leri seçilen oyun klasörünün dışına çıkamaz.
- Yarım kalan `Preparing`, `RollingBack` veya `Restoring` transaction'ları oyun klasörü seçilirken recovery akışına alınır.
- Aynı oyun için farklı BlueLobby süreçlerinin paralel mutation yapması oyun-başı OS lock ile engellenir; mutation öncesi diskten güncel manifest tekrar okunur.
- Firewall kuralı oluşturulacaksa adı transaction'a özgü olur; mevcut başka bir kural sahiplenilmez.

## Hedef seçimi

BlueLobby artık oyun klasörünün tamamındaki aynı isimli DLL'leri sınırsız recursive olarak değiştirmez. Önce seçilen çalıştırıcının bulunduğu klasörü hedefler; uygun runtime bulunamazsa oyun kökü ve en fazla iki seviye derinlikte, önceliklendirilmiş `Binaries/Win64`, `bin/x64`, `lib` gibi runtime dizinleri değerlendirilir. `redist`, `sdk`, `tools`, `backup`, test ve installer dizinleri dışarıda bırakılır. Binary mimarisi PE/ELF header'ından okunur ve çalıştırıcı mimarisiyle eşleştirilir.

## Platformlar

- Windows x64
- Linux x64

UI Avalonia 11.3.22 üzerine kuruludur. Tüm aktif projeler .NET 10 hedefler; ürün sürümü 4.3.0'dir.


## v4.3.0 keşif ve responsive altyapısı

- Steam `libraryfolders.vdf` + `appmanifest_*.acf` üzerinden bounded discovery
- Heroic installed metadata discovery
- Lutris oyun tanımı discovery
- GOG odaklı bounded root discovery; drive-wide scan yok
- Discovery cancellation ve stale-result koruması
- Manuel klasör seçimi mevcut davranış olarak korunur
- Responsive compact navigation ve scrollable içerik
- MVVM foundation (`MainViewModel`) discovery akışında kullanılır

Bu katman mevcut patch/restore motorunun yerine geçmez; oyun seçimini kolaylaştıran ek bir giriş yoludur.

## Üçüncü taraf bileşenler

Proje üçüncü taraf API bileşenlerini dağıtmaz. Kullanıcı, lisans ve kullanım şartlarına uygun bir bileşeni uygulama klasörüne kendisi getirebilir veya yerel arama yardımcı fonksiyonunu kullanabilir.

## Geliştirme

```bash
dotnet restore BlueLobby.sln
dotnet build BlueLobby.sln -c Release
dotnet run --project Tests/BlueLobby.Tests.csproj -c Release
```

## Dağıtım

CI, Windows ve Linux için self-contained single-file paketleri ZIP/TAR.GZ olarak üretir ve SHA-256 checksum yayımlar.

`Legacy/Wpf/` eski WPF implementasyonunu arşiv olarak içerir; aktif uygulama Avalonia tabanlıdır.

## Sınırlar

- DRM bypass / crack özelliği yoktur.
- Yetkisiz erişim veya hesap ele geçirme davranışı yoktur.
- DLC kilidi aşma yoktur; ilgili alan yalnızca kullanıcıya ait kimliklerin yazılabileceği bir şablondur.

## v4.3.0 hardened platform behavior

The active target is Windows x64 + Linux x64. Linux Windows-game launching is routed through Steam by AppID when possible so Proton/compatdata setup remains under Steam's control; native Linux executables continue to launch directly. Steam metadata parsing is now token-based and launcher discovery is bounded.


## v4.3.0 UI

Windows + Linux desktop arayüzü yeniden düzenlendi: daha belirgin gezinme, güçlü ana aksiyon hiyerarşisi, durum çipleri, daha temiz kart yapısı ve dar desktop/Steam Deck pencerelerinde daha iyi yerleşim. Mobil/Android/iOS hedefi eklenmedi.
