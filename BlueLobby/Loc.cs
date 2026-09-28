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
            Add("tr", "discover_games", "🔎 Kurulu Oyunları Tara");
            Add("en", "discover_games", "🔎 Discover Installed Games");
            Add("de", "discover_games", "🔎 Installierte Spiele suchen");
            Add("tr", "touch_mode", "🖐 Dokunmatik mod (Steam Deck)");
            Add("en", "touch_mode", "🖐 Touch mode (Steam Deck)");
            Add("de", "touch_mode", "🖐 Touch-Modus (Steam Deck)");
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
            Add("tr", "brand_eyebrow", "BLUELOBBY MASAÜSTÜ");
            Add("en", "brand_eyebrow", "BLUELOBBY DESKTOP");
            Add("de", "brand_eyebrow", "BLUELOBBY DESKTOP");
            Add("tr", "nav_workspace", "ÇALIŞMA ALANI");
            Add("en", "nav_workspace", "WORKSPACE");
            Add("de", "nav_workspace", "ARBEITSBEREICH");
            Add("tr", "nav_tools", "ARAÇLAR");
            Add("en", "nav_tools", "TOOLS");
            Add("de", "nav_tools", "WERKZEUGE");
            Add("tr", "status_ready", "Hazır");
            Add("en", "status_ready", "Ready");
            Add("de", "status_ready", "Bereit");
            Add("tr", "page_game_title", "Oyun");
            Add("en", "page_game_title", "Game");
            Add("de", "page_game_title", "Spiel");
            Add("tr", "page_game_subtitle", "Oyunu seç, durumu kontrol et ve tek yerden yönet.");
            Add("en", "page_game_subtitle", "Select the game, check its state, and manage it from one place.");
            Add("de", "page_game_subtitle", "Spiel auswählen, Status prüfen und alles an einem Ort verwalten.");
            Add("tr", "page_friends_title", "Arkadaşlar");
            Add("en", "page_friends_title", "Friends");
            Add("de", "page_friends_title", "Freunde");
            Add("tr", "page_friends_subtitle", "LAN/VPN eşleşmeleri ve kayıtlı oyuncular.");
            Add("en", "page_friends_subtitle", "Saved players and LAN/VPN connections.");
            Add("de", "page_friends_subtitle", "Gespeicherte Spieler und LAN/VPN-Verbindungen.");
            Add("tr", "page_settings_title", "Ayarlar");
            Add("en", "page_settings_title", "Settings");
            Add("de", "page_settings_title", "Einstellungen");
            Add("tr", "page_settings_subtitle", "Tema, dil ve masaüstü davranışını düzenle.");
            Add("en", "page_settings_subtitle", "Configure theme, language, and desktop behavior.");
            Add("de", "page_settings_subtitle", "Theme, Sprache und Desktop-Verhalten konfigurieren.");
            Add("tr", "page_log_title", "Gelişmiş / Log");
            Add("en", "page_log_title", "Advanced / Log");
            Add("de", "page_log_title", "Erweitert / Log");
            Add("tr", "page_log_subtitle", "Yaptığın işlemlerin teknik kaydı ve dışa aktarma.");
            Add("en", "page_log_subtitle", "Technical activity history and export tools.");
            Add("de", "page_log_subtitle", "Technische Aktivitätshistorie und Exportwerkzeuge.");
            Add("tr", "page_wizard_title", "Kurulum");
            Add("en", "page_wizard_title", "Setup");
            Add("de", "page_wizard_title", "Einrichtung");
            Add("tr", "page_wizard_subtitle", "BlueLobby'yi hazırlamak için adım adım rehber.");
            Add("en", "page_wizard_subtitle", "A step-by-step guide to prepare BlueLobby.");
            Add("de", "page_wizard_subtitle", "Schritt-für-Schritt-Assistent zur Einrichtung von BlueLobby.");
            Add("tr", "page_learn_title", "Mavi'nin Okulu");
            Add("en", "page_learn_title", "Mavi's School");
            Add("de", "page_learn_title", "Mavis Schule");
            Add("tr", "page_learn_subtitle", "Uygulamanın ne yaptığını ve güvenli kullanımını öğren.");
            Add("en", "page_learn_subtitle", "Learn what the app does and how to use it safely.");
            Add("de", "page_learn_subtitle", "Lerne, was die App tut und wie du sie sicher nutzt.");
            Add("tr", "section_game_setup", "Oyun kurulumu");
            Add("en", "section_game_setup", "Game setup");
            Add("de", "section_game_setup", "Spieleinrichtung");
            Add("tr", "section_installed_games", "Kurulu oyunlar");
            Add("en", "section_installed_games", "Installed games");
            Add("de", "section_installed_games", "Installierte Spiele");
            Add("tr", "section_session", "Oturum ayarları");
            Add("en", "section_session", "Session settings");
            Add("de", "section_session", "Sitzungseinstellungen");
            Add("tr", "section_main_actions", "Ana işlemler");
            Add("en", "section_main_actions", "Main actions");
            Add("de", "section_main_actions", "Hauptaktionen");
            Add("tr", "section_activity", "İşlem kaydı");
            Add("en", "section_activity", "Activity log");
            Add("de", "section_activity", "Aktivitätsprotokoll");
            Add("tr", "activity_subtitle", "Patch, restore, discovery ve sistem kontrollerinin zaman çizelgesi.");
            Add("en", "activity_subtitle", "Timeline of patch, restore, discovery, and system checks.");
            Add("de", "activity_subtitle", "Zeitachse für Patch, Restore, Discovery und Systemprüfungen.");
            Add("tr", "discover_subtitle", "Steam, Heroic ve Lutris kayıtlarından güvenli keşif.");
            Add("en", "discover_subtitle", "Safe discovery from Steam, Heroic, and Lutris metadata.");
            Add("de", "discover_subtitle", "Sichere Erkennung über Steam-, Heroic- und Lutris-Metadaten.");
            Add("tr", "setup_subtitle", "Dosyalar değiştirileceğinde önce yedek ve bütünlük kontrolü yapılır.");
            Add("en", "setup_subtitle", "Backups and integrity checks are performed before files are changed.");
            Add("de", "setup_subtitle", "Vor Änderungen werden Backups und Integritätsprüfungen durchgeführt.");
            Add("tr", "game_manage_subtitle", "Oyun klasörünü seç, kurulu oyunlardan keşfet veya kayıtlı bir kurulumu aç.");
            Add("en", "game_manage_subtitle", "Select a game folder, discover installed games, or open a saved installation.");
            Add("de", "game_manage_subtitle", "Spielordner wählen, installierte Spiele erkennen oder eine gespeicherte Installation öffnen.");
            Add("tr", "status_patch", "Yama");
            Add("en", "status_patch", "Patch");
            Add("de", "status_patch", "Patch");
            Add("tr", "status_patch_detail", "Hedef / transaction durumu");
            Add("en", "status_patch_detail", "Target / transaction state");
            Add("de", "status_patch_detail", "Ziel- / Transaktionsstatus");
            Add("tr", "status_component", "Bileşen");
            Add("en", "status_component", "Component");
            Add("de", "status_component", "Komponente");
            Add("tr", "status_component_detail", "Yerel bileşen dosyaları");
            Add("en", "status_component_detail", "Local component files");
            Add("de", "status_component_detail", "Lokale Komponentdateien");
            Add("tr", "status_compat", "Uyumluluk");
            Add("en", "status_compat", "Compatibility");
            Add("de", "status_compat", "Kompatibilität");
            Add("tr", "status_compat_detail", "AppID tabanlı kontrol");
            Add("en", "status_compat_detail", "AppID-based check");
            Add("de", "status_compat_detail", "AppID-basierte Prüfung");
            Add("tr", "status_ping", "Ping");
            Add("en", "status_ping", "Ping");
            Add("de", "status_ping", "Ping");
            Add("tr", "status_ping_detail", "Arkadaş ağı gecikmesi");
            Add("en", "status_ping_detail", "Friend network latency");
            Add("de", "status_ping_detail", "Latenz zur Freund-Verbindung");
            Add("tr", "status_vpn", "VPN");
            Add("en", "status_vpn", "VPN");
            Add("de", "status_vpn", "VPN");
            Add("tr", "status_vpn_detail", "Yerel/VPN adaptörü");
            Add("en", "status_vpn_detail", "Local/VPN adapter");
            Add("de", "status_vpn_detail", "Lokaler/VPN-Adapter");
            Add("tr", "session_subtitle", "Sık kullanılan alanlar burada; daha nadir seçenekler aşağıdaki gelişmiş bölümde.");
            Add("en", "session_subtitle", "Keep common session values here; rarer options are below in Advanced.");
            Add("de", "session_subtitle", "Häufige Sitzungswerte hier; seltenere Optionen unten unter Erweitert.");
            Add("tr", "advanced_options", "Gelişmiş seçenekler");
            Add("en", "advanced_options", "Advanced options");
            Add("de", "advanced_options", "Erweiterte Optionen");
            Add("tr", "advanced_subtitle", "Bu seçenekleri yalnızca ne yaptığını biliyorsan değiştir.");
            Add("en", "advanced_subtitle", "Change these only when you know what they do.");
            Add("de", "advanced_subtitle", "Nur ändern, wenn die Funktion klar ist.");
            Add("tr", "tools", "Yardımcı araçlar");
            Add("en", "tools", "Utility tools");
            Add("de", "tools", "Hilfswerkzeuge");
            Add("tr", "tools_subtitle", "Durum kontrolü, bileşen arama ve profil paylaşımı gibi yardımcı işlemler.");
            Add("en", "tools_subtitle", "Supporting actions such as status checks, component search, and profile sharing.");
            Add("de", "tools_subtitle", "Hilfsaktionen wie Statusprüfung, Komponentensuche und Profilfreigabe.");
            Add("tr", "actions_subtitle", "Önce durumu kontrol et; ardından ana işlemi seç.");
            Add("en", "actions_subtitle", "Check the state first, then choose the main action.");
            Add("de", "actions_subtitle", "Status prüfen und danach die Hauptaktion wählen.");
            Add("tr", "share_profile", "Profili Paylaş");
            Add("en", "share_profile", "Share Profile");
            Add("de", "share_profile", "Profil teilen");
            Add("tr", "import_profile", "Profil Al");
            Add("en", "import_profile", "Import Profile");
            Add("de", "import_profile", "Profil importieren");
            Add("tr", "active_target", "AKTİF OYUN HEDEFİ");
            Add("en", "active_target", "ACTIVE GAME TARGET");
            Add("de", "active_target", "AKTIVES SPIELZIEL");
            Add("tr", "game_target_empty", "Henüz bir oyun klasörü seçilmedi.");
            Add("en", "game_target_empty", "No game folder selected yet.");
            Add("de", "game_target_empty", "Noch kein Spielordner gewählt.");
            Add("tr", "setup_wizard_again", "Kurulum sihirbazını tekrar göster");
            Add("en", "setup_wizard_again", "Show setup wizard again");
            Add("de", "setup_wizard_again", "Einrichtungsassistent erneut zeigen");
            Add("de", "game_manage_subtitle", "Ordner wählen, installierte Spiele erkennen oder gespeicherte Einrichtung öffnen.");
        }
    }
}
