# Teknik değişiklik özeti

## v4.0 — Transactional recovery çekirdeği

- .NET 10'a geçildi; CI artık `10.0.x` kullanıyor.
- Avalonia 11.3.22'ye yükseltildi.
- Patch engine UI'dan ayrıldı; UI artık dosya mutation callback'i vermez.
- Patch manifesti schema v2'ye taşındı ve transaction state içeriyor: `Preparing`, `Committed`, `Restoring`, `RollingBack`, `RolledBack`.
- SHA-256 original/backup/patched hashleri eklendi.
- Binary mimarisi PE/ELF header'ından tespit ediliyor.
- Recursive bütün-DLL patch yaklaşımı kaldırıldı; hedef önce seçilen exe'nin runtime klasöründen çözülüyor.
- Atomic/durable file write ve atomic manifest save eklendi.
- Backup'lar restore doğrulanmadan silinmiyor.
- Restore öncesi post-patch kullanıcı değişikliği algılanıyor ve sessiz overwrite engelleniyor.
- Manifest path validation eklendi.
- Yarım transaction startup/folder-selection recovery eklendi.
- Oyun-başı cross-process lock ve stale-manifest tazelemesi eklendi; iki ayrı BlueLobby örneğinin aynı oyunu eşzamanlı mutate etmesi engellendi.
- Firewall ownership benzersiz transaction rule adına taşındı; mevcut kullanıcı kuralları sahiplenilmiyor.
- UI log erişimi UI dispatcher ile güvenli hale getirildi.
- Status refresh için debounce/cancellation eklendi.
- Component search doğrulama ve atomic copy kullanıyor; bilinmeyen architecture reddediliyor.
- Windows/Linux oyun process kontrolü mümkün olduğunca gerçek executable yoluna bağlandı.
- Log dosyası 5 MB seviyesinde rotate ediliyor.
- Release pipeline artık tek executable dosyası yerine gerçek paket + checksum üretiyor.
- Eski WPF kodu aktif runtime yolundan ayrı tutuluyor.

## Not

Bu sürüm için çalışma ortamında .NET SDK bulunmadığından yerel `dotnet build/test` sonucu bağımsız olarak doğrulanamadı. CI dosyası güncel hedef olarak .NET 10 ve integration testlerini çalıştıracak şekilde düzenlendi.


## v4.3.0 UI refresh

- Desktop-first visual system refreshed.
- Navigation hierarchy, cards, status chips and primary actions redesigned.
- Theme surface/border palette centralized.
- UI strings added for the new shell/page headings.
- No Android/iOS/mobile target added.
