# Yapılan düzeltme özeti

- Kod arkası sınıfına manifest/ayar modeli ayrıldı: `Models.cs`.
- EXE adayı artık ilk dosya otomatik seçilmez; `CmbExes` listesi doldurulur, eski seçim hatırlanır.
- Oyun klasörü değişince eski `selectedExePath` sıfırlanır; yanlış oyuna ait EXE başlatma riski kapatıldı.
- Patch öncesi tüm hedefler ve emülatör dosyaları doğrulanır; eksik varsa dosya yazımına başlanmaz.
- Geri yükleme artık rastgele `*.bak` silmez. `%LocalAppData%\SteamLANControlCenter\manifests` altındaki manifeste göre çalışır.
- Metin dosyaları üzerine yazmadan önce `.pre_slcc` yedeği alınır; manifest olmadan fallback sadece `steam_api*.dll.bak` geri yükler.
- Hata durumunda kısmi işlemler geri alınır.
- Firewall kuralı eklemeden önce varlık kontrolü yapar, mükerrer kural oluşturmaz, `remoteip=localsubnet,26.0.0.0/8` ile sınırlanır, geri yüklemeyle kaldırılır.
- Radmin algılaması process adı + NIC kontrolüne çekildi; banner akışı tutarlı hale getirildi.
- Ping/status timer'ı reentrancy korumalı; renkler cache'li; klasör taramaları UI thread'i bloklamaz.
- Oyunun çalışıp çalışmadığı patch/restore öncesi kontrol edilir.
- Oyun başlatma elevated bağlamdan mümkün olduğunca ayrık yapılmaya çalışılır.
- Log dosyaya yazılır; kopyala/dışa aktar/temizle eklendi.
- Ayarlar JSON olarak saklanır; AppID doğrulaması eklendi.
- CI: restore/build/publish ayrıldı, PR tetiklemesi, self-contained single-file publish, sürümlü artifact.
- README/LICENSE/.gitignore eklendi; otomatik DLC kilidi aşma davranışı kaldırıldı, yerine yorumlu DLC şablonu seçeneği kondu.

Yerel doğrulama: `dotnet build -c Release -p:EnableWindowsTargeting=true` başarılı, 0 uyarı.


---

# Aşama 1 (onaylı) tamamlandı

- **Sürükle-bırak:** oyun klasörü pencereye bırakılınca seçilir; dosya değil klasör istenir, yanlış şey bırakılınca nazik uyarı verir.
- **İlerleme çubuğu:** tüm uzun işlemlerde yüzde + adım mesajı gösterilir (hedef tarama, doğrulama, DLL güncelleme, firewall, manifest, geri yükleme). Bilinmeyen sürelerde belirsiz animasyon; hiçbir işlem sessizce bekletmez.
- **Renkli ping:** 60 ms altı yeşil, 60-150 sarı, üstü kırmızı.
- **Hata doktoru:** teknik exception'lar kullanıcı diline çevrilir (izin yok → yönetici önerisi, dosya kilitli → oyunu kapat, klasör yok → yeniden seç, yol çok uzun → kısa yol önerisi). Dosya kilidi HResult ile yakalanır.
- Log artık arka plan iş parçacığından da güvenli (Dispatcher.BeginInvoke).


---

# Aşama 2-5 (onaylı) tamamlandı

- **Tarihli yedekler:** durum satırı düzenleme tarihini gösterir (🗓 dd.MM.yyyy HH:mm).
- **Geri alma ekranı:** `RestoreWindow` manifestteki her şeyi listeler (DLL yedekleri, metin yedekleri, silinecek araç dosyaları, firewall kuralı, tarih) ve onay ister. Manifest yoksa sınırlı mod (yalnız steam_api*.dll.bak) ayrıca onaylanır.
- **Arkadaş listesi:** isim+IP kaydet, listeden seç/"Kullan" ile ana IP alanına doldur, sil; çift tık da çalışır. Ayarlarda kalıcı.
- **Tema:** Gece/Gündüz/Pembe; canlı değişir, kaydedilir.
- **Maskot Mavi:** sağ üstte usul usul zıplar, tıklayınca mini rehber verir, kapatılabilir.
- **Dil desteği:** Loc.cs ile tr/en/de; pencere genişledi, uzun metinler tooltip ile korunur.
- **Güncelleme kontrolü:** UpdateChecker sessiz; yeni sürüm yoksa hiçbir şey demez. ReleasesUrl boş = kapalı.
- **Testler:** Tests/ projesi (DLL referanslı, bağımsız). 7 test: ComputeKey, manifest roundtrip, hedef bulma, hata doktoru.
- **Derleme:** ana proje 0 hata/0 uyarı; testler 7/7 PASS.

Yerel test: ana projeyi Release derleyip `dotnet run --project Tests` .


---

# Arayuz yenileme (v2.2 gorunum)

- Sol menu navigasyonu: Oyun / Arkadaslar / Ayarlar / Gelismis (log).
- Terminal benzeri log artik varsayilan olarak GIZLI; "Gelismis / Log" menuden acilir. HATA olunca otomatik o bolume gecilir.
- Mavi maskot sol altta kart icinde; ayarlardan kapatilabilir.
- Radmin VPN bulunamazsa banner'da "Resmi Siteden Indir" butonu (radmin-vpn.com) + standart ayar butonu.


---

# Emulator otomasyonu (Ara ve Kopyala)

- Durum kartinda emulator satiri: 🟢 hazir / 🟡 bekleniyor (dil destekli).
- "Emulatoru Ara ve Kopyala" butonu: Indirilenler + Masaustu + Belgeler icinde steam_api64.dll / steam_api.dll arar, bulunca onay ister ve EXE yanina kopyalar.
- Bulunamazsa dosya secme penceresi acilir; ayni klasordeki ikinci dosya da otomatik yakalanir.
