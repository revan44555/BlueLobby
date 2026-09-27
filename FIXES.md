# Teknik değişiklik özeti

Bu dosya, ayrıntılı geçmiş kayıtların nötr özetidir. Kod davranışı değiştirilmemiştir; yalnızca kullanıcıya görünen metinler ve proje tanıtım dili sadeleştirilmiştir.

- UI/çeviri metinleri yerel ağ/VPN oturum hazırlığı, yedekleme ve geri yükleme diliyle uyumlu hale getirildi.
- Oyun kartı örnekleri jenerik isimlere çevrildi; marka/oyun adları kaldırıldı.
- README ve yayın/CI görünür metinleri nötrleştirildi.
- Ürün açıklaması yerel ağ/VPN yedekleme paneli olacak şekilde sadeleştirildi.
- Geri alma manifesti, hedef doğrulama, yedekleme, firewall kuralı ekleme/geri alma, tema/dil/maskot, güncelleme kontrolü ve test altyapısı korundu.

Yerel doğrulama: `dotnet build -c Release -p:EnableWindowsTargeting=true` hedefi korunur; testler 7/7 PASS beklentisi korunur.


## BlueLobby rename
- Proje/namespace/assembly adı `BlueLobby` yapıldı.
- Ana proje dosyası `BlueLobby.csproj`, test projesi `BlueLobby.Tests` olarak güncellendi.
- CI workflow yolları ve EXE adı `BlueLobby.exe` olacak şekilde güncellendi.
- Görünür başlıklar ve kullanıcıya dönük metinler `BlueLobby` diline çekildi.
- Fonksiyonel hedef dosya adları (`steam_api*.dll`, `steam_appid.txt`, `steam_settings`) değiştirilmedi.
