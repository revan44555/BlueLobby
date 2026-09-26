using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SteamLANControlCenter
{
    public partial class MainWindow : Window
    {
        private string selectedGameDir = "";
        private string selectedExePath = "";
        private DispatcherTimer statusTimer;

        public MainWindow()
        {
            InitializeComponent();
            Log("Steam LAN Control Center v2.1 Başlatıldı.");

            statusTimer = new DispatcherTimer();
            statusTimer.Interval = TimeSpan.FromSeconds(3);
            statusTimer.Tick += async (s, e) => await UpdateSystemStatusAsync();
            statusTimer.Start();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateSystemStatusAsync();

            bool isRadminRunning = Process.GetProcessesByName("RvServices").Length > 0 ||
                                   Process.GetProcessesByName("Radmin_VPN").Length > 0;

            if (!isRadminRunning)
            {
                BannerAutoSetup.Visibility = Visibility.Visible;
                MessageBoxResult result = MessageBox.Show(
                    "Sisteminizde Radmin VPN veya aktif ağ bağlantısı bulunamadı.\n\nSizin için otomatik yapılması gereken standart LAN konfigürasyonunu ve güvenlik duvarı ayarlarını kuralım mı?",
                    "Otomatik Kurulum Önerisi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    ExecuteAutoSetup();
                }
            }
        }

        private void BtnAutoSetup_Click(object sender, RoutedEventArgs e)
        {
            ExecuteAutoSetup();
        }

        private void ExecuteAutoSetup()
        {
            Log("⚙️ Otomatik kurulum ve konfigürasyon başlatılıyor...");
            
            // Standart Radmin IP Şablonu ve Otomatik Ayarlar
            TxtFriendIp.Text = "26.0.0.1";
            ChkUnlockDlc.IsChecked = true;
            ChkFirewall.IsChecked = true;
            ChkCustomBroadcast.IsChecked = true;

            BannerAutoSetup.Visibility = Visibility.Collapsed;
            Log("✅ Standart ayarlar ve hazır parametreler başarıyla yüklendi. Oyun klasörünüzü seçip Fix uygulayabilirsiniz.");
        }

        private void Log(string message)
        {
            TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            TxtLog.ScrollToEnd();
        }

        private async Task UpdateSystemStatusAsync()
        {
            bool isRadminRunning = Process.GetProcessesByName("RvServices").Length > 0 ||
                                   Process.GetProcessesByName("Radmin_VPN").Length > 0;

            if (isRadminRunning)
            {
                TxtRadminStatus.Text = "🟢 Radmin VPN: Aktif";
                TxtRadminStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a6e3a1"));
                BannerAutoSetup.Visibility = Visibility.Collapsed;
            }
            else
            {
                TxtRadminStatus.Text = "🔴 Radmin VPN: Bulunamadı / Kapalı";
                TxtRadminStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f38ba8"));
            }

            string ip = TxtFriendIp.Text.Trim();
            if (IPAddress.TryParse(ip, out _))
            {
                try
                {
                    using (Ping pinger = new Ping())
                    {
                        PingReply reply = await pinger.SendPingAsync(ip, 800);
                        if (reply.Status == IPStatus.Success)
                        {
                            TxtPingStatus.Text = $"Ping: {reply.RoundtripTime} ms";
                            TxtPingStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a6e3a1"));
                        }
                        else
                        {
                            TxtPingStatus.Text = "Ping: Erişilemiyor";
                            TxtPingStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f38ba8"));
                        }
                    }
                }
                catch { }
            }

            if (!string.IsNullOrEmpty(selectedGameDir))
            {
                bool hasBak = Directory.GetFiles(selectedGameDir, "*.bak", SearchOption.AllDirectories).Any();
                if (hasBak)
                {
                    TxtPatchStatus.Text = "🟢 Oyun Durumu: Goldberg Fix Uygulanmış";
                    TxtPatchStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#a6e3a1"));
                }
                else
                {
                    TxtPatchStatus.Text = "🟡 Oyun Durumu: Orijinal / Fix Yok";
                    TxtPatchStatus.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#f9e2af"));
                }
            }
        }

        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() == true)
            {
                selectedGameDir = dialog.FolderName;
                TxtGamePath.Text = selectedGameDir;
                Log($"Oyun klasörü seçildi: {selectedGameDir}");

                var exes = Directory.GetFiles(selectedGameDir, "*.exe", SearchOption.TopDirectoryOnly)
                                    .Where(f => !Path.GetFileName(f).ToLower().Contains("unins"))
                                    .ToList();

                if (exes.Any())
                {
                    selectedExePath = exes.First();
                    BtnLaunch.IsEnabled = true;
                    Log($"Oyun çalıştırıcı bulundu: {Path.GetFileName(selectedExePath)}");
                }
            }
        }

        private void BtnApplyFix_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedGameDir) || !Directory.Exists(selectedGameDir))
            {
                MessageBox.Show("Lütfen önce geçerli bir oyun klasörü seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int patchedCount = 0;

                string[] dll64List = Directory.GetFiles(selectedGameDir, "steam_api64.dll", SearchOption.AllDirectories);
                foreach (string dllPath in dll64List)
                {
                    ApplyFixToDll(dllPath, is64Bit: true);
                    patchedCount++;
                }

                string[] dll32List = Directory.GetFiles(selectedGameDir, "steam_api.dll", SearchOption.AllDirectories);
                foreach (string dllPath in dll32List)
                {
                    ApplyFixToDll(dllPath, is64Bit: false);
                    patchedCount++;
                }

                if (ChkFirewall.IsChecked == true && !string.IsNullOrEmpty(selectedExePath))
                {
                    AddFirewallRule(selectedExePath);
                }

                if (patchedCount > 0)
                {
                    Log("🎉 FIX BAŞARIYLA UYGULANDI! Lobiye girmeye hazırsınız.");
                }
                else
                {
                    Log("❌ HATA: Klasörde steam_api.dll veya steam_api64.dll bulunamadı.");
                }
            }
            catch (Exception ex)
            {
                Log($"HATA: İşlem başarısız: {ex.Message}");
            }
        }

        private void ApplyFixToDll(string dllPath, bool is64Bit)
        {
            string dir = Path.GetDirectoryName(dllPath);
            string bakPath = dllPath + ".bak";

            if (!File.Exists(bakPath))
            {
                File.Copy(dllPath, bakPath);
                Log($"Orijinal DLL yedeklendi: {Path.GetFileName(dllPath)}.bak");
            }

            string targetResource = is64Bit ? "steam_api64.dll" : "steam_api.dll";
            byte[] emuBytes = GetEmbeddedOrLocalBuffer(targetResource);

            if (emuBytes != null)
            {
                File.WriteAllBytes(dllPath, emuBytes);
                Log($"{(is64Bit ? "64-Bit" : "32-Bit")} Goldberg Emülatör kopyalandı.");
            }

            if (!string.IsNullOrWhiteSpace(TxtAppId.Text))
            {
                File.WriteAllText(Path.Combine(dir, "steam_appid.txt"), TxtAppId.Text.Trim());
            }

            string settingsDir = Path.Combine(dir, "steam_settings");
            Directory.CreateDirectory(settingsDir);

            if (!string.IsNullOrWhiteSpace(TxtPlayerName.Text))
            {
                File.WriteAllText(Path.Combine(settingsDir, "force_account_name.txt"), TxtPlayerName.Text.Trim());
            }

            if (CmbLanguage.SelectedItem != null)
            {
                string lang = ((ComboBoxItem)CmbLanguage.SelectedItem).Content.ToString();
                File.WriteAllText(Path.Combine(settingsDir, "force_language.txt"), lang.ToLower());
            }

            if (ChkUnlockDlc.IsChecked == true)
            {
                File.WriteAllText(Path.Combine(settingsDir, "DLC.txt"), "unlock_all");
                File.WriteAllText(Path.Combine(dir, "unlock_all_dlc.txt"), "1");
            }

            if (ChkCustomBroadcast.IsChecked == true)
            {
                string rawIp = TxtFriendIp.Text.Trim();
                if (IPAddress.TryParse(rawIp, out _))
                {
                    File.WriteAllText(Path.Combine(settingsDir, "custom_broadcasts.txt"), rawIp);
                    Log($"Radmin Yayın IP'si yazıldı: {rawIp}");
                }
            }
        }

        private byte[] GetEmbeddedOrLocalBuffer(string filename)
        {
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, filename);
            if (File.Exists(localPath))
            {
                return File.ReadAllBytes(localPath);
            }

            var assembly = Assembly.GetExecutingAssembly();
            string resourceName = assembly.GetManifestResourceNames()
                                          .FirstOrDefault(str => str.EndsWith(filename));

            if (!string.IsNullOrEmpty(resourceName))
            {
                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (MemoryStream ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    return ms.ToArray();
                }
            }

            return null;
        }

        private void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(selectedGameDir) || !Directory.Exists(selectedGameDir))
            {
                MessageBox.Show("Lütfen önce geçerli bir oyun klasörü seçin!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string[] bakFiles = Directory.GetFiles(selectedGameDir, "*.bak", SearchOption.AllDirectories);
                if (!bakFiles.Any())
                {
                    Log("Geri yüklenecek yedek (.bak) dosyası bulunamadı.");
                    return;
                }

                foreach (string bakPath in bakFiles)
                {
                    string originalDllPath = bakPath.Substring(0, bakPath.Length - 4);
                    File.Copy(bakPath, originalDllPath, true);
                    File.Delete(bakPath);

                    string dir = Path.GetDirectoryName(originalDllPath);
                    string appidPath = Path.Combine(dir, "steam_appid.txt");
                    string unlockDlcFile = Path.Combine(dir, "unlock_all_dlc.txt");
                    string settingsDir = Path.Combine(dir, "steam_settings");

                    if (File.Exists(appidPath)) File.Delete(appidPath);
                    if (File.Exists(unlockDlcFile)) File.Delete(unlockDlcFile);
                    if (Directory.Exists(settingsDir)) Directory.Delete(settingsDir, true);

                    Log($"Orijinale dönüldü: {Path.GetFileName(originalDllPath)}");
                }

                Log("🔄 Oyuna ait orijinal dosyalar başarıyla geri yüklendi!");
            }
            catch (Exception ex)
            {
                Log($"HATA: Geri yükleme başarısız: {ex.Message}");
            }
        }

        private void AddFirewallRule(string exePath)
        {
            try
            {
                string ruleName = $"SteamLAN_{Path.GetFileNameWithoutExtension(exePath)}";
                string cmd = $"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow program=\"{exePath}\" enable=yes";

                ProcessStartInfo psi = new ProcessStartInfo("netsh", cmd)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                Process.Start(psi);
                Log($"🛡️ Windows Güvenlik Duvarı izni eklendi: {ruleName}");
            }
            catch
            {
                Log("⚠️ Firewall izni eklenemedi.");
            }
        }

        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(selectedExePath) && File.Exists(selectedExePath))
            {
                Log($"🚀 Oyun başlatılıyor: {Path.GetFileName(selectedExePath)}");
                Process.Start(new ProcessStartInfo
                {
                    FileName = selectedExePath,
                    WorkingDirectory = Path.GetDirectoryName(selectedExePath)
                });
            }
        }

        private void TxtFriendIp_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            TxtPingStatus.Text = "Ping: -- ms";
        }
    }
}
