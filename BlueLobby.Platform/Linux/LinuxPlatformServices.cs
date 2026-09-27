using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace BlueLobby.Platform.Linux
{
    public sealed class LinuxPlatformServices : IPlatformServices
    {
        private sealed class LinuxPaths : IPlatformPaths
        {
            private static string Home => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            // XDG Base Directory spesifikasyonu
            public string RoamingDataDir =>
                Path.Combine(XdgDir("XDG_CONFIG_HOME", Path.Combine(Home, ".config")), "bluelobby");

            public string LocalDataDir =>
                Path.Combine(XdgDir("XDG_DATA_HOME", Path.Combine(Home, ".local", "share")), "bluelobby");

            private static string XdgDir(string variable, string fallback)
            {
                string? value = Environment.GetEnvironmentVariable(variable);
                return string.IsNullOrWhiteSpace(value) ? fallback : value;
            }

            public IReadOnlyList<string> GetGameSearchRoots()
            {
                string home = Home;
                var roots = new List<string>
                {
                    Path.Combine(home, ".steam", "steam", "steamapps", "common"),
                    Path.Combine(home, ".local", "share", "Steam", "steamapps", "common"),
                    Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam", "steamapps", "common"), // Flatpak
                    Path.Combine(home, "GOG Games"),
                    Path.Combine(home, "Games"),
                };
                return roots;
            }
        }

        private static readonly string[] VpnInterfaceKeywords = { "tailscale", "zerotier", "zt", "hamachi", "wireguard", "wg" };

        private readonly LinuxPaths _paths = new();

        // Linux'ta Wine/Proton ağ erişimini kendi başına yönetir; kullanıcıya sudo ile kural eklettirmek gereksiz.
        public bool SupportsFirewall => false;
        public string VpnDownloadUrl => "https://tailscale.com/download/linux";

        // Linux'ta sudo gerektiren işlem yok (firewall kapalı).
        public bool IsElevated => true;

        public Task<bool> IsGameRunningAsync(string exePath)
        {
            string target = Path.GetFileNameWithoutExtension(exePath).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(target))
            {
                return Task.FromResult(false);
            }

            // /proc/<pid>/comm üzerinden süreç adı eşleşmesi (pgrep'e bağımlılık yok).
            bool found = false;
            try
            {
                foreach (string procDir in Directory.EnumerateDirectories("/proc"))
                {
                    string pid = Path.GetFileName(procDir);
                    if (!pid.All(char.IsDigit))
                    {
                        continue;
                    }

                    try
                    {
                        string comm = File.ReadAllText(Path.Combine(procDir, "comm")).Trim().ToLowerInvariant();
                        if (comm == target)
                        {
                            found = true;
                            break;
                        }
                    }
                    catch
                    {
                        // Süreç listelenirken bitti: yok say.
                    }
                }
            }
            catch
            {
                // /proc erişilemez: güvenli varsayılan.
            }

            return Task.FromResult(found);
        }
        public IPlatformPaths Paths => _paths;

        public Task<string?> EnsureFirewallRuleAsync(string exePath) => Task.FromResult<string?>(null);

        public Task<bool> RemoveFirewallRuleAsync(string ruleName) => Task.FromResult(true);

        public Task LaunchGameAsync(string exePath, string gameDir)
        {
            // Linux'ta doğrudan başlat; gerekirse xdg-open ile sistem varsayılanı kullanılır.
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? gameDir,
                    UseShellExecute = true
                });
            }
            catch
            {
                Process.Start(new ProcessStartInfo { FileName = "xdg-open", Arguments = $"\"{exePath}\"", UseShellExecute = false });
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsVpnActiveAsync()
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(GetVpnIpv4()));
        }

        public string? GetVpnIpv4()
        {
            foreach (string ipv4 in EnumerateVpnIpv4())
            {
                // Tailscale 100.64.0.0/10, ZeroTier 10.x, Hamachi 25.x/5.x blokları
                if (ipv4.StartsWith("100.", StringComparison.Ordinal) ||
                    ipv4.StartsWith("10.", StringComparison.Ordinal) ||
                    ipv4.StartsWith("25.", StringComparison.Ordinal))
                {
                    return ipv4;
                }
            }
            return null;
        }

        public IReadOnlyList<string> GetVpnInterfaceNames()
        {
            var names = new List<string>();
            foreach (NetworkInterface nic in SafeGetInterfaces())
            {
                string text = $"{nic.Name} {nic.Description}".ToLowerInvariant();
                foreach (string keyword in VpnInterfaceKeywords)
                {
                    if (text.Contains(keyword))
                    {
                        names.Add(nic.Name);
                        break;
                    }
                }
            }
            return names;
        }

        private static IEnumerable<string> EnumerateVpnIpv4()
        {
            foreach (NetworkInterface nic in SafeGetInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                string text = $"{nic.Name} {nic.Description}".ToLowerInvariant();
                bool isVpn = false;
                foreach (string keyword in VpnInterfaceKeywords)
                {
                    if (text.Contains(keyword))
                    {
                        isVpn = true;
                        break;
                    }
                }
                if (!isVpn)
                {
                    continue;
                }

                foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                    {
                        yield return ip.Address.ToString();
                    }
                }
            }
        }

        private static NetworkInterface[] SafeGetInterfaces()
        {
            try
            {
                return NetworkInterface.GetAllNetworkInterfaces();
            }
            catch
            {
                return Array.Empty<NetworkInterface>();
            }
        }
    }
}
