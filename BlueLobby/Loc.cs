using System;
using System.Collections.Generic;

namespace BlueLobby
{
    public static class Loc
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Data = new()
        {
            ["tr"] = new()
            {
                ["title"] = "🎮 BLUELOBBY",
                ["subtitle"] = "Yerel ağ/VPN oturum hazırlık, yedekleme ve geri alma paneli",
                ["warn"] = "Yalnızca sahip olduğunuz oyunlar ve yasal LAN kullanımı içindir.",
                ["banner"] = "⚠️ VPN ağ adaptörü bulunamadı.",
                ["banner_btn"] = "⚡ Standart LAN Ayarlarını Uygula",
                ["radmin_check"] = "🔴 VPN: Kontrol Ediliyor...",
                ["patch_none"] = "🔴 Oyun Durumu: Klasör Seçilmedi",
                ["gamefolder"] = "Oyun Klasörü:",
                ["exe"] = "Çalıştırıcı:",
                ["player"] = "Oyuncu Adı:",
                ["appid"] = "AppID:",
                ["friendip"] = "Arkadaş IP:",
                ["gamelang"] = "Dil:",
                ["dlc"] = "Ek içerik şablonu oluştur",
                ["firewall"] = "Güvenlik Duvarı İzni",
                ["broadcast"] = "VPN Lobi Sabitleme",
                ["friends"] = "👥 Arkadaş Listesi",
                ["fname"] = "İsim:",
                ["fip"] = "IP:",
                ["add"] = "Ekle",
                ["use"] = "Kullan",
                ["remove"] = "Sil",
                ["theme"] = "Tema:",
                ["uilang"] = "Arayüz Dili:",
                ["mascot"] = "Maskotu göster",
                ["apply"] = "⚡ Uygula",
                ["restore"] = "🔄 Geri Yükle",
                ["launch"] = "🚀 Oyunu Başlat",
                ["copy"] = "Kopyala",
                ["export"] = "Dışa Aktar",
                ["clear"] = "Temizle",
                ["tip_drop"] = "Oyun klasörünü seçin ya da pencereye sürükleyip bırakın.",
                ["tip_friendip"] = "VPN IPv4 adresi. Örn: 26.x.x.x",
                ["tip_dlc"] = "Kilit aşma yapmaz. Yasal olarak sahip olduğunuz ek içerik AppID'lerini ekleyeceğiniz şablon oluşturur.",
                ["theme_gece"] = "Gece",
                ["theme_gunduz"] = "Gündüz",
                ["theme_pembe"] = "Pembe",
                ["lang_tr"] = "Türkçe",
                ["lang_en"] = "English",
                ["lang_de"] = "Deutsch",
                ["mascot_tip"] = "Merhaba! Ben Mavi. Takıldığında bana sor. 🤖",
                ["nav_game"] = "🎮  Oyun",
                ["nav_friends"] = "👥  Arkadaşlar",
                ["nav_settings"] = "⚙️  Ayarlar",
                ["nav_log"] = "📋  Gelişmiş / Log",
                ["find_emu"] = "🔍 Yerel Bileşenleri Ara ve Kopyala",
                ["dl_radmin"] = "⬇ VPN İstemcisini İndir",
                ["emu_ok"] = "🟢 Bileşen: hazır",
                ["emu_missing"] = "🟡 Bileşen: bekleniyor — aşağıdaki \"Ara ve Kopyala\"ya bas",
            },
            ["en"] = new()
            {
                ["title"] = "🎮 BLUELOBBY",
                ["subtitle"] = "Local network/VPN session preparation, backup and restore panel",
                ["warn"] = "For games you own and legal LAN use only.",
                ["banner"] = "⚠️ VPN network adapter not found.",
                ["banner_btn"] = "⚡ Apply Standard LAN Settings",
                ["radmin_check"] = "🔴 VPN: Checking...",
                ["patch_none"] = "🔴 Game status: No folder selected",
                ["gamefolder"] = "Game Folder:",
                ["exe"] = "Executable:",
                ["player"] = "Player Name:",
                ["appid"] = "AppID:",
                ["friendip"] = "Friend IP:",
                ["gamelang"] = "Language:",
                ["dlc"] = "Create extra-content template",
                ["firewall"] = "Firewall Permission",
                ["broadcast"] = "VPN Lobby Pin",
                ["friends"] = "👥 Friend List",
                ["fname"] = "Name:",
                ["fip"] = "IP:",
                ["add"] = "Add",
                ["use"] = "Use",
                ["remove"] = "Remove",
                ["theme"] = "Theme:",
                ["uilang"] = "UI Language:",
                ["mascot"] = "Show mascot",
                ["apply"] = "⚡ Apply",
                ["restore"] = "🔄 Restore",
                ["launch"] = "🚀 Launch Game",
                ["copy"] = "Copy",
                ["export"] = "Export",
                ["clear"] = "Clear",
                ["tip_drop"] = "Select the game folder or drag && drop it onto the window.",
                ["tip_friendip"] = "VPN IPv4 address. E.g. 26.x.x.x",
                ["tip_dlc"] = "Does not bypass locks. Creates a template for extra-content AppIDs you legally own.",
                ["theme_gece"] = "Night",
                ["theme_gunduz"] = "Day",
                ["theme_pembe"] = "Pink",
                ["lang_tr"] = "Türkçe",
                ["lang_en"] = "English",
                ["lang_de"] = "Deutsch",
                ["mascot_tip"] = "Hi! I'm Mavi. Ask me if you get stuck. 🤖",
                ["nav_game"] = "🎮  Game",
                ["nav_friends"] = "👥  Friends",
                ["nav_settings"] = "⚙️  Settings",
                ["nav_log"] = "📋  Advanced / Log",
                ["find_emu"] = "🔍 Search && Copy Local Components",
                ["dl_radmin"] = "⬇ Download VPN Client",
                ["emu_ok"] = "🟢 Component: ready",
                ["emu_missing"] = "🟡 Component: waiting — use \"Search && Copy\" below",
            },
            ["de"] = new()
            {
                ["title"] = "🎮 BLUELOBBY",
                ["subtitle"] = "Lokales Netzwerk/VPN-Sitzungsvorbereitungs-, Backup- und Wiederherstellungspanel",
                ["warn"] = "Nur für Spiele, die Sie besitzen, und legale LAN-Nutzung.",
                ["banner"] = "⚠️ VPN-Netzwerkadapter nicht gefunden.",
                ["banner_btn"] = "⚡ Standard-LAN-Einstellungen anwenden",
                ["radmin_check"] = "🔴 VPN: Wird geprüft...",
                ["patch_none"] = "🔴 Spielstatus: Kein Ordner ausgewählt",
                ["gamefolder"] = "Spielordner:",
                ["exe"] = "Programmdatei:",
                ["player"] = "Spielername:",
                ["appid"] = "AppID:",
                ["friendip"] = "Freund-IP:",
                ["gamelang"] = "Sprache:",
                ["dlc"] = "Zusatzinhalt-Vorlage erstellen",
                ["firewall"] = "Firewall-Erlaubnis",
                ["broadcast"] = "VPN-Lobby-Anheftung",
                ["friends"] = "👥 Freundesliste",
                ["fname"] = "Name:",
                ["fip"] = "IP:",
                ["add"] = "Hinzufügen",
                ["use"] = "Übernehmen",
                ["remove"] = "Entfernen",
                ["theme"] = "Thema:",
                ["uilang"] = "Oberflächensprache:",
                ["mascot"] = "Maskottchen anzeigen",
                ["apply"] = "⚡ Anwenden",
                ["restore"] = "🔄 Wiederherstellen",
                ["launch"] = "🚀 Spiel starten",
                ["copy"] = "Kopieren",
                ["export"] = "Exportieren",
                ["clear"] = "Leeren",
                ["tip_drop"] = "Spielordner auswählen oder per Drag && Drop auf das Fenster ziehen.",
                ["tip_friendip"] = "VPN-IPv4-Adresse. Z. B. 26.x.x.x",
                ["tip_dlc"] = "Umgeht keine Sperren. Erstellt eine Vorlage für Zusatzinhalt-AppIDs, die Sie besitzen.",
                ["theme_gece"] = "Nacht",
                ["theme_gunduz"] = "Tag",
                ["theme_pembe"] = "Rosa",
                ["lang_tr"] = "Türkçe",
                ["lang_en"] = "English",
                ["lang_de"] = "Deutsch",
                ["mascot_tip"] = "Hallo! Ich bin Mavi. Frag mich, wenn du nicht weiterkommst. 🤖",
                ["nav_game"] = "🎮  Spiel",
                ["nav_friends"] = "👥  Freunde",
                ["nav_settings"] = "⚙️  Einstellungen",
                ["nav_log"] = "📋  Erweitert / Log",
                ["find_emu"] = "🔍 Lokale Komponenten suchen && kopieren",
                ["dl_radmin"] = "⬇ VPN-Client laden",
                ["emu_ok"] = "🟢 Komponente: bereit",
                ["emu_missing"] = "🟡 Komponente: wartet — unten \"Suchen && Kopieren\" nutzen",
            },
        };


