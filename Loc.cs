using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

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

        public static string Current { get; private set; } = "tr";

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

        public static void Apply(Window window)
        {
            void Set(string name, string key)
            {
                if (window.FindName(name) is FrameworkElement el)
                {
                    switch (el)
                    {
                        case TextBlock tb: tb.Text = T(key); break;
                        case Button b: b.Content = T(key); break;
                        case CheckBox cb: cb.Content = T(key); break;
                    }
                }
            }

            Set("TxtTitle", "title");
            Set("TxtSubtitle", "subtitle");
            Set("TxtWarn", "warn");
            Set("BannerText", "banner");
            Set("BtnAutoSetupBanner", "banner_btn");
            Set("TxtRadminStatus", "radmin_check");
            Set("LblGameFolder", "gamefolder");
            Set("LblExe", "exe");
            Set("LblPlayerName", "player");
            Set("LblAppId", "appid");
            Set("LblFriendIp", "friendip");
            Set("LblGameLang", "gamelang");
            Set("ChkUnlockDlc", "dlc");
            Set("ChkFirewall", "firewall");
            Set("ChkCustomBroadcast", "broadcast");
            Set("LblFriends", "friends");
            Set("LblFriendName", "fname");
            Set("LblFriendIp2", "fip");
            Set("BtnAddFriend", "add");
            Set("BtnUseFriend", "use");
            Set("BtnRemoveFriend", "remove");
            Set("LblTheme", "theme");
            Set("LblUiLang", "uilang");
            Set("ChkMascot", "mascot");
            Set("BtnApply", "apply");
            Set("BtnRestore", "restore");
            Set("BtnLaunch", "launch");
            Set("BtnCopyLog", "copy");
            Set("BtnExportLog", "export");
            Set("BtnClearLog", "clear");
            Set("BtnThemeGece", "theme_gece");
            Set("BtnThemeGunduz", "theme_gunduz");
            Set("BtnThemePembe", "theme_pembe");
            Set("BtnUiTr", "lang_tr");
            Set("BtnUiEn", "lang_en");
            Set("BtnUiDe", "lang_de");

            if (window.FindName("TxtGamePath") is TextBox gamePath)
            {
                gamePath.ToolTip = T("tip_drop");
            }

            if (window.FindName("TxtFriendIp") is TextBox friendIp)
            {
                friendIp.ToolTip = T("tip_friendip");
            }

            if (window.FindName("ChkUnlockDlc") is CheckBox dlc)
            {
                dlc.ToolTip = T("tip_dlc");
            }

            Set("NavOyun", "nav_game");
            Set("NavFriends", "nav_friends");
            Set("NavSettings", "nav_settings");
            Set("NavLog", "nav_log");
            Set("BtnDownloadRadmin", "dl_radmin");
            Set("BtnFindEmulator", "find_emu");

            if (window.FindName("TxtMascot") is TextBlock mascot)
            {
                mascot.ToolTip = T("mascot_tip");
            }

            if (window.FindName("TxtPatchStatus") is TextBlock patchStatus && patchStatus.Tag == null)
            {
                patchStatus.Text = T("patch_none");
            }
        }
    }
}
