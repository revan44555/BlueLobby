using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BlueLobby.Platform
{
    /// <summary>Windows/Linux arasında değişen işlemlerin sözleşmesi.</summary>
    public interface IPlatformServices
    {
        /// <summary>Bu platformda güvenlik duvarı kuralı yönetimi anlamlı mı? (Linux'ta false — Wine/Proton kendi iznini yönetir.)</summary>
        bool SupportsFirewall { get; }

        /// <summary>Verilen exe için gelen trafiğe izin veren, BlueLobby'a ait benzersiz isimli bir kural oluşturur. Başarı durumunu döndürür.</summary>
        Task<bool> EnsureFirewallRuleAsync(string exePath, string ruleName);

        /// <summary>BlueLobby tarafından verilen benzersiz isimdeki güvenlik duvarı kuralını kaldırır. Kural yoksa da başarılı kabul eder.</summary>
        Task<bool> RemoveFirewallRuleAsync(string ruleName);

        /// <summary>Oyun çalıştırılabilir dosyasını başlatır. Windows'ta normal kullanıcı bağlamını tercih eder.</summary>
        Task LaunchGameAsync(string exePath, string gameDir, int? steamAppId = null);

        /// <summary>VPN arayüzü aktif mi? Platforma göre Radmin, Tailscale, ZeroTier, Hamachi, WireGuard veya NetBird.</summary>
        Task<bool> IsVpnActiveAsync();

        /// <summary>Desteklenen VPN arayüzünden doğrulanmış IPv4 adresi; yoksa null.</summary>
        string? GetVpnIpv4();

        /// <summary>Uygulamanın yükseltilmiş yetkiyle çalışıp çalışmadığı. Linux'ta kavram yok (her zaman true).</summary>
        bool IsElevated { get; }

        /// <summary>Verilen çalıştırılabilir dosyaya karşılık gelen süreç şu an çalışıyor mu?</summary>
        Task<bool> IsGameRunningAsync(string exePath);

        /// <summary>VPN istemcisi indirme sayfasının URL'i.</summary>
        string VpnDownloadUrl { get; }

        /// <summary>VPN ağ arabirimi adlarını döndürür (durum çubuğu/banner için).</summary>
        IReadOnlyList<string> GetVpnInterfaceNames();

        /// <summary>Platforma özel veri klasörleri.</summary>
        IPlatformPaths Paths { get; }
    }

    public interface IPlatformPaths
    {
        /// <summary>Ayarlar için gezici veri klasörü (Windows: %AppData%/BlueLobby, Linux: ~/.config/bluelobby).</summary>
        string RoamingDataDir { get; }

        /// <summary>Manifest/log için yerel veri klasörü (Windows: %LocalAppData%/BlueLobby, Linux: ~/.local/share/bluelobby).</summary>
        string LocalDataDir { get; }

        /// <summary>Eski manuel/klasör tabanlı oyun araması için kök adayları.</summary>
        IReadOnlyList<string> GetGameSearchRoots();

        /// <summary>Steam kurulum kökleri. Windows registry ve bilinen dizinler; Linux/SteamOS bilinen XDG/Flatpak yolları.</summary>
        IReadOnlyList<string> GetSteamRoots();
    }
}
