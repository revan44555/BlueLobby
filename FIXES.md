# Teknik değişiklik özeti

## v3.1 — İki platformlu mimari (Windows + Linux)
- Proje üç katmana ayrıldı: `BlueLobby.Platform` (IPlatformServices + Windows/Linux implementasyonları),
  `BlueLobby.Core` (platform bağımsız yama/geri yükleme motoru), `BlueLobby` (Avalonia arayüzü).
- Arayüz WPF'den Avalonia'ya taşındı; tek kod tabanından `win-x64` ve `linux-x64` binary üretilir.
- Yönetici zorunluluğu kaldırıldı (`asInvoker`). Windows'ta güvenlik duvarı kuralı istenirse ve yetki yoksa
  kullanıcı bilgilendirilir; Linux'ta firewall özelliği kapalıdır (Wine/Proton kendi iznini yönetir).
- Yama öncesi "oyun çalışıyor mu" kontrolü platform servislerine taşındı (Windows: süreç adı, Linux: /proc).
- Hedef tarama Linux oyunlarını da kapsar: `steam_api64.so` / `libsteam_api.so`.
- VPN tespiti platforma göre: Windows'ta Radmin (26.x), Linux'ta Tailscale/ZeroTier/Hamachi/WireGuard.
- Eski WPF kodu `Legacy/Wpf/` altında arşivlidir.
- Tam özellik portu: 4 bölüm navigasyonu (Oyun/Arkadaşlar/Ayarlar/Log), arkadaş listesi
  (ekle/çift tıkla kullan/sil), 3 tema, tr/en/de arayüz dili (Loc), maskot, geri yükleme önizleme
  diyaloğu, ping testi, otomatik LAN kurulumu, yerel bileşen arama, tam ekran (F11), log kopyala/dışa aktar.
- Yerel doğrulama: `dotnet build BlueLobby.sln -c Release` 0 hata; testler 13/13 PASS;
  `win-x64` ve `linux-x64` self-contained publish doğrulandı.

## Geçmiş
- UI/çeviri metinleri yerel ağ/VPN oturum hazırlığı, yedekleme ve geri yükleme diliyle uyumlu hale getirildi.
- Geri alma manifesti, hedef doğrulama, yedekleme ve test altyapısı korundu.
