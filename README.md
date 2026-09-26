# Steam LAN Control Center

Türkçe tek paragraf: Bu proje, sahip olduğunuz Steam oyunları için LAN/Radmin VPN hazırlığını, yedeklemeyi ve geri almayı kolaylaştıran WPF yardımcı panelidir. Steam API dosyalarını değiştiren işlevler çevrimdışı/LAN kullanımı içindir; çevrimiçi/anti-cheat ortamlarında hesap veya erişim riski doğurabilir.

## Önemli sınır
- “Tüm DLC’leri aç” tarzı otomatik kilit aşma davranışı bilerek yoktur.
- `DLC.txt şablonu oluştur` seçeneği sadece yasal olarak sahip olduğunuz DLC AppID’lerini ekleyeceğiniz yorumlu şablon oluşturur.
- Araç yedek ve geri yükleme manifesti tutar; manifest olmadan sadece `steam_api*.dll.bak` dosyalarını geri yükler, diğer dosyalara dokunmaz.

## Kullanım
1. Oyun klasörünü seçin.
2. Gerekirse EXE aday listesinden çalıştırıcıyı seçin.
3. Oyuncu adı, AppID ve arkadaş Radmin IP değerini girin.
4. `Fix Uygula` ile düzenleme yapın; geri almak için `Geri Yükle` kullanın.

## Emülatör dosyaları
Proje, `steam_api.dll` / `steam_api64.dll` emülatör dosyalarını dağıtmaz. Çalışma zamanında EXE yanında bu dosyalar aranır. Dosyaların lisans ve kullanım şartlarına uygun şekilde temin edilmesi kullanıcının sorumluluğundadır.

## Veri ve güvenlik
- Ayarlar: `%AppData%\SteamLANControlCenter\settings.json`
- Manifest/log: `%LocalAppData%\SteamLANControlCenter\`
- Firewall kuralı varsa güncellenir; geri yüklemede kaldırılır.
- Uygulama yönetici ister; oyunu mümkünse explorer üzerinden normal kullanıcı bağlamında açmaya çalışır.

## Geliştirme
```bash
dotnet restore
dotnet build -c Release
```
