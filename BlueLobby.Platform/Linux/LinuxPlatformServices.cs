using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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

            public IReadOnlyList<string> GetSteamRoots()
            {
                string home = Home;
                return new[]
                {
                    Path.Combine(home, ".steam", "steam"),
                    Path.Combine(home, ".local", "share", "Steam"),
                    Path.Combine(home, ".var", "app", "com.valvesoftware.Steam", ".local", "share", "Steam"),
                };
            }
        }

        private static readonly string[] VpnInterfaceKeywords = { "tailscale", "zerotier", "hamachi", "wireguard", "netbird" };

        private readonly LinuxPaths _paths = new();

        // Linux'ta Wine/Proton ağ erişimini kendi başına yönetir; kullanıcıya sudo ile kural eklettirmek gereksiz.
        public bool SupportsFirewall => false;
        public string VpnDownloadUrl => "https://tailscale.com/download/linux";

        // Linux'ta sudo gerektiren işlem yok (firewall kapalı).
        public bool IsElevated => true;

        public Task<bool> IsGameRunningAsync(string exePath)
        {
            string target = Path.GetFileName(exePath);
            string expected = Path.GetFullPath(exePath);
            if (string.IsNullOrWhiteSpace(target)) return Task.FromResult(false);

            try
            {
                foreach (string procDir in Directory.EnumerateDirectories("/proc"))
                {
                    string pid = Path.GetFileName(procDir);
                    if (!pid.All(char.IsDigit)) continue;

                    try
                    {
                        string comm = File.ReadAllText(Path.Combine(procDir, "comm")).Trim();
                        if (!string.Equals(comm, Path.GetFileNameWithoutExtension(target), StringComparison.OrdinalIgnoreCase) &&
                            !string.Equals(comm, target, StringComparison.OrdinalIgnoreCase)) continue;

                        string exeLink = Path.Combine(procDir, "exe");
                        FileInfo info = new(exeLink);
                        FileSystemInfo? resolved = info.ResolveLinkTarget(true);
                        if (resolved != null && string.Equals(Path.GetFullPath(resolved.FullName), expected, StringComparison.Ordinal))
                            return Task.FromResult(true);
                    }
                    catch
                    {
                        // Process yok olabilir veya /proc erişimi kısıtlı olabilir.
                    }
                }
            }
            catch
            {
                // /proc erişilemezse güvenli varsayılan.
            }

            return Task.FromResult(false);
        }
        public IPlatformPaths Paths => _paths;

        public Task<bool> EnsureFirewallRuleAsync(string exePath, string ruleName) => Task.FromResult(false);

        public Task<bool> RemoveFirewallRuleAsync(string ruleName) => Task.FromResult(true);

        public Task LaunchGameAsync(string exePath, string gameDir, int? steamAppId = null)
        {
            // Proton oyunları Linux'tan Windows .exe'sini doğrudan çalıştırmak yerine
            // Steam'e AppID üzerinden bırakılmalı; böylece Steam doğru Proton sürümünü,
            // compatdata prefix'ini ve launch environment'ını kurar.
            if (steamAppId is > 0 && IsWindowsExecutable(exePath) && TryLaunchViaSteam(steamAppId.Value))
                return Task.CompletedTask;

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

        private static bool IsWindowsExecutable(string path) =>
            string.Equals(Path.GetExtension(path), ".exe", StringComparison.OrdinalIgnoreCase);

        private static bool TryLaunchViaSteam(int appId)
        {
            foreach (string command in new[] { "steam" })
            {
                try
                {
                    Process? process = Process.Start(new ProcessStartInfo
                    {
                        FileName = command,
                        ArgumentList = { "-applaunch", appId.ToString(System.Globalization.CultureInfo.InvariantCulture) },
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    });
                    if (process != null) return true;
                }
                catch (System.ComponentModel.Win32Exception)
                {
                    // Komut PATH'te yok; bir sonraki aday denenir.
                }
                catch
                {
                    return false;
                }
            }
            return false;
        }

        public Task<bool> IsVpnActiveAsync()
        {
            return Task.FromResult(!string.IsNullOrWhiteSpace(GetVpnIpv4()));
        }

        public string? GetVpnIpv4()
        {
            foreach ((string ipv4, string interfaceText) in EnumerateVpnIpv4())
                if (IsSupportedVpnIpv4(ipv4, interfaceText)) return ipv4;
            return null;
        }

        public IReadOnlyList<string> GetVpnInterfaceNames()
        {
            var names = new List<string>();
            foreach (NetworkInterface nic in SafeGetInterfaces())
                if (IsVpnInterface(nic)) names.Add(nic.Name);
            return names;
        }

        private static IEnumerable<(string Ipv4, string InterfaceText)> EnumerateVpnIpv4()
        {
            foreach (NetworkInterface nic in SafeGetInterfaces())
            {
                if (nic.OperationalStatus != OperationalStatus.Up || !IsVpnInterface(nic)) continue;
                string text = $"{nic.Name} {nic.Description}".ToLowerInvariant();
                foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                    if (ip.Address.AddressFamily == AddressFamily.InterNetwork) yield return (ip.Address.ToString(), text);
            }
        }

        private static bool IsVpnInterface(NetworkInterface nic)
        {
            string name = nic.Name.ToLowerInvariant();
            string text = $"{name} {nic.Description}".ToLowerInvariant();
            return text.Contains("tailscale", StringComparison.Ordinal) ||
                   text.Contains("zerotier", StringComparison.Ordinal) ||
                   name.StartsWith("zt", StringComparison.Ordinal) ||
                   text.Contains("hamachi", StringComparison.Ordinal) ||
                   text.Contains("wireguard", StringComparison.Ordinal) ||
                   name.StartsWith("wg", StringComparison.Ordinal) ||
                   text.Contains("netbird", StringComparison.Ordinal) ||
                   name.StartsWith("wt", StringComparison.Ordinal);
        }

        private static bool IsSupportedVpnIpv4(string ipv4, string interfaceText)
        {
            if (!IPAddress.TryParse(ipv4, out IPAddress? address) || address.AddressFamily != AddressFamily.InterNetwork) return false;
            byte[] b=address.GetAddressBytes();
            bool tailscale=b[0]==100 && b[1]>=64 && b[1]<=127;
            bool privateRange=b[0]==10 || (b[0]==172 && b[1]>=16 && b[1]<=31) || (b[0]==192 && b[1]==168);
            if(interfaceText.Contains("tailscale",StringComparison.Ordinal)) return tailscale;
            if(interfaceText.Contains("zerotier",StringComparison.Ordinal)) return b[0]==10;
            if(interfaceText.Contains("hamachi",StringComparison.Ordinal)) return b[0]==25;
            if(interfaceText.Contains("wireguard",StringComparison.Ordinal)||interfaceText.Contains("netbird",StringComparison.Ordinal)||interfaceText.Contains(" wg",StringComparison.Ordinal)) return privateRange||tailscale;
            return false;
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
