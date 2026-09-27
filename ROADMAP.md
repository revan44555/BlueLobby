# Yol Haritası (kullanıcı istekleri)

## Şu anki karar: basitlik korunuyor
Projenin çekirdek değeri "klasör seç → uygula → oyna → geri yükle" akışının sade ve
güvenilir olması. Ağır altyapı özellikleri zamanı gelene kadar ertelendi.

## ✅ Onaylanan özellikler (yapılacak)

1. [x] **Oyun başına profil kaydı**  ✅ v3.2 (UI'a bağlandı) — Her oyun kendi ayarını hatırlasın.
   Örn: Garry's Mod seçince oyuncu adı + AppID otomatik ona göre dolsun;
   Left 4 Dead 2'ye geçince otomatik değişsin. Elle düzeltmek yok.
5. [x] **Paylaşılabilir oturum profilleri**  ✅ v3.2 (UI'a bağlandı) — Ayarları arkadaşına tek kod ile gönder.
   Sen "şu ayarlarla kurulum yap" deyip kodu atarsın, arkadaşı kodu yapıştırıp
   aynı ayarlarla tek tıkla kurar. QR kod sonraki aşama.

## 🏗️ Altyapı durumu (KURULDU, test edildi)

Aşağıdaki sınıflar BlueLobby.Core'e eklendi ve 30/30 test ile doğrulandı.
Özellikler UI'a bağlanırken bu hazır parçalar kullanılacak:

| Sınıf | Hangi madde | Ne işe yarar |
|---|---|---|
| `Profiles.cs` (GameProfile + ProfileStore) | 1 | Oyun başına ayar kaydı: save/load/list |
| `SessionProfileCodec` | 5 | Ayar seti → `BL1.` token → çözme; yol taşımaz, bozuk token reddeder |
| `GameVerifier` | C | "Bu klasörde oyun mu var?" kontrolü |
| `BackupValidator` | C | Yedek sağlamlığı + "Steam güncellemesi yamayı sildi mi?" tespiti |
| `CompatDatabase` | B | 10 oyuncuk tohum uyumluluk listesi, appid ile sorgu |

UI entegrasyon sırası: ~~1~~ ✅ → ~~5~~ ✅ → ~~C~~ ✅ → ~~A~~ ✅ → ~~B~~ ✅ → ~~D~~ ✅ — TÜM ONAYLANANLAR TAMAM (v3.2)

## 🎯 Değer katan geliştirmeler (onaylandı, sıra: sihirbaz → uyumluluk → güvenlik → Linux)

A. [x] **Kurulum sihirbazı** ✅ v3.3 (5 adım: oyun seç → bileşen → VPN → uygula+çipler → arkadaşla oyna)
- [x] **Mavi'nin Okulu** ✅ v3.3 — sol menüde eğitim bölümü: uygulama tanıtımı, çip rehberi,
  sorun giderme, güvenlik anlatımı, Mavi ipuçları; sihirbaz buradan tekrar başlatılabilir — Program ilk açıldığında 3 adımlık rehber:
   (1) oyun klasörünü seç, (2) bileşen dosyanı bul/getir (nerden bulacağını gösterir),
   (3) VPN'i kur (platforma göre Tailscale/Radmin yönlendirmesi).
   Amaç: yeni kullanıcı 8 buton karşısında şaşırmasın, 2 dakikada kurulum bitsin.

B. [x] **Oyun uyumluluk listesi** ✅ v3.2 (10 oyuncuk tohum liste, çip + log ipucu) — "Hangi oyun LAN'da çalışıyor, hangisi ekstra ayar
   istiyor, hangisi hiç çalışmıyor" listesi. Klasör seçilince anında cevap:
   "LAN destekliyor ✓ / Lobisi yok, IP ile katılım ⚠ / Çalışmıyor ✗ (Denuvo)".
   Liste toplulukla büyür, minimum liste ile başlanır.

C. [x] **Güvenlik ağı** ✅ v3.2 — Yanlışlıkla oyunu bozmayı önleyen üçlü:
   (1) Uygula'dan önce "bu klasörde oyun mu var?" doğrulaması,
   (2) geri yüklemeden önce yedeğin sağlamlık kontrolü (bozuksa dur),
   (3) Steam güncellemesi/bütünlük doğrulaması sonrası yamanın silindiğini algıla
   ve "yama tazelenmeli" uyarısı ver.

D. [x] **Linux'u güçlendirme** ✅ v3.2 (Deck dokunmatik + Heroic + Proton önerisi) — Steam Deck'te dokunmatik kullanılabilirlik (büyük
   butonlar), Heroic/Lutris kurulu oyunları otomatik bulma, Proton sürümü önerisi.
   Amaç: Linux LAN oyunculuğunda rakipsiz konum.

## 📦 Küçük özellikler havuzu (şimdilik gereksiz — rafa kaldırıldı)
Uygula+Başlat tek buton, geri yükleme hatırlatıcısı, son oyunlar listesi, kısayollar,
sistem tepsisi, hatalı IP uyarısı, sorun giderme paketi, güncelleme uyarısı,
onay özeti, dil listesi genişletme. Gerekirse buradan döneriz.

## ⏳ Zamanı geldiğinde
- [ ] **Lobi ağı + senkron başlatma** — çevrimiçi arkadaş listesi, oda sistemi,
  herkesin oyununun aynı anda açılması. Projenin en büyük mimari değişikliği;
  1, 5, A, B, C, D hazır olduktan sonra. Kendi sunucusu gerektirir.
- [ ] **Oyun keşfi** — kurulu oyunları otomatik bulma (Steam/GOG/Heroic/Lutris).
- [ ] **Bileşen paket yöneticisi** — sürümlü, imzalı; kaynaktan derle modeliyle.
- [ ] **Topluluk açılımı** — katkı rehberi, issue şablonları, davetiye kültürü.
- [ ] Steam Deck / Android kumanda, Proton seçimi.

## 🚫 Kırmızı çizgiler (her özellik için geçerli)
- DRM bypass / crack özellikleri ASLA
- DLC kilit aşma ASLA (şablon boş kalır)
- Emülatör bileşeni repodan/asla dağıtılmaz (kullanıcı getirir veya kaynaktan derler)
