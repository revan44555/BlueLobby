using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;

namespace SteamLANControlCenter
{
    public partial class MainWindow : Window
    {
        private static readonly SolidColorBrush GreenBrush = CreateBrush("#a6e3a1");
        private static readonly SolidColorBrush RedBrush = CreateBrush("#f38ba8");
        private static readonly SolidColorBrush YellowBrush = CreateBrush("#f9e2af");
        private static readonly SolidColorBrush MutedBrush = CreateBrush("#a6adc8");

        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        private readonly string _settingsPath;
        private readonly string _manifestDir;
        private readonly string _logPath;
        private readonly AppSettings _settings = new();
        private readonly DispatcherTimer _statusTimer;
        private int _statusBusy;
        private int _operationRunning;
        private string selectedGameDir = string.Empty;
        private string selectedExePath = string.Empty;

        private bool _isFullscreen;
        private WindowStyle _normalWindowStyle;
        private ResizeMode _normalResizeMode;
        private WindowState _normalWindowState;

        public MainWindow()
        {
            InitializeComponent();

            // v3 UI masaüstü launcher davranışı: uygulama her açılışta kullanılabilir çalışma alanını doldurur.
            WindowState = WindowState.Maximized;
            _normalWindowStyle = WindowStyle;
            _normalResizeMode = ResizeMode;
            _normalWindowState = WindowState;

            string appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SteamLANControlCenter");
            string localDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SteamLANControlCenter");
            Directory.CreateDirectory(appDir);
            Directory.CreateDirectory(localDir);
            Directory.CreateDirectory(Path.Combine(localDir, "manifests"));

            _settingsPath = Path.Combine(appDir, "settings.json");
            _manifestDir = Path.Combine(localDir, "manifests");
            _logPath = Path.Combine(localDir, "app.log");

            LoadSettingsIntoUi();
            Loc.SetLanguage(_settings.UiLanguage);
            Loc.Apply(this);
            ThemeManager.Apply(this, _settings.Theme);
            ApplyMascotVisibility(_settings.ShowMascot);

            Log("Steam LAN Control Center v3.0 başlatıldı. Klasörü seçebilir veya pencereye sürükleyip bırakabilirsiniz.");

            _statusTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(4) };
            _statusTimer.Tick += async (_, _) => await UpdateSystemStatusAsync();
            _statusTimer.Start();
        }

        private static SolidColorBrush CreateBrush(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }

        // ===================== Maskot =====================

        private void StartMascotBounce()
        {
            if (TxtMascot.Visibility != Visibility.Visible)
            {
                return;
            }

            var anim = new DoubleAnimation(0, -7, new Duration(TimeSpan.FromMilliseconds(300)))
            {
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut },
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                BeginTime = TimeSpan.FromMilliseconds(600)
            };

