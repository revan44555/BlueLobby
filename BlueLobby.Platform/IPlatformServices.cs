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

        /// <summary>Verilen exe için gelen trafiğe izin veren bir kural oluşturur; kural adını döndürür. Başarısızsa null.</summary>
        Task<string?> EnsureFirewallRuleAsync(string exePath);

        /// <summary>İsimle eşleşen güvenlik duvarı kuralını kaldırır.</summary>
        Task<bool> RemoveFirewallRuleAsync(string ruleName);

        /// <summary>Oyun çalıştırılabilir dosyasını başlatır. Windows'ta normal kullanıcı bağlamını tercih eder.</summary>
        Task LaunchGameAsync(string exePath, string gameDir);

        /// <summary>VPN istemcisi aktif mi? (Windows: Radmin; Linux: Tailscale/ZeroTier)</summary>
        Task<bool> IsVpnActiveAsync();

        /// <summary>VPN ağ arabiriminden IPv4 adresi; yoksa null. (Windows: 26.x Radmin; Linux: Tailscale 100.x / ZeroTier 10.x)</summary>
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

        /// <summary>Oyun araması için kök adayları (Windows: Program Files x2; Linux: ~/.steam, ~/.local/share/Steam vb.).</summary>
        IReadOnlyList<string> GetGameSearchRoots();
    }
}
