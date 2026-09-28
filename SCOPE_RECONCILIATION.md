# BlueLobby 4.2.0 — Windows/Linux scope reconciliation

Bu sürüm yeniden gözden geçirilerek aktif ürün kapsamı Windows x64 + Linux x64 olarak sabitlendi.

## Geri alınan / düzeltilen gereksiz etkiler
- Mobil/Android/iOS hedeflerini ima eden dokümantasyon kaldırıldı.
- Kompakt navigation artık mobil uygulama modeli olarak tanımlanmıyor; dar masaüstü penceresi ve Steam Deck erişilebilirliği olarak ele alınıyor.
- Orijinal masaüstü minimum pencere ölçüsü (860x600) geri getirildi.
- Compact navigation'ın yerleşimi v4.0'daki masaüstü sırasına geri getirildi.

## Korunan altyapı
- Transactional patch engine
- SHA-256 integrity
- Backup/restore/recovery
- Bounded + architecture-aware DLL resolution
- Windows/Linux platform abstraction
- Steam/Heroic/Lutris/GOG bounded discovery
- Manuel klasör seçimi ve drag & drop
- Firewall ownership
- Cross-process game lock
- MainViewModel discovery foundation

## Ek doğrulama
`Tests/Program.cs` içine yarım kalmış `Preparing` transaction'ının gerçek dosya yedeği üzerinden geri kazanıldığını doğrulayan bir integration senaryosu eklendi.

## Doğrulama sınırı
Bu çalışma ortamında .NET 10 SDK/compiler olmadığı için gerçek `dotnet restore/build/test` çalıştırılamadı. Statik XML/reference/C# structural kontrolleri yapıldı; CI gerçek derleme ve test için kaynak doğrulama katmanıdır.