            MascotTransform.BeginAnimation(TranslateTransform.YProperty, anim);
        }

        private void ApplyMascotVisibility(bool show)
        {
            MascotCard.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            if (show)
            {
                StartMascotBounce();
            }
            else
            {
                MascotTransform.BeginAnimation(TranslateTransform.YProperty, null);
            }
        }

        private void ChkMascot_Click(object sender, RoutedEventArgs e)
        {
            ApplyMascotVisibility(ChkMascot.IsChecked == true);
            SaveSettings();
        }

        private void TxtMascot_Click(object sender, MouseButtonEventArgs e)
        {
            string tip = Loc.T("mascot_tip") + "\n\n1) Klasörü seç ya da sürükle\n2) Fix'e bas\n3) Takılırsan loga bak";
            MessageBox.Show(this, tip, "Mavi 🤖", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ===================== Navigasyon =====================

        private void Nav_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string section)
            {
                SetSection(section);
            }
        }

        private void SetSection(string section)
        {
            SectionGame.Visibility = section == "game" ? Visibility.Visible : Visibility.Collapsed;
            SectionFriends.Visibility = section == "friends" ? Visibility.Visible : Visibility.Collapsed;
            SectionSettings.Visibility = section == "settings" ? Visibility.Visible : Visibility.Collapsed;
            SectionLog.Visibility = section == "log" ? Visibility.Visible : Visibility.Collapsed;

            HighlightNav(NavOyun, section == "game");
            HighlightNav(NavFriends, section == "friends");
            HighlightNav(NavSettings, section == "settings");
            HighlightNav(NavLog, section == "log");
        }

        private void HighlightNav(Button button, bool active)
        {
            button.Background = active ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#45475a")) : Brushes.Transparent;
            button.FontWeight = active ? FontWeights.SemiBold : FontWeights.Normal;
        }

        private void BtnDownloadRadmin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo { FileName = "https://www.radmin-vpn.com/", UseShellExecute = true });
                Log("🌐 Radmin VPN resmi sitesi açıldı. Kurulumdan sonra program yeniden başlatılınca algılanır.");
            }
            catch (Exception ex)
            {
                Log($"⚠️ Site açılamadı: {ex.Message} — adres: https://www.radmin-vpn.com/");
            }
        }

        // ===================== Emülatör Ara ve Kopyala =====================

        private async void BtnFindEmulator_Click(object sender, RoutedEventArgs e)
        {
            ReportProgress(-1, "Bilgisayarda emülatör dosyaları aranıyor...");
            try
            {
                string[] names = { "steam_api64.dll", "steam_api.dll" };
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                var searchRoots = new List<string>();
                string? userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                if (!string.IsNullOrWhiteSpace(userProfile))
                {
                    searchRoots.Add(Path.Combine(userProfile, "Downloads"));
                    searchRoots.Add(Path.Combine(userProfile, "Desktop"));
                    searchRoots.Add(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
                }

                var found = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                await Task.Run(() =>
                {
                    foreach (string root in searchRoots.Where(Directory.Exists))
                    {
                        var opts = new EnumerationOptions
                        {
                            RecurseSubdirectories = true,
                            IgnoreInaccessible = true,
                            MatchCasing = MatchCasing.CaseInsensitive,
                            MaxRecursionDepth = 6
                        };

                        foreach (string name in names)
                        {
                            if (found.ContainsKey(name))
                            {
                                continue;
                            }

                            try
                            {
                                string? hit = Directory.GetFiles(root, name, opts).FirstOrDefault();
                                if (hit != null)
                                {
                                    lock (found)
                                    {
                                        found.TryAdd(name, hit);
                                    }
                                }
                            }
                            catch
                            {
                                // erişilemeyen klasörleri atla
                            }
                        }

                        if (found.Count == names.Length)
                        {
                            break;
                        }
                    }
                });

                if (found.Count == 0)
                {
                    // Elle seçtir
                    ShowProgress(false);
                    var ofd = new OpenFileDialog
                    {
                        Filter = "Emülatör DLL|steam_api64.dll;steam_api.dll",
                        Title = "steam_api64.dll dosyasını seçin"
                    };

                    if (ofd.ShowDialog() == true)
                    {
                        found[Path.GetFileName(ofd.FileName)] = ofd.FileName;
                        string? sibling = names.FirstOrDefault(n => !found.ContainsKey(n));
                        if (sibling != null)
                        {
                            string candidate = Path.Combine(Path.GetDirectoryName(ofd.FileName) ?? string.Empty, sibling);
                            if (File.Exists(candidate))
                            {
                                found[sibling] = candidate;
                            }
                        }
                    }
                    else
                    {
                        Log("Arama yapılmadı; kullanıcı vazgeçti.");
                        return;
                    }
                }

                ShowProgress(false);

                if (found.Count == 0)
                {
                    MessageBox.Show(
                        this,
                        "Dosyalar bulunamadı.\n\nİndirdiğiniz ZIP'ten steam_api64.dll ve steam_api.dll dosyalarını çıkarın, sonra tekrar deneyin.\nHedef klasör: " + baseDir,
                        "Bulunamadı",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("Şu dosyalar bulundu ve EXE'nin yanına kopyalanacak:");
                sb.AppendLine();
                foreach (var kv in found)
                {
                    sb.AppendLine($"  • {kv.Key}");
                    sb.AppendLine($"    {kv.Value}");
                }
                sb.AppendLine();
                sb.Append("Kopyalansın mı?");

                var result = MessageBox.Show(this, sb.ToString(), "Dosyalar Bulundu", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result != MessageBoxResult.Yes)
                {
                    Log("Kopyalama kullanıcı tarafından iptal edildi.");
                    return;
                }

                foreach (var kv in found)
                {
                    string dest = Path.Combine(baseDir, kv.Key);
                    File.Copy(kv.Value, dest, overwrite: true);
                    Log($"📦 Kopyalandı: {kv.Key} → {dest}");
                }

                MessageBox.Show(this, "Tamamlandı! Artık \"Fix Uygula\"yı kullanabilirsiniz.", "✅ Hazır", MessageBoxButton.OK, MessageBoxImage.Information);
                await UpdateSystemStatusAsync();
            }
            catch (Exception ex)
            {
                ShowProgress(false);
                Log($"HATA: {ErrorDoctor.Explain(ex)}");
            }
        }

        // ===================== Tema & Dil =====================

        private void CmbTheme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbTheme.SelectedItem is ComboBoxItem item && item.Tag is string theme)
            {
                ThemeManager.Apply(this, theme);
                SaveSettings();
            }
        }

        private void CmbUiLang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbUiLang.SelectedItem is ComboBoxItem item && item.Tag is string lang)
            {
                Loc.SetLanguage(lang);
                Loc.Apply(this);
                SaveSettings();
            }
        }

        // ===================== Sürükle-bırak =====================

        private void Window_DragEnter(object sender, DragEventArgs e)
        {
            bool hasFolder = e.Data.GetDataPresent(DataFormats.FileDrop) &&
                             e.Data.GetData(DataFormats.FileDrop) is string[] paths &&
                             paths.Any(Directory.Exists);

            e.Effects = hasFolder ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private async void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is not string[] paths)
            {
                return;
            }

            string? dir = paths.FirstOrDefault(Directory.Exists);
            if (dir == null)
            {
                MessageBox.Show("Buraya bir klasör bırakmalısınız.", "Sürükle-bırak", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            await SelectGameFolderAsync(dir);
        }

        // ===================== Ortak akış =====================

        private async Task SelectGameFolderAsync(string dir)
        {
            if (!Directory.Exists(dir))
            {
                MessageBox.Show("Seçilen klasör erişilemiyor.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            selectedGameDir = dir;
            TxtGamePath.Text = selectedGameDir;
            Log($"Oyun klasörü seçildi: {selectedGameDir}");

            ReportProgress(-1, "EXE adayları taranıyor...");
            try
            {
                await PopulateExecutablesAsync();
            }
            finally
            {
                ShowProgress(false);
            }

            SaveSettings();
            await UpdateSystemStatusAsync();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Bazı Windows/DPI ortamlarında XAML WindowState ilk layout'tan önce uygulanmayabilir.
            // Dispatcher ile bir kez daha maximize ederek pencerenin gerçekten çalışma alanına oturmasını sağlarız.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (!_isFullscreen)
                {
                    WindowState = WindowState.Maximized;
                }
            }), DispatcherPriority.ApplicationIdle);

            StartMascotBounce();

            if (!string.IsNullOrWhiteSpace(selectedGameDir) && Directory.Exists(selectedGameDir))
            {
                TxtGamePath.Text = selectedGameDir;
                ReportProgress(-1, "EXE adayları taranıyor...");
                try
                {
                    await PopulateExecutablesAsync();
                }
                finally
                {
                    ShowProgress(false);
                }
            }

            await UpdateSystemStatusAsync();

            // Güncelleme kontrolü: sessiz; sadece yeni sürüm varsa konuşur.
            _ = UpdateChecker.CheckAsync(this);

            bool radminActive = await IsRadminActiveAsync();
            BannerAutoSetup.Visibility = radminActive ? Visibility.Collapsed : Visibility.Visible;
            if (!radminActive)
            {
                var result = MessageBox.Show(
                    "Radmin VPN ağ adaptörü bulunamadı.\n\nStandart LAN/Radmin ayarları uygulansın mı?",
                    "Standart Ayar Önerisi",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await ExecuteAutoSetupAsync();
                }
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                ToggleFullscreen();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && _isFullscreen)
            {
                ExitFullscreen();
                e.Handled = true;
            }
        }

        private void ToggleFullscreen()
        {
            if (_isFullscreen)
            {
                ExitFullscreen();
                return;
            }

            _normalWindowStyle = WindowStyle;
            _normalResizeMode = ResizeMode;
            _normalWindowState = WindowState;

            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            WindowState = WindowState.Maximized;
            _isFullscreen = true;
        }

        private void ExitFullscreen()
        {
            WindowStyle = _normalWindowStyle;
            ResizeMode = _normalResizeMode;
            WindowState = _normalWindowState == WindowState.Minimized
                ? WindowState.Normal
                : _normalWindowState;
            _isFullscreen = false;
        }

        private async void BtnPingTest_Click(object sender, RoutedEventArgs e)
        {
            await UpdateSystemStatusAsync();
            Log("Ping testi tamamlandı.");
        }

        private async void BtnConnectionScan_Click(object sender, RoutedEventArgs e)
        {
            await UpdateSystemStatusAsync();
            bool radmin = await IsRadminActiveAsync();
            string ip = TxtFriendIp.Text.Trim();
            Log(radmin
                ? $"Bağlantı taraması: Radmin aktif{(string.IsNullOrWhiteSpace(ip) ? ". Hedef IP belirtilmedi." : $", hedef {ip}.")}"
                : "Bağlantı taraması: Radmin VPN aktif değil.");
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _statusTimer.Stop();
            SaveSettings();
        }

        private async void BtnAutoSetup_Click(object sender, RoutedEventArgs e)
        {
            await ExecuteAutoSetupAsync();
        }

        private async Task ExecuteAutoSetupAsync()
        {
            ChkUnlockDlc.IsChecked = false;
            ChkFirewall.IsChecked = true;
            ChkCustomBroadcast.IsChecked = true;

            string? radminIp = await GetRadminIpv4Async();
            if (!string.IsNullOrWhiteSpace(radminIp))
            {
                TxtFriendIp.Text = radminIp;
                Log($"✅ Radmin IPv4 algılandı: {radminIp}");
            }
            else
            {
                Log("⚠️ Radmin IPv4 algılanamadı. Radmin VPN ağını kontrol edin.");
            }

            BannerAutoSetup.Visibility = Visibility.Collapsed;
            SaveSettings();
        }

        // ===================== Arkadaş listesi =====================

        private void RefreshFriendList()
        {
            LstFriends.Items.Clear();
            foreach (FriendEntry friend in _settings.Friends.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase))
            {
                LstFriends.Items.Add(friend);
            }
        }

        private void BtnAddFriend_Click(object sender, RoutedEventArgs e)
        {
            string name = TxtFriendName.Text.Trim();
            string ip = TxtNewFriendIp.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Arkadaş için bir isim yazın.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!IPAddress.TryParse(ip, out _))
            {
                MessageBox.Show("Geçerli bir IP adresi girin. Örn: 26.x.x.x", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            _settings.Friends.RemoveAll(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
            _settings.Friends.Add(new FriendEntry
            {
                Name = name,
                Ip = ip.Trim(),
                AddedUtc = DateTime.UtcNow.ToString("O")
            });

            TxtFriendName.Clear();
            TxtNewFriendIp.Clear();
            RefreshFriendList();
            SaveSettings();
            Log($"👥 Arkadaş eklendi: {name} — {ip}");
        }

        private void BtnUseFriend_Click(object sender, RoutedEventArgs e)
        {
            UseSelectedFriend();
        }

        private void LstFriends_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            UseSelectedFriend();
        }

        private void LstFriends_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Seçim değişimi; kullanım butonu/double-click ile uygulanır.
        }

        private void UseSelectedFriend()
        {
            if (LstFriends.SelectedItem is not FriendEntry friend)
            {
                MessageBox.Show("Listeden bir arkadaş seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            TxtFriendIp.Text = friend.Ip;
            Log($"👥 {friend.Name} seçildi ({friend.Ip}). Ping durumu yukarıda renklenecek.");
        }

        private void BtnRemoveFriend_Click(object sender, RoutedEventArgs e)
        {
            if (LstFriends.SelectedItem is not FriendEntry friend)
            {
                MessageBox.Show("Listeden bir arkadaş seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            _settings.Friends.RemoveAll(f => string.Equals(f.Name, friend.Name, StringComparison.OrdinalIgnoreCase) && f.Ip == friend.Ip);
            RefreshFriendList();
            SaveSettings();
            Log($"👥 Arkadaş silindi: {friend.Name}");
        }

        // ===================== Log & ilerleme =====================

        private void Log(string message)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new Action(() => Log(message)));
                return;
            }

            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            TxtLog.AppendText(line + Environment.NewLine);
            TxtLog.ScrollToEnd();

            if (message.StartsWith("HATA", StringComparison.OrdinalIgnoreCase) || message.Contains("❌"))
            {
                SetSection("log");
            }

            try
            {
                File.AppendAllText(_logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
            }
            catch
            {
                // Log dosyası yazılamazsa uygulama düşmemeli.
            }
        }

        private void ShowProgress(bool show, string message = "")
        {
            ProgressArea.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
            TxtProgress.Text = message;
            if (!show)
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = 0;
            }
        }

        private void ReportProgress(double percent, string message)
        {
            ShowProgress(true, message);
            if (percent < 0)
            {
                ProgressBar.IsIndeterminate = true;
            }
            else
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = Math.Clamp(percent, 0, 100);
            }
        }

        // ===================== Durum =====================

        private async Task UpdateSystemStatusAsync()
        {
            if (Interlocked.Exchange(ref _statusBusy, 1) == 1)
            {
                return;
            }

            try
            {
                bool radminActive = await IsRadminActiveAsync();
                TxtRadminStatus.Text = radminActive ? "🟢 Radmin VPN: Aktif" : "🔴 Radmin VPN: Bulunamadı / Kapalı";
                TxtRadminStatus.Foreground = radminActive ? GreenBrush : RedBrush;
                BannerAutoSetup.Visibility = radminActive ? Visibility.Collapsed : Visibility.Visible;

                string ip = TxtFriendIp.Text.Trim();
                if (IPAddress.TryParse(ip, out IPAddress? parsedIp) && parsedIp.AddressFamily == AddressFamily.InterNetwork && !ip.Contains('x', StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        using var pinger = new Ping();
                        PingReply reply = await pinger.SendPingAsync(parsedIp, 800);
                        if (reply.Status == IPStatus.Success)
                        {
                            long ms = reply.RoundtripTime;
                            TxtPingStatus.Text = $"Ping: {ms} ms";
                            TxtPingStatus.Foreground = ms < 60 ? GreenBrush : ms < 150 ? YellowBrush : RedBrush;
                        }
                        else
                        {
                            TxtPingStatus.Text = "Ping: Erişilemiyor";
                            TxtPingStatus.Foreground = RedBrush;
                        }
                    }
                    catch
                    {
                        TxtPingStatus.Text = "Ping: Ölçülemedi";
                        TxtPingStatus.Foreground = YellowBrush;
                    }
                }
                else
                {
                    TxtPingStatus.Text = "Ping: -- ms";
                    TxtPingStatus.Foreground = MutedBrush;
                }

                bool emu64 = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steam_api64.dll"));
                bool emu32 = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steam_api.dll"));
                if (emu64 || emu32)
                {
                    TxtEmuStatus.Text = Loc.T("emu_ok") + (emu64 && emu32 ? "" : (emu64 ? " (yalnız 64-bit)" : " (yalnız 32-bit)"));
                    TxtEmuStatus.Foreground = GreenBrush;
                }
                else
                {
                    TxtEmuStatus.Text = Loc.T("emu_missing");
                    TxtEmuStatus.Foreground = YellowBrush;
                }

                if (string.IsNullOrWhiteSpace(selectedGameDir) || !Directory.Exists(selectedGameDir))
                {
                    TxtPatchStatus.Tag = null;
                    TxtPatchStatus.Text = "🔴 Oyun Durumu: Klasör Seçilmedi";
                    TxtPatchStatus.Foreground = RedBrush;
                }
                else
                {
                    PatchManifest? manifest = LoadManifest(selectedGameDir);
                    if (manifest != null)
                    {
                        string when = "tarih bilinmiyor";
                        if (DateTime.TryParse(manifest.CreatedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime dt))
                        {
                            when = dt.ToLocalTime().ToString("dd.MM.yyyy HH:mm");
                        }

                        TxtPatchStatus.Tag = "patched";
                        TxtPatchStatus.Text = $"🟢 Oyun düzenlendi (🗓 {when})";
                        TxtPatchStatus.Foreground = GreenBrush;
                    }
                    else
                    {
                        TxtPatchStatus.Tag = null;
                        TxtPatchStatus.Text = "🟡 Oyun Durumu: Araç kaydı yok";
                        TxtPatchStatus.Foreground = YellowBrush;
                    }
                }
            }
            catch (Exception ex)
            {
                Log($"⚠️ Durum güncellemesi başarısız: {ex.Message}");
            }
            finally
            {
                Interlocked.Exchange(ref _statusBusy, 0);
            }
        }

        private async Task<bool> IsRadminActiveAsync()
        {
            return await Task.Run(() =>
            {
                bool processFound = Process.GetProcessesByName("RvService").Length > 0 ||
                                    Process.GetProcessesByName("RvServices").Length > 0 ||
                                    Process.GetProcessesByName("Radmin_VPN").Length > 0 ||
                                    Process.GetProcessesByName("Radmin VPN").Length > 0;

                return processFound || !string.IsNullOrWhiteSpace(GetRadminIpv4());
            });
        }

        private static string? GetRadminIpv4()
        {
            try
            {
                foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (nic.OperationalStatus != OperationalStatus.Up)
                    {
                        continue;
                    }

                    string text = $"{nic.Name} {nic.Description}".ToLowerInvariant();
                    if (!text.Contains("radmin"))
                    {
                        continue;
                    }

                    foreach (UnicastIPAddressInformation ip in nic.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork && ip.Address.ToString().StartsWith("26.", StringComparison.Ordinal))
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }
            catch
            {
                // NIC listesi alınamazsa sessizce geç.
            }

            return null;
        }

        private Task<string?> GetRadminIpv4Async()
        {
            return Task.Run(() => GetRadminIpv4());
        }

        // ===================== Klasör / EXE =====================

        private async void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFolderDialog();
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            await SelectGameFolderAsync(dialog.FolderName);
        }

        private async Task PopulateExecutablesAsync()
        {
            selectedExePath = string.Empty;
            BtnLaunch.IsEnabled = false;
            CmbExes.Items.Clear();

            if (string.IsNullOrWhiteSpace(selectedGameDir) || !Directory.Exists(selectedGameDir))
            {
                return;
            }

            List<string> exes = await Task.Run(() =>
            {
                try
                {
                    return Directory.GetFiles(selectedGameDir, "*.exe", SearchOption.TopDirectoryOnly)
                        .Where(f => !Path.GetFileName(f).Contains("unins", StringComparison.OrdinalIgnoreCase))
                        .Where(f => !Path.GetFileName(f).Contains("crash", StringComparison.OrdinalIgnoreCase))
                        .Where(f => !Path.GetFileName(f).Contains("redist", StringComparison.OrdinalIgnoreCase))
                        .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)
                        .ToList();
                }
                catch
                {
                    return new List<string>();
                }
            });

            if (exes.Count == 0)
            {
                Log("⚠️ Klasörde uygun EXE bulunamadı. Çalıştırıcı için alt klasörleri elle kontrol edin.");
                return;
            }

            int preferredIndex = 0;
            for (int i = 0; i < exes.Count; i++)
            {
                string path = exes[i];
                var item = new ComboBoxItem { Content = Path.GetFileName(path), Tag = path };
                CmbExes.Items.Add(item);

                if (!string.IsNullOrWhiteSpace(_settings.ExeName) &&
                    string.Equals(Path.GetFileName(path), _settings.ExeName, StringComparison.OrdinalIgnoreCase))
                {
                    preferredIndex = i;
                }
            }

            CmbExes.SelectedIndex = preferredIndex;
            OnExecutableSelectionChanged();
            Log($"EXE adayları yüklendi: {exes.Count}");
        }

        private void CmbExes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OnExecutableSelectionChanged();
        }

        private void OnExecutableSelectionChanged()
        {
            if (CmbExes.SelectedItem is ComboBoxItem item && item.Tag is string path && File.Exists(path))
            {
                selectedExePath = path;
                BtnLaunch.IsEnabled = true;
                Log($"Çalıştırıcı seçildi: {Path.GetFileName(path)}");
            }
            else
            {
                selectedExePath = string.Empty;
                BtnLaunch.IsEnabled = false;
            }
        }

        // ===================== Fix Uygula =====================

        private async void BtnApplyFix_Click(object sender, RoutedEventArgs e)
        {
            if (Interlocked.Exchange(ref _operationRunning, 1) == 1)
            {
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(selectedGameDir) || !Directory.Exists(selectedGameDir))
                {
                    MessageBox.Show("Önce geçerli bir oyun klasörü seçin veya pencereye sürükleyin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!int.TryParse(TxtAppId.Text.Trim(), out int appId) || appId <= 0)
                {
                    MessageBox.Show("AppID pozitif sayı olmalıdır.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ReportProgress(-1, "Oyunun kapalı olduğu kontrol ediliyor...");
                if (await IsSelectedGameRunningAsync())
                {
                    ShowProgress(false);
                    MessageBox.Show("Seçilen oyun şu an çalışıyor gibi görünüyor.\n\nÖnce oyundan tamamen çıkın, sonra tekrar deneyin.", "Oyun Açık", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ReportProgress(5, "Steam API dosyaları aranıyor...");
                var targets = await Task.Run(() => Core.FindSteamApiTargets(selectedGameDir));
                if (targets.Count == 0)
                {
                    ShowProgress(false);
                    Log("❌ HATA: Bu klasörde steam_api.dll veya steam_api64.dll bulunamadı. Doğru oyun klasörünü seçtiğinizden emin olun.");
                    return;
                }

                ReportProgress(15, "Gerekli dosyalar doğrulanıyor...");
                var buffers = new List<(SteamApiTarget Target, byte[] Bytes)>();
                foreach (SteamApiTarget target in targets)
                {
                    byte[]? bytes = await Task.Run(() => GetEmulatorBytes(target.Is64Bit));
                    if (bytes == null || bytes.Length == 0)
                    {
                        ShowProgress(false);
                        Log($"❌ HATA: {(target.Is64Bit ? "steam_api64.dll" : "steam_api.dll")} için emülatör dosyası bulunamadı. Hiçbir dosya değiştirilmedi.");
                        return;
                    }

                    buffers.Add((target, bytes));
                }

                ShowProgress(false);
                var confirm = MessageBox.Show(
                    "Bu işlem oyun klasöründeki Steam API dosyalarını değiştirir ve geri alma kaydı oluşturur.\n\nYalnızca sahip olduğunuz oyunlar için, çevrimdışı/LAN kullanımında kullanın. Anti-cheat/çevrimiçi oyunlarda risk olabilir.\n\nDevam edilsin mi?",
                    "Onay Gerekli",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                {
                    Log("İşlem kullanıcı tarafından iptal edildi.");
                    return;
                }

                SetBusy(true);

                var options = new PatchOptions(
                    TxtPlayerName.Text.Trim(),
                    CmbLanguage.SelectedItem is ComboBoxItem li && li.Content is string lang ? lang.ToLowerInvariant() : "english",
                    ChkCustomBroadcast.IsChecked == true ? TxtFriendIp.Text.Trim() : string.Empty,
                    ChkUnlockDlc.IsChecked == true,
                    appId);

                var manifest = LoadManifest(selectedGameDir) ?? new PatchManifest
                {
                    GameDir = selectedGameDir,
                    CreatedUtc = DateTime.UtcNow.ToString("O")
                };

                IProgress<(double Percent, string Message)> reporter = new Progress<(double Percent, string Message)>(p => ReportProgress(p.Percent, p.Message));

                try
                {
                    int total = buffers.Count;
                    await Task.Run(() =>
                    {
                        for (int i = 0; i < total; i++)
                        {
                            reporter.Report((20.0 + 55.0 * i / total, $"Hedef güncelleniyor ({i + 1}/{total})..."));
                            ApplyPatchToTarget(buffers[i].Target, buffers[i].Bytes, options, manifest);
                        }
                    });

                    reporter.Report((85, "Güvenlik duvarı ayarlanıyor..."));
                    if (ChkFirewall.IsChecked == true && !string.IsNullOrWhiteSpace(selectedExePath))
                    {
                        string? ruleName = await EnsureFirewallRuleAsync(selectedExePath);
                        if (!string.IsNullOrWhiteSpace(ruleName))
                        {
                            manifest.FirewallRuleName = ruleName;
                            manifest.FirewallExePath = selectedExePath;
                        }
                    }

                    reporter.Report((95, "Geri alma kaydı yazılıyor..."));
                    manifest.CreatedUtc = DateTime.UtcNow.ToString("O");
                    SaveManifest(manifest);

                    reporter.Report((100, "Tamamlandı."));
                    Log($"🎉 İşlem tamamlandı. Hedef sayısı: {buffers.Count}. Geri almak için 'Geri Yükle' düğmesini kullanın.");
                }
                catch (Exception ex)
                {
                    await RollbackManifestAsync(manifest);
                    Log($"HATA: {ErrorDoctor.Explain(ex)}");
                }
            }
            catch (Exception ex)
            {
                Log($"HATA: {ErrorDoctor.Explain(ex)}");
            }
            finally
            {
                ShowProgress(false);
                SetBusy(false);
                SaveSettings();
                await UpdateSystemStatusAsync();
                Interlocked.Exchange(ref _operationRunning, 0);
            }
        }

        private byte[]? GetEmulatorBytes(bool is64Bit)
        {
            string fileName = is64Bit ? "steam_api64.dll" : "steam_api.dll";
            string localPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            return File.Exists(localPath) ? File.ReadAllBytes(localPath) : null;
        }

        private sealed record PatchOptions(string PlayerName, string Language, string BroadcastIp, bool CreateDlcTemplate, int AppId);

        private void ApplyPatchToTarget(SteamApiTarget target, byte[] emulatorBytes, PatchOptions options, PatchManifest manifest)
        {
            string dllPath = target.Path;
            string dir = Path.GetDirectoryName(dllPath) ?? selectedGameDir;
            string backupPath = dllPath + ".bak";

            if (!File.Exists(backupPath))
            {
                File.Copy(dllPath, backupPath);
                Log($"Orijinal DLL yedeklendi: {Path.GetFileName(backupPath)}");
            }

            string tempPath = dllPath + ".tmp";
            File.WriteAllBytes(tempPath, emulatorBytes);
            File.Copy(tempPath, dllPath, overwrite: true);
            File.Delete(tempPath);

            manifest.Entries.RemoveAll(e => string.Equals(e.DllPath, dllPath, StringComparison.OrdinalIgnoreCase));
            manifest.Entries.Add(new PatchEntry { DllPath = dllPath, BackupPath = backupPath, Is64Bit = target.Is64Bit });

            WriteManagedTextFile(Path.Combine(dir, "steam_appid.txt"), options.AppId.ToString(), manifest);
            string settingsDir = Path.Combine(dir, "steam_settings");
            if (!Directory.Exists(settingsDir))
            {
                Directory.CreateDirectory(settingsDir);
                Core.AddUnique(manifest.CreatedDirectories, settingsDir);
            }

            if (!string.IsNullOrWhiteSpace(options.PlayerName))
            {
                WriteManagedTextFile(Path.Combine(settingsDir, "force_account_name.txt"), options.PlayerName, manifest);
            }

            if (!string.IsNullOrWhiteSpace(options.Language))
            {
                WriteManagedTextFile(Path.Combine(settingsDir, "force_language.txt"), options.Language, manifest);
            }

            if (options.CreateDlcTemplate)
            {
                const string template = "# Bu dosyaya yalnızca sahip olduğunuz DLC AppID'lerini satır satır yazın." + "\n" +
                                        "# Otomatik kilit aşma yapılmaz. Örnek: 123456" + "\n";
                WriteManagedTextFile(Path.Combine(settingsDir, "DLC.txt"), template, manifest);
            }

            if (!string.IsNullOrWhiteSpace(options.BroadcastIp) && IPAddress.TryParse(options.BroadcastIp, out IPAddress? ip) && ip.AddressFamily == AddressFamily.InterNetwork)
            {
                WriteManagedTextFile(Path.Combine(settingsDir, "custom_broadcasts.txt"), options.BroadcastIp, manifest);
            }

            Log($"{(target.Is64Bit ? "64-bit" : "32-bit")} hedef güncellendi: {Path.GetFileName(dllPath)}");
        }

        private void WriteManagedTextFile(string path, string content, PatchManifest manifest)
        {
            if (File.Exists(path))
            {
                string backupPath = path + ".pre_slcc";
                if (!File.Exists(backupPath))
                {
                    File.Copy(path, backupPath);
                    manifest.TextBackups.RemoveAll(b => string.Equals(b.OriginalPath, path, StringComparison.OrdinalIgnoreCase));
                    manifest.TextBackups.Add(new FileBackup { OriginalPath = path, BackupPath = backupPath });
                }
            }
            else
            {
                Core.AddUnique(manifest.CreatedFiles, path);
            }

            string tempPath = path + ".tmp";
            File.WriteAllText(tempPath, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            File.Copy(tempPath, path, overwrite: true);
            File.Delete(tempPath);
        }

        private async Task<string?> EnsureFirewallRuleAsync(string exePath)
        {
            string ruleName = $"SteamLAN_{Core.ComputeKey(exePath)}";
            ProcessResult show = await RunNetshAsync($"advfirewall firewall show rule name=\"{ruleName}\"");
            if (show.ExitCode == 0)
            {
                Log($"🛡️ Firewall kuralı zaten var: {ruleName}");
                return ruleName;
            }

            ProcessResult add = await RunNetshAsync($"advfirewall firewall add rule name=\"{ruleName}\" dir=in action=allow program=\"{exePath}\" enable=yes remoteip=localsubnet,26.0.0.0/8");
            if (add.ExitCode == 0)
            {
                Log($"🛡️ Firewall kuralı eklendi: {ruleName}");
                return ruleName;
            }

            Log($"⚠️ Firewall kuralı eklenemedi: {add.ErrorText.Trim()} {add.OutputText.Trim()}");
            return null;
        }

        private async Task<ProcessResult> RunNetshAsync(string arguments)
        {
            var psi = new ProcessStartInfo("netsh", arguments)
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = Process.Start(psi) ?? throw new InvalidOperationException("netsh başlatılamadı.");
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            await Task.Run(() => process.WaitForExit(5000));

            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); } catch { }
            }

            string output = await outputTask;
            string error = await errorTask;
            return new ProcessResult(process.ExitCode, output, error);
        }

        // ===================== Geri Yükle =====================

        private async void BtnRestore_Click(object sender, RoutedEventArgs e)
        {
            if (Interlocked.Exchange(ref _operationRunning, 1) == 1)
            {
                return;
            }

            try
            {
                if (string.IsNullOrWhiteSpace(selectedGameDir) || !Directory.Exists(selectedGameDir))
                {
                    MessageBox.Show("Önce geçerli bir oyun klasörü seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                PatchManifest? manifest = LoadManifest(selectedGameDir);

                if (manifest != null)
                {
                    // Geri alma ekranı: ne yapılacağını listeleyip onay al.
                    var dialog = new RestoreWindow(manifest) { Owner = this };
                    if (dialog.ShowDialog() != true)
                    {
                        Log("Geri yükleme iptal edildi.");
                        return;
                    }
                }
                else
                {
                    var fallback = MessageBox.Show(
                        "Bu klasör için ayrıntılı bir geri alma kaydı bulunamadı.\n\nYalnızca klasördeki steam_api*.dll.bak yedekleri geri yüklensin mi?\n(steam_appid ve settings dosyalarına dokunulmaz.)",
                        "Sınırlı Geri Yükleme",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (fallback != MessageBoxResult.Yes)
                    {
                        Log("Geri yükleme iptal edildi.");
                        return;
                    }
                }

                ReportProgress(-1, "Oyunun kapalı olduğu kontrol ediliyor...");
                if (await IsSelectedGameRunningAsync())
                {
                    ShowProgress(false);
                    MessageBox.Show("Seçilen oyun şu an çalışıyor gibi görünüyor.\n\nÖnce oyundan tamamen çıkın.", "Oyun Açık", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                SetBusy(true);
                IProgress<(double Percent, string Message)> reporter = new Progress<(double Percent, string Message)>(p => ReportProgress(p.Percent, p.Message));
                RestoreResult result = await RestoreGameAsync(selectedGameDir, reporter);

                if (result.FirewallRuleRemoved)
                {
                    Log("🛡️ Firewall kuralı kaldırıldı.");
                }

                Log(result.Message);
            }
            catch (Exception ex)
            {
                Log($"HATA: {ErrorDoctor.Explain(ex)}");
            }
            finally
            {
                ShowProgress(false);
                SetBusy(false);
                await UpdateSystemStatusAsync();
                Interlocked.Exchange(ref _operationRunning, 0);
            }
        }

        private async Task<RestoreResult> RestoreGameAsync(string gameDir, IProgress<(double Percent, string Message)> reporter)
        {
            return await Task.Run(async () =>
            {
                var result = new RestoreResult();
                reporter.Report((10, "Geri alma kaydı okunuyor..."));
                PatchManifest? manifest = LoadManifest(gameDir);

                if (manifest == null)
                {
                    reporter.Report((40, "Yedekler aranıyor..."));
                    var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, MatchCasing = MatchCasing.CaseInsensitive };
                    string[] backups = Directory.GetFiles(gameDir, "steam_api*.dll.bak", options);
                    int i = 0;
                    foreach (string bakPath in backups)
                    {
                        i++;
                        reporter.Report((40.0 + 50.0 * i / Math.Max(1, backups.Length), $"Yedek geri yükleniyor ({i}/{backups.Length})..."));
                        string original = bakPath.Substring(0, bakPath.Length - 4);
                        File.Copy(bakPath, original, overwrite: true);
                        File.Delete(bakPath);
                        result.RestoredDllBackups++;
                    }

                    result.Message = result.RestoredDllBackups > 0
                        ? $"Manifest yoktu; yalnızca {result.RestoredDllBackups} adet DLL yedeği geri yüklendi."
                        : "Geri alma kaydı ve uygun yedek bulunamadı.";
                    return result;
                }

                int total = Math.Max(1, manifest.Entries.Count);
                int n = 0;
                foreach (PatchEntry entry in manifest.Entries.ToList())
                {
                    n++;
                    reporter.Report((10.0 + 50.0 * n / total, $"DLL geri yükleniyor ({n}/{total})..."));
                    if (File.Exists(entry.BackupPath))
                    {
                        File.Copy(entry.BackupPath, entry.DllPath, overwrite: true);
                        File.Delete(entry.BackupPath);
                        result.RestoredDllBackups++;
                    }
                }

                reporter.Report((65, "Metin dosyaları geri yükleniyor..."));
                foreach (FileBackup backup in manifest.TextBackups.ToList())
                {
                    if (File.Exists(backup.BackupPath))
                    {
                        File.Copy(backup.BackupPath, backup.OriginalPath, overwrite: true);
                        File.Delete(backup.BackupPath);
                        result.RestoredTextBackups++;
                    }
                }

                reporter.Report((80, "Araç dosyaları temizleniyor..."));
                foreach (string createdFile in manifest.CreatedFiles.ToList())
                {
                    if (File.Exists(createdFile))
                    {
                        File.Delete(createdFile);
                        result.RemovedCreatedFiles++;
                    }
                }

                foreach (string createdDirectory in manifest.CreatedDirectories.OrderByDescending(d => d.Length).ToList())
                {
                    if (Directory.Exists(createdDirectory) && !Directory.EnumerateFileSystemEntries(createdDirectory).Any())
                    {
                        Directory.Delete(createdDirectory, recursive: false);
                        result.RemovedCreatedDirectories = true;
                    }
                }

                if (!string.IsNullOrWhiteSpace(manifest.FirewallRuleName))
                {
                    reporter.Report((92, "Firewall kuralı kaldırılıyor..."));
                    ProcessResult delete = await RunNetshAsync($"advfirewall firewall delete rule name=\"{manifest.FirewallRuleName}\"");
                    result.FirewallRuleRemoved = delete.ExitCode == 0;
                }

                string manifestPath = GetManifestPath(gameDir);
                if (File.Exists(manifestPath))
                {
                    File.Delete(manifestPath);
                }

                result.Message = $"Geri yükleme tamamlandı. DLL yedeği: {result.RestoredDllBackups}, metin yedeği: {result.RestoredTextBackups}, oluşturulan dosya: {result.RemovedCreatedFiles}.";
                return result;
            });
        }

        private async Task RollbackManifestAsync(PatchManifest manifest)
        {
            await Task.Run(() =>
            {
                foreach (PatchEntry entry in manifest.Entries)
                {
                    if (File.Exists(entry.BackupPath))
                    {
                        File.Copy(entry.BackupPath, entry.DllPath, overwrite: true);
                    }
                }

                foreach (FileBackup backup in manifest.TextBackups)
                {
                    if (File.Exists(backup.BackupPath))
                    {
                        File.Copy(backup.BackupPath, backup.OriginalPath, overwrite: true);
                    }
                }

                foreach (string file in manifest.CreatedFiles)
                {
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }
                }

                foreach (string dir in manifest.CreatedDirectories.OrderByDescending(d => d.Length))
                {
                    if (Directory.Exists(dir) && !Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir, recursive: false);
                    }
                }
            });

            Log("↩️ Hata nedeniyle kısmi işlemler geri alındı.");
        }

        // ===================== Diğer =====================

        private void BtnLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(selectedExePath) || !File.Exists(selectedExePath))
            {
                MessageBox.Show("Başlatılacak EXE seçilmedi.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                Log($"🚀 Oyun başlatılıyor: {Path.GetFileName(selectedExePath)}");
                Process.Start(new ProcessStartInfo { FileName = "explorer.exe", Arguments = $"\"{selectedExePath}\"", UseShellExecute = true });
            }
            catch (Exception ex)
            {
                Log($"⚠️ explorer ile başlatılamadı ({ex.Message}); doğrudan başlatılıyor.");
                Process.Start(new ProcessStartInfo
                {
                    FileName = selectedExePath,
                    WorkingDirectory = Path.GetDirectoryName(selectedExePath) ?? selectedGameDir,
                    UseShellExecute = true
                });
            }
        }

        private async void BtnCopyLog_Click(object sender, RoutedEventArgs e)
        {
            await TrySetClipboardAsync(TxtLog.Text);
            Log("Log panoya kopyalandı.");
        }

        private async void BtnExportLog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "Log dosyası (*.log)|*.log|Tüm dosyalar (*.*)|*.*",
                FileName = $"steam-lan-control-{DateTime.Now:yyyyMMdd-HHmmss}.log"
            };

            if (dialog.ShowDialog() == true)
            {
                await File.WriteAllTextAsync(dialog.FileName, TxtLog.Text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
                Log($"Log dışa aktarıldı: {dialog.FileName}");
            }
        }

        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            TxtLog.Clear();
            Log("Log temizlendi.");
        }

        private async Task TrySetClipboardAsync(string text)
        {
            for (int i = 0; i < 3; i++)
            {
                try
                {
                    await Task.Run(() => System.Windows.Clipboard.SetText(text));
                    return;
                }
                catch
                {
                    await Task.Delay(120);
                }
            }
        }

        private void TxtFriendIp_TextChanged(object sender, TextChangedEventArgs e)
        {
            TxtPingStatus.Text = "Ping: -- ms";
            TxtPingStatus.Foreground = MutedBrush;
        }

        private async Task<bool> IsSelectedGameRunningAsync()
        {
            if (string.IsNullOrWhiteSpace(selectedExePath) || !File.Exists(selectedExePath))
            {
                return false;
            }

            string exeName = Path.GetFileNameWithoutExtension(selectedExePath);
            return await Task.Run(() =>
            {
                foreach (Process process in Process.GetProcessesByName(exeName))
                {
                    try
                    {
                        string? mainModule = process.MainModule?.FileName;
                        if (!string.IsNullOrWhiteSpace(mainModule) && string.Equals(mainModule, selectedExePath, StringComparison.OrdinalIgnoreCase))
                        {
                            return true;
                        }
                    }
                    catch
                    {
                        return true;
                    }
                }

                return false;
            });
        }

        private void SetBusy(bool busy)
        {
            BtnApply.IsEnabled = !busy;
            BtnRestore.IsEnabled = !busy;
            BtnBrowse.IsEnabled = !busy;
            BtnLaunch.IsEnabled = !busy && !string.IsNullOrWhiteSpace(selectedExePath) && File.Exists(selectedExePath);
        }

        private void LoadSettingsIntoUi()
        {
            try
            {
                if (File.Exists(_settingsPath))
                {
                    AppSettings? loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_settingsPath), JsonOptions);
                    if (loaded != null)
                    {
                        _settings.GameDir = loaded.GameDir;
                        _settings.ExeName = loaded.ExeName;
                        _settings.PlayerName = loaded.PlayerName;
                        _settings.AppId = loaded.AppId;
                        _settings.FriendIp = loaded.FriendIp;
                        _settings.Language = loaded.Language;
                        _settings.UnlockDlcTemplate = loaded.UnlockDlcTemplate;
                        _settings.AddFirewallRule = loaded.AddFirewallRule;
                        _settings.UseCustomBroadcast = loaded.UseCustomBroadcast;
                        _settings.Theme = string.IsNullOrWhiteSpace(loaded.Theme) ? "gece" : loaded.Theme;
                        _settings.UiLanguage = string.IsNullOrWhiteSpace(loaded.UiLanguage) ? "tr" : loaded.UiLanguage;
                        _settings.ShowMascot = loaded.ShowMascot;
                        _settings.Friends = loaded.Friends ?? new List<FriendEntry>();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Settings load failed: {ex}");
            }

            selectedGameDir = !string.IsNullOrWhiteSpace(_settings.GameDir) && Directory.Exists(_settings.GameDir) ? _settings.GameDir : string.Empty;
            TxtGamePath.Text = selectedGameDir;
            TxtPlayerName.Text = string.IsNullOrWhiteSpace(_settings.PlayerName) ? "Player1" : _settings.PlayerName;
            TxtAppId.Text = string.IsNullOrWhiteSpace(_settings.AppId) ? "480" : _settings.AppId;
            TxtFriendIp.Text = _settings.FriendIp;
            ChkUnlockDlc.IsChecked = _settings.UnlockDlcTemplate;
            ChkFirewall.IsChecked = _settings.AddFirewallRule;
            ChkCustomBroadcast.IsChecked = _settings.UseCustomBroadcast;
            ChkMascot.IsChecked = _settings.ShowMascot;

            foreach (ComboBoxItem item in CmbLanguage.Items.OfType<ComboBoxItem>())
            {
                if (item.Content is string language && string.Equals(language, _settings.Language, StringComparison.OrdinalIgnoreCase))
                {
                    item.IsSelected = true;
                    break;
                }
            }

            foreach (ComboBoxItem item in CmbTheme.Items.OfType<ComboBoxItem>())
            {
                if (item.Tag is string theme && theme == _settings.Theme)
                {
                    item.IsSelected = true;
                    break;
                }
            }

            foreach (ComboBoxItem item in CmbUiLang.Items.OfType<ComboBoxItem>())
            {
                if (item.Tag is string lang && lang == _settings.UiLanguage)
                {
                    item.IsSelected = true;
                    break;
                }
            }

            RefreshFriendList();
        }

        private void SaveSettings()
        {
            try
            {
                _settings.GameDir = selectedGameDir;
                _settings.ExeName = string.IsNullOrWhiteSpace(selectedExePath) ? string.Empty : Path.GetFileName(selectedExePath);
                _settings.PlayerName = TxtPlayerName.Text.Trim();
                _settings.AppId = TxtAppId.Text.Trim();
                _settings.FriendIp = TxtFriendIp.Text.Trim();
                _settings.Language = CmbLanguage.SelectedItem is ComboBoxItem selected && selected.Content is string lang ? lang : "english";
                _settings.UnlockDlcTemplate = ChkUnlockDlc.IsChecked == true;
                _settings.AddFirewallRule = ChkFirewall.IsChecked == true;
                _settings.UseCustomBroadcast = ChkCustomBroadcast.IsChecked == true;
                _settings.Theme = CmbTheme.SelectedItem is ComboBoxItem themeItem && themeItem.Tag is string th ? th : "gece";
                _settings.UiLanguage = CmbUiLang.SelectedItem is ComboBoxItem langItem && langItem.Tag is string ul ? ul : "tr";
                _settings.ShowMascot = ChkMascot.IsChecked == true;

                File.WriteAllText(_settingsPath, JsonSerializer.Serialize(_settings, JsonOptions));
            }
            catch (Exception ex)
            {
                Log($"⚠️ Ayarlar kaydedilemedi: {ex.Message}");
            }
        }

        private string GetManifestPath(string gameDir)
        {
            return Path.Combine(_manifestDir, Core.ComputeKey(gameDir) + ".json");
        }

        private PatchManifest? LoadManifest(string gameDir)
        {
            try
            {
                string path = GetManifestPath(gameDir);
                if (!File.Exists(path))
                {
                    return null;
                }

                PatchManifest? manifest = JsonSerializer.Deserialize<PatchManifest>(File.ReadAllText(path), JsonOptions);
                if (manifest == null || !string.Equals(manifest.GameDir, gameDir, StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                return manifest;
            }
            catch
            {
                return null;
            }
        }

        private void SaveManifest(PatchManifest manifest)
        {
            File.WriteAllText(GetManifestPath(manifest.GameDir), JsonSerializer.Serialize(manifest, JsonOptions), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private sealed record ProcessResult(int ExitCode, string OutputText, string ErrorText);
    }
}