        public static void SetLanguage(string lang)
        {
            if (Data.ContainsKey(lang))
            {
                Current = lang;
            }
        }

        public static string T(string key)
        {
            if (Data.TryGetValue(Current, out var dict) && dict.TryGetValue(key, out var text))
            {
                return text;
            }

            return key;
        }

        
        public static string Current { get; private set; } = "tr";

        static Loc()
        {
            void Add(string lang, string key, string value) => Data[lang][key] = value;
            Add("tr", "ping_btn", "🌐 Ping Testi");
            Add("en", "ping_btn", "🌐 Ping Test");
            Add("de", "ping_btn", "🌐 Ping-Test");
            Add("tr", "autoscan", "⚡ Standart LAN Ayarlarını Uygula");
            Add("en", "autoscan", "⚡ Apply Standard LAN Settings");
            Add("de", "autoscan", "⚡ Standard-LAN-Einstellungen anwenden");
            Add("tr", "fullscreen", "⛶ Tam Ekran (F11)");
            Add("en", "fullscreen", "⛶ Fullscreen (F11)");
            Add("de", "fullscreen", "⛶ Vollbild (F11)");
            Add("tr", "restore_q", "Geri yükleme onayı");
            Add("en", "restore_q", "Restore confirmation");
            Add("de", "restore_q", "Wiederherstellung bestätigen");
            Add("tr", "yes", "Evet");
            Add("en", "yes", "Yes");
            Add("de", "yes", "Ja");
            Add("tr", "no", "Hayır");
            Add("en", "no", "No");
            Add("de", "no", "Nein");
            Add("tr", "friend_used", "Arkadaş IP olarak kullanıldı");
            Add("en", "friend_used", "Used as friend IP");
            Add("de", "friend_used", "Als Freundes-IP übernommen");
            Add("tr", "nav_learn", "🎓 Mavi'nin Okulu");
            Add("en", "nav_learn", "🎓 Mavi's School");
            Add("de", "nav_learn", "🎓 Mavis Schule");
        }
    }
}
