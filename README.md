# BlueLobby

Türkçe tek paragraf: Bu proje, sahip olduğunuz oyunlar için yerel ağ/VPN oturum hazırlığını, yedeklemeyi ve geri almayı kolaylaştıran WPF yardımcı panelidir. Hedef oyun klasöründeki API bileşenleriyle ilgili düzenlemeler çevrimdışı/LAN kullanımı içindir; çevrimiçi/anti-cheat ortamlarında hesap veya erişim riski doğurabilir.

## Önemli sınır
- Otomatik kilit aşma veya yetkisiz erişim sağlama davranışı yoktur.
- Ek içerik şablonu seçeneği yalnızca yasal olarak sahip olduğunuz ek içerik kimliklerini ekleyeceğiniz yorumlu şablon oluşturur.
- Araç yedek ve geri yükleme manifesti tutar; manifest olmadan yalnızca hedef API DLL yedeklerini geri yükler, diğer dosyalara dokunmaz.

## Kullanım
1. Oyun klasörünü seçin.
2. Gerekirse EXE aday listesinden çalıştırıcıyı seçin.
3. Oyuncu adı, uygulama kimliği ve arkadaş VPN IP değerini girin.
4. `Uygula` ile düzenleme yapın; geri almak için `Geri Yükle` kullanın.

## Üçüncü taraf bileşenler
Proje, üçüncü taraf API bileşen dosyalarını dağıtmaz. Çalışma zamanında EXE yanında uygun bileşen dosyaları aranır. Dosyaların lisans ve kullanım şartlarına uygun şekilde temin edilmesi kullanıcının sorumluluğundadır.

## Veri ve güvenlik
- Ayarlar ve manifest/log için kullanıcı uygulama veri klasörleri kullanılır.
- Firewall kuralı varsa güncellenir; geri yüklemede kaldırılır.
- Uygulama yönetici ister; oyunu mümkünse explorer üzerinden normal kullanıcı bağlamında açmaya çalışır.

## Geliştirme
```bash
dotnet restore
dotnet build -c Release
```

## Not
Bu proje herhangi bir oyun platformu, mağaza veya VPN sağlayıcısı ile bağlantılı/destekli değildir. Tüm markalar ilgili sahiplerine aittir.


## Mimari (v3.1+)
Proje iki platformda da çalışacak şekilde katmanlara ayrıldı:

- `BlueLobby.Platform` — `IPlatformServices` sözleşmesi; `Windows/WindowsPlatformServices.cs` (netsh firewall, Radmin tespiti) ve `Linux/LinuxPlatformServices.cs` (Tailscale/ZeroTier tespiti, XDG yolları) çalışma zamanında seçilir. Linux'ta firewall özelliği kapalıdır (Wine/Proton kendi iznini yönetir).
- `BlueLobby.Core` — platform bağımsız iş mantığı: hedef tarama (`.dll` ve `.so`), yama motoru, yedekleme/geri yükleme manifesti.
- `BlueLobby` — Avalonia tabanlı arayüz; aynı kod `win-x64` ve `linux-x64` olarak derlenir.
  - `Ui.cs` — stil/ölçü yardımcıları (görsel düzen revizyonları burada yapılmalı)
  - `MainWindow.Ui.cs` — arayüz kurulumu (partial)
  - `MainWindow.axaml.cs` — iş mantığı (partial)

Derleme:
```bash
dotnet build BlueLobby.sln -c Release
dotnet publish BlueLobby/BlueLobby.csproj -c Release -r win-x64 --self-contained -o pub/win
dotnet publish BlueLobby/BlueLobby.csproj -c Release -r linux-x64 --self-contained -o pub/linux
```

Eski WPF uygulaması `Legacy/Wpf/` altında arşivlidir.
