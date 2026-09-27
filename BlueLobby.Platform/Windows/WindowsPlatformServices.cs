using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace BlueLobby.Platform.Windows
{
    [System.Runtime.Versioning.SupportedOSPlatform("windows")]
    public sealed class WindowsPlatformServices : IPlatformServices
    {
        private sealed class WindowsPaths : IPlatformPaths
        {
            public string RoamingDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlueLobby");
            public string LocalDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BlueLobby");

            public IReadOnlyList<string> GetGameSearchRoots()
            {
                var roots = new List<string>();
                string? programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string? programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (!string.IsNullOrWhiteSpace(programFiles)) roots.Add(programFiles);
                if (!string.IsNullOrWhiteSpace(programFilesX86)) roots.Add(programFilesX86);
                return roots;
            }
        }

        private readonly WindowsPaths _paths = new();

        public bool SupportsFirewall => true;
        public string VpnDownloadUrl => "https://www.radmin-vpn.com/";

        public bool IsElevated
        {
            get
            {
                try
                {
                    using var identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                }
                catch
                {
                    return false;
                }
            }
        }

        public Task<bool> IsGameRunningAsync(string exePath)
        {
            string name = Path.GetFileNameWithoutExtension(exePath);
            bool running = Process.GetProcessesByName(name).Length > 0;
            return Task.FromResult(running);
        }
        public IPlatformPaths Paths => _paths;

        public async Task<string?> EnsureFirewallRuleAsync(string exePath)
        {
            string ruleName = "BlueLobby_" + Path.GetFileNameWithoutExtension(exePath);

            ProcessResult show = await RunNetshAsync($"advfirewall firewall show rule name=\"{ruleName}\"");
            if (show.ExitCode == 0)
            {
                return ruleName;
            }

            ProcessResult add = await RunNetshAsync($"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow program=\"{exePath}\" enable=yes");
            return add.ExitCode == 0 ? ruleName : null;
        }

        public async Task<bool> RemoveFirewallRuleAsync(string ruleName)
        {
            if (string.IsNullOrWhiteSpace(ruleName))
            {
                return false;
            }

            ProcessResult delete = await RunNetshAsync($"advfirewall firewall delete rule name=\"{ruleName}\"");
            return delete.ExitCode == 0;
        }

        private static async Task<ProcessResult> RunNetshAsync(string arguments)
        {
            var psi = new ProcessStartInfo("netsh", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("netsh başlatılamadı.");
            string stdout = await process.StandardOutput.ReadToEndAsync();
            string stderr = await process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();
            return new ProcessResult(process.ExitCode, stdout + stderr);
        }

        public Task LaunchGameAsync(string exePath, string gameDir)
        {
            try
            {
                // Oyunu mümkünse explorer üzerinden normal kullanıcı bağlamında aç.
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{exePath}\"", UseShellExecute = true });
            }
            catch
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exePath,
                    WorkingDirectory = Path.GetDirectoryName(exePath) ?? gameDir,
                    UseShellExecute = true
                });
            }

            return Task.CompletedTask;
        }

        public Task<bool> IsVpnActiveAsync()
        {
            bool processFound = Process.GetProcessesByName("RvService").Length > 0 ||
                                Process.GetProcessesByName("RvServices").Length > 0 ||
                                Process.GetProcessesByName("Radmin_VPN").Length > 0 ||
                                Process.GetProcessesByName("Radmin VPN").Length > 0;
            return Task.FromResult(processFound || !string.IsNullOrWhiteSpace(GetVpnIpv4()));
        }

        public string? GetVpnIpv4()
        {
            foreach (string ipv4 in EnumerateVpnIpv4("radmin"))
            {
                if (ipv4.StartsWith("26.", StringComparison.Ordinal))
                {
                    return ipv4;
                }
            }
            return null;
        }

        public IReadOnlyList<string> GetVpnInterfaceNames()
        {
            var names = new List<string>();
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                string text = $"{nic.Name} {nic.Description}".ToLowerInvariant();
                if (text.Contains("radmin"))
                {
                    names.Add(nic.Name);
                }
            }
            return names;
        }

        private static IEnumerable<string> EnumerateVpnIpv4(string interfaceKeyword)
        {
            NetworkInterface[] nics;
            try
            {
                nics = NetworkInterface.GetAllNetworkInterfaces();
            }
            catch
            {
                yield break;
            }

            foreach (NetworkInterface nic in nics)
            {
                if (nic.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                string text = $"{nic.Name} {nic.Description}".ToLowerInvariant();
                if (!text.Contains(interfaceKeyword))
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

        private sealed record ProcessResult(int ExitCode, string Output);
    }
}
