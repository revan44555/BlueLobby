using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using BlueLobby.Core;

namespace BlueLobby
{
    // İş mantığı: durum, yama/geri yükleme/başlatma, arkadaşlar, durum güncellemeleri.
    // Görsel düzen MainWindow.Ui.cs + Ui.cs dosyalarındadır.
    public partial class MainWindow : Window
    {
        private readonly AppSettings _settings;
        private string _gameDir = string.Empty;
        private string _exePath = string.Empty;
        private int _busy;
        private string _section = "game";
        private string _logBuffer = string.Empty;

        public MainWindow()
        {
            _settings = App.Store.LoadSettings();
            Loc.SetLanguage(_settings.UiLanguage);
            ThemeManager.Get(_settings.Theme);

            // Steam Deck algılama: dokunmatik modu otomatik aç
            string? steamDeck = Environment.GetEnvironmentVariable("STEAM_DECK");
            if (steamDeck == "1")
            {
                _settings.TouchMode = true;
            }
            Ui.TouchMode = _settings.TouchMode;

            Title = "BlueLobby v3.1";
            Width = 1100; Height = 740; MinWidth = 860; MinHeight = 600;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowState = WindowState.Maximized;

            KeyDown += (_, e) =>
            {
                if (e.Key == Key.F11) ToggleFullscreen();
            };
            DragDrop.SetAllowDrop(this, true);
            AddHandler(DragDrop.DropEvent, Window_Drop);

            Content = BuildUi();
            ApplyTheme();
            StartMascot();

            _ = UpdateSystemStatusAsync();
            _ = UpdateChecker.CheckAsync(this);

            if (!string.IsNullOrWhiteSpace(_settings.GameDir) && Directory.Exists(_settings.GameDir))
            {
                _ = SelectGameFolderAsync(_settings.GameDir);
            }

            Log("BlueLobby v3.1 başlatıldı. Klasörü seçebilir veya pencereye sürükleyebilirsiniz.");
            _ = SuggestAutoSetupIfVpnMissingAsync();

            if (!_settings.WizardDone)
            {
                SetSection("wizard");
                RenderWizardStep();
                Log("🧙 İlk kurulum için sihirbaz açıldı — 3 adımda hazır olursun.");
            }
        }

        private void ToggleFullscreen()
        {
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
        }

        // ===================== Log =====================

        private void Log(string line)
        {
            string entry = $"[{DateTime.Now:HH:mm:ss}] {line}{Environment.NewLine}";
            _logBuffer += entry;
            if (_logBox != null)
            {
                _logBox.Text = _logBuffer;
                _logBox.CaretIndex = _logBox.Text.Length;
            }
            App.Store.AppendLog(line);
        }

        private async Task CopyTextAsync(string text)
        {
            try
            {
                var top = TopLevel.GetTopLevel(this);
                if (top?.Clipboard != null)
                {
                    await top.Clipboard.SetTextAsync(text);
                    Log("📋 Panoya kopyalandı.");
                }
            }
            catch (Exception ex)
            {
                Log($"⚠️ Kopyalanamadı: {ex.Message}");
            }
        }

        private void BtnExportLog_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                string dir = Path.GetDirectoryName(App.Store.LogPath)!;
                string target = Path.Combine(dir, $"bluelobby-export-{DateTime.Now:yyyyMMdd-HHmmss}.log");
                File.WriteAllText(target, _logBuffer);
                Log($"💾 Log dışa aktarıldı: {target}");
            }
            catch (Exception ex)
            {
                Log($"⚠️ Dışa aktarma başarısız: {ex.Message}");
            }
        }

        // ===================== Durum çipleri =====================

        private static void SetChip(TextBlock? chip, string text, IBrush brush)
        {
            if (chip == null) return;
            chip.Text = text;
            chip.Foreground = brush;
        }

        private async Task UpdateSystemStatusAsync()
        {
            try
            {
                bool vpnActive = await App.Services.IsVpnActiveAsync();
                string? vpnIp = App.Services.GetVpnIpv4();

                string ipText = _txtFriendIp?.Text?.Trim() ?? string.Empty;
                string pingText; IBrush pingBrush;
                if (IPAddress.TryParse(ipText, out IPAddress? ip) && ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    try
                    {
                        using var pinger = new Ping();
                        PingReply reply = await pinger.SendPingAsync(ip, 800);
                        if (reply.Status == IPStatus.Success)
                        {
                            long ms = reply.RoundtripTime;
                            pingText = $"Ping: {ms} ms";
                            pingBrush = ms < 60 ? Ui.Ok : ms < 150 ? Ui.Mid : Ui.Err;
                        }
                        else
                        {
                            pingText = "Ping: Erişilemiyor";
                            pingBrush = Ui.Err;
                        }
                    }
                    catch
                    {
                        pingText = "Ping: Ölçülemedi";
                        pingBrush = Ui.Mid;
                    }
                }
                else
                {
                    pingText = "Ping: -- ms";
                    pingBrush = Ui.Brush(Ui.C.Muted);
                }

                bool emu64 = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, OperatingSystem.IsWindows() ? "steam_api64.dll" : "steam_api64.so"))
                          || File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steam_api64.dll"));
                bool emu32 = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, OperatingSystem.IsWindows() ? "steam_api.dll" : "libsteam_api.so"))
                          || File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "steam_api.dll"));
                string emuText; IBrush emuBrush;
                if (emu64 || emu32)
                {
                    emuText = Loc.T("emu_ok") + (emu64 && emu32 ? "" : emu64 ? " (yalnız 64-bit)" : " (yalnız 32-bit)");
                    emuBrush = Ui.Ok;
                }
                else
                {
                    emuText = Loc.T("emu_missing");
                    emuBrush = Ui.Mid;
                }

                string patchText; IBrush patchBrush;
                if (string.IsNullOrWhiteSpace(_gameDir) || !Directory.Exists(_gameDir))
                {
                    patchText = Loc.T("patch_none");
                    patchBrush = Ui.Err;
                }
                else
                {
                    int count = await Task.Run(() => GameScanner.FindApiDllTargets(_gameDir).Count);
                    PatchManifest? mf = App.Store.LoadManifest(_gameDir);
                    if (mf != null && BackupValidator.IsPatchStale(mf))
                    {
                        patchText = "🟠 Yama sıfırlanmış — tekrar Uygula";
                        patchBrush = Ui.Mid;
                    }
                    else
                    {
                        patchText = count > 0 ? $"🟢 {count} hedef hazır" : "🟡 Hedef bulunamadı";
                        patchBrush = count > 0 ? Ui.Ok : Ui.Mid;
                    }
                }

                // Uyumluluk (madde B): AppID'ye göre oyun durumu
                CompatResult? compat = null;
                if (TryResolveAppId(out int appIdResolved))
                {
                    compat = App.Compat.Lookup(appIdResolved);
                }

                Dispatcher.UIThread.Post(() =>
                {
                    if (compat != null)
                    {
                        SetChip(_chipCompat, $"Uyumluluk: {compat.Display}",
                            compat.Status == CompatStatus.Works ? Ui.Ok :
                            compat.Status == CompatStatus.NeedsConfig ? Ui.Mid :
                            compat.Status == CompatStatus.Unsupported ? Ui.Err :
                            Ui.Brush(Ui.C.Muted));
                    }
                    else
                    {
                        SetChip(_chipCompat, "Uyumluluk: --", Ui.Brush(Ui.C.Muted));
                    }
                    SetChip(_chipVpn, vpnActive
                        ? $"🟢 VPN: {(vpnIp ?? Loc.T("radmin_check"))}"
                        : "🔴 VPN: Bulunamadı / Kapalı", vpnActive ? Ui.Ok : Ui.Err);
                    SetChip(_chipPing, pingText, pingBrush);
                    SetChip(_chipEmu, emuText, emuBrush);
                    SetChip(_chipPatch, patchText, patchBrush);
                });
            }
            catch
            {
                // Durum güncellemesi isteğe bağlıdır.
            }
        }

        private bool TryResolveAppId(out int appId)
        {
            appId = 0;
            if (int.TryParse(_txtAppId?.Text?.Trim(), out int fromBox) && fromBox > 0)
            {
                appId = fromBox;
                return true;
            }
            if (!string.IsNullOrWhiteSpace(_gameDir))
            {
                try
                {
                    string sidPath = Path.Combine(_gameDir, "steam_appid.txt");
                    if (File.Exists(sidPath) &&
                        int.TryParse(File.ReadAllText(sidPath).Trim(), out int fromFile) && fromFile > 0)
                    {
                        appId = fromFile;
                        return true;
                    }
                }
                catch
                {
                    // Okunamadı: bilinmiyor kalır.
                }
            }
            return false;
        }

        private async Task SuggestAutoSetupIfVpnMissingAsync()
        {
            try
            {
                bool vpnActive = await App.Services.IsVpnActiveAsync();
                if (!vpnActive)
                {
                    Log($"🟡 VPN ağı algılanamadı. '{Loc.T("autoscan")}' ile standart LAN ayarlarını uygulayabilir veya '{Loc.T("dl_radmin")}' ile istemciyi kurabilirsiniz.");
                }
            }
            catch
            {
                // Öneri isteğe bağlıdır.
            }
        }

        // ===================== Klasör / çalıştırıcı =====================

        private async void BtnBrowse_Click(object? sender, RoutedEventArgs e)
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions { Title = Loc.T("gamefolder"), AllowMultiple = false });
            if (folders.Count > 0)
            {
                await SelectGameFolderAsync(folders[0].Path.LocalPath);
            }
        }

        private async void Window_Drop(object? sender, DragEventArgs e)
        {
            var items = e.Data.GetFiles()?.ToList();
            if (items == null || items.Count != 1) return;

            string path = items[0].Path.LocalPath;
            if (File.Exists(path))
            {
                path = Path.GetDirectoryName(path) ?? path;
            }

            await SelectGameFolderAsync(path);
        }

        private void CmbExes_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_cmbExes?.SelectedItem is not string name || string.IsNullOrWhiteSpace(_gameDir)) return;

            _exePath = Path.Combine(_gameDir, name);
            Log($"Çalıştırıcı seçildi: {name}");
        }

        private async Task SelectGameFolderAsync(string dir)
        {
            _gameDir = dir;
            Dispatcher.UIThread.Post(() => { if (_txtGameDir != null) _txtGameDir.Text = dir; });
            Dispatcher.UIThread.Post(() => _cmbExes?.Items.Clear());
            _exePath = string.Empty;

            await Task.Run(() =>
            {
                var options = new EnumerationOptions { IgnoreInaccessible = true };
                foreach (string f in Directory.EnumerateFiles(dir, "*", options))
                {
                    string ext = Path.GetExtension(f).ToLowerInvariant();
                    bool candidate = ext == ".exe" || ext == ".sh" || ext == ".x86_64" || ext == ".x86" ||
                                     (ext == string.Empty && !Path.GetFileName(f).Contains("."));
                    if (candidate)
                    {
                        string name = Path.GetFileName(f);
                        Dispatcher.UIThread.Post(() => _cmbExes?.Items.Add(name));
                    }
                }
            });

            int targets = await Task.Run(() => GameScanner.FindApiDllTargets(dir).Count);
            Log(targets > 0
                ? $"🟢 Klasör seçildi: {targets} API hedefi bulundu (dll/so)."
                : "🟡 Klasör seçildi ama API hedefi bulunamadı.");

            if (OperatingSystem.IsLinux() && targets > 0)
            {
                var protons = GameLibrary.FindProtonVersions(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                Log(protons.Count > 0
                    ? $"🐧 Proton önerisi: {string.Join(", ", protons)} — oyunda sorun olursa bu sürümlerden birini dene."
                    : "🐧 Proton bulunamadı — oyun native çalışmıyorsa Steam'den Proton kur.");
            }

            GameProfile? savedProfile = App.Profiles.Load(dir);
            if (savedProfile != null)
            {
                ApplyProfileToUi(savedProfile);
                Log($"📌 '{Path.GetFileName(dir)}' ayarları profilden yüklendi (oyuncu: {savedProfile.PlayerName}, AppID: {savedProfile.AppId}).");
            }

            if (TryResolveAppId(out int wizAppId))
            {
                CompatResult cr = App.Compat.Lookup(wizAppId);
                if (cr.Status != CompatStatus.Unknown)
                {
                    Log($"🎮 {cr.Display}{(string.IsNullOrWhiteSpace(cr.Note) ? "" : $" — {cr.Note}")}");
                }
            }

            _wizardHasFolder = true;
            if (_section == "wizard")
            {
                Dispatcher.UIThread.Post(RenderWizardStep);
            }

            await UpdateSystemStatusAsync();
        }

        // ===================== Bileşen ara / otomatik kurulum / ping =====================

        private async void BtnFindEmu_Click(object? sender, RoutedEventArgs e)
        {
            Log(Loc.T("find_emu") + "...");
            string[] names = OperatingSystem.IsWindows()
                ? new[] { "steam_api64.dll", "steam_api.dll" }
                : new[] { "steam_api64.so", "libsteam_api.so" };
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            var searchRoots = new List<string>();
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!string.IsNullOrWhiteSpace(home))
            {
                searchRoots.Add(Path.Combine(home, "Downloads"));
                searchRoots.Add(Path.Combine(home, "Desktop"));
                searchRoots.Add(Path.Combine(home, "Documents"));
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
                        MaxRecursionDepth = 6
                    };

                    foreach (string name in names)
                    {
                        if (found.ContainsKey(name)) continue;

                        try
                        {
                            foreach (string path in Directory.EnumerateFiles(root, "*", opts))
                            {
                                if (Path.GetFileName(path).Equals(name, StringComparison.OrdinalIgnoreCase))
                                {
                                    found[name] = path;
                                    break;
                                }
                            }
                        }
                        catch
                        {
                            // Klasör taranamadı: atla.
                        }
                    }
                }

                foreach (KeyValuePair<string, string> kv in found)
                {
                    try
                    {
                        File.Copy(kv.Value, Path.Combine(baseDir, kv.Key), overwrite: true);
                    }
                    catch
                    {
                        // Kopyalanamadı.
                    }
                }
            });

            foreach (KeyValuePair<string, string> kv in found)
            {
                Log($"✅ Bileşen kopyalandı: {kv.Key} ← {kv.Value}");
            }
            if (found.Count == 0)
            {
                Log("🟡 Hiçbir bileşen dosyası bulunamadı (Downloads/Desktop/Documents).");
            }

            await UpdateSystemStatusAsync();
        }

        private async void BtnAutoSetup_Click(object? sender, RoutedEventArgs e)
        {
            if (_chkUnlockDlc != null) _chkUnlockDlc.IsChecked = false;
            if (_chkFirewall != null) _chkFirewall.IsChecked = App.Services.SupportsFirewall;
            if (_chkCustomBroadcast != null) _chkCustomBroadcast.IsChecked = true;

            string? vpnIp = await Task.Run(() => App.Services.GetVpnIpv4());
            if (!string.IsNullOrWhiteSpace(vpnIp) && _txtFriendIp != null)
            {
                _txtFriendIp.Text = vpnIp;
                Log($"✅ VPN IPv4 algılandı: {vpnIp}");
            }
            else
            {
                Log("⚠️ VPN IPv4 algılanamadı. VPN ağını kontrol edin.");
            }

            SaveUiToSettings();
            await UpdateSystemStatusAsync();
        }

        private async void BtnPingTest_Click(object? sender, RoutedEventArgs e)
        {
            await UpdateSystemStatusAsync();
            Log("Ping testi tamamlandı.");
        }

        // ===================== Arkadaş listesi =====================

        private void RefreshFriendList()
        {
            _lstFriends?.Items.Clear();
            foreach (FriendEntry friend in _settings.Friends)
            {
                _lstFriends?.Items.Add($"{friend.Name} — {friend.Ip}");
            }
        }

        private void BtnAddFriend_Click(object? sender, RoutedEventArgs e)
        {
            string name = _txtFriendName?.Text?.Trim() ?? string.Empty;
            string ip = _txtFriendIpNew?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(ip))
            {
                Log("❌ Arkadaş eklemek için isim ve IP gerekli.");
                return;
            }

            _settings.Friends.RemoveAll(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
            _settings.Friends.Add(new FriendEntry { Name = name, Ip = ip, AddedUtc = DateTime.UtcNow.ToString("O") });
            App.Store.SaveSettings(_settings);
            RefreshFriendList();
            if (_txtFriendName != null) _txtFriendName.Text = string.Empty;
            if (_txtFriendIpNew != null) _txtFriendIpNew.Text = string.Empty;
            Log($"👥 Arkadaş eklendi: {name}");
        }

        private void BtnRemoveFriend_Click(object? sender, RoutedEventArgs e)
        {
            if (_lstFriends?.SelectedItem is not string selected) return;

            string name = selected.Split('—')[0].Trim();
            _settings.Friends.RemoveAll(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
            App.Store.SaveSettings(_settings);
            RefreshFriendList();
            Log($"👥 Arkadaş silindi: {name}");
        }

        private void LstFriends_DoubleTapped(object? sender, TappedEventArgs e)
        {
            if (_lstFriends?.SelectedItem is not string selected) return;

            string[] parts = selected.Split('—');
            if (parts.Length > 1 && _txtFriendIp != null)
            {
                _txtFriendIp.Text = parts[^1].Trim();
                SetSection("game");
                Log($"👥 {Loc.T("friend_used")}: {parts[^1].Trim()}");
            }
        }

        // ===================== Ayar değişiklikleri =====================

        private void CmbTheme_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox)?.SelectedItem is ComboBoxItem item && item.Tag is string theme)
            {
                _settings.Theme = theme;
                App.Store.SaveSettings(_settings);
                ApplyTheme();
            }
        }

        private void CmbUiLang_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox)?.SelectedItem is ComboBoxItem item && item.Tag is string lang)
            {
                _settings.UiLanguage = lang;
                App.Store.SaveSettings(_settings);
                Loc.SetLanguage(lang);
                RebuildUi();
            }
        }

        private void ChkTouch_Changed(object? sender, RoutedEventArgs e)
        {
            _settings.TouchMode = (sender as CheckBox)?.IsChecked == true;
            App.Store.SaveSettings(_settings);
            Ui.TouchMode = _settings.TouchMode;
            RebuildUi();
        }

        private void ChkMascot_Changed(object? sender, RoutedEventArgs e)
        {
            _settings.ShowMascot = (sender as CheckBox)?.IsChecked == true;
            App.Store.SaveSettings(_settings);
            if (_mascotBlock != null) _mascotBlock.IsVisible = _settings.ShowMascot;
        }

        // ===================== Yama / geri yükleme / başlatma =====================

        private async void BtnApply_Click(object? sender, RoutedEventArgs e)
        {
            if (Interlocked.Exchange(ref _busy, 1) == 1) return;

            try
            {
                if (string.IsNullOrWhiteSpace(_gameDir) || !Directory.Exists(_gameDir))
                {
                    Log("❌ Önce geçerli bir oyun klasörü seçin.");
                    return;
                }

                // Güvenlik ağı (C): yanlış klasör koruması
                var verify = GameVerifier.VerifyGameFolder(_gameDir);
                if (!verify.LooksLikeGame)
                {
                    var warn = new ConfirmDialog("Klasör doğrulama",
                        new[]
                        {
                            $"Bu klasör oyun kurulumu gibi görünmüyor: {verify.Reason}",
                            "Yine de devam etmek istiyor musunuz?",
                        },
                        Loc.T("yes"), Loc.T("no"));
                    bool proceed = await warn.ShowDialog<bool>(this);
                    if (!proceed)
                    {
                        Log("İşlem iptal edildi (klasör doğrulama).");
                        return;
                    }
                }

                if (!int.TryParse(_txtAppId?.Text?.Trim(), out int appId) || appId <= 0)
                {
                    Log("❌ AppID pozitif sayı olmalıdır.");
                    return;
                }

                if (!string.IsNullOrWhiteSpace(_exePath) && await App.Services.IsGameRunningAsync(_exePath))
                {
                    Log("❌ Seçilen oyun şu an çalışıyor gibi görünüyor. Önce oyundan tamamen çıkın.");
                    return;
                }

                var targets = await Task.Run(() => GameScanner.FindApiDllTargets(_gameDir));
                if (targets.Count == 0)
                {
                    Log("❌ Hedef API dosyası bulunamadı.");
                    return;
                }

                var buffers = new List<(ApiDllTarget Target, byte[] Bytes)>();
                foreach (ApiDllTarget t in targets)
                {
                    byte[]? bytes = PatchEngine.LoadComponentBytes(AppDomain.CurrentDomain.BaseDirectory, t);
                    if (bytes == null || bytes.Length == 0)
                    {
                        Log($"❌ {(t.Is64Bit ? "64-bit" : "32-bit")} bileşen dosyası ({GameScanner.GetComponentFileName(t)}) bulunamadı. Hiçbir dosya değiştirilmedi.");
                        return;
                    }
                    buffers.Add((t, bytes));
                }

                var manifest = App.Store.LoadManifest(_gameDir) ?? new PatchManifest
                {
                    GameDir = _gameDir,
                    CreatedUtc = DateTime.UtcNow.ToString("O")
                };

                var options = new PatchEngine.PatchOptions(
                    _txtPlayerName?.Text?.Trim() ?? string.Empty,
                    (_cmbLanguage?.SelectedItem as ComboBoxItem)?.Content as string ?? "english",
                    _chkCustomBroadcast?.IsChecked == true ? _txtFriendIp?.Text?.Trim() ?? string.Empty : string.Empty,
                    _chkUnlockDlc?.IsChecked == true,
                    appId);

                try
                {
                    await Task.Run(() =>
                    {
                        foreach ((ApiDllTarget target, byte[] bytes) in buffers)
                        {
                            App.Engine.ApplyPatchToTarget(target, bytes, options, manifest, Log);
                        }
                    });

                    if (_chkFirewall?.IsChecked == true && _chkFirewall.IsVisible && !string.IsNullOrWhiteSpace(_exePath))
                    {
                        if (!App.Services.IsElevated)
                        {
                            Log("⚠️ Güvenlik duvarı kuralı için yönetici yetkisi gerekli. Kural atlandı; isterseniz programı sağ tık → 'Yönetici olarak çalıştır' ile açıp yeniden uygulayın.");
                        }
                        else
                        {
                            string? rule = await App.Services.EnsureFirewallRuleAsync(_exePath);
                            if (rule != null)
                            {
                                manifest.FirewallRuleName = rule;
                                manifest.FirewallExePath = _exePath;
                            }
                            else
                            {
                                Log("⚠️ Güvenlik duvarı kuralı eklenemedi.");
                            }
                        }
                    }

                    manifest.CreatedUtc = DateTime.UtcNow.ToString("O");
                    App.Store.SaveManifest(manifest);
                    Log($"🎉 İşlem tamamlandı. Hedef sayısı: {buffers.Count}. Geri almak için 'Geri Yükle' kullanın.");
                }
                catch (Exception ex)
                {
                    await App.Engine.RollbackManifestAsync(manifest);
                    Log($"HATA: {ErrorDoctor.Explain(ex)}");
                }
            }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
                SaveUiToSettings();
                await UpdateSystemStatusAsync();
            }
        }

        private async void BtnRestore_Click(object? sender, RoutedEventArgs e)
        {
            if (Interlocked.Exchange(ref _busy, 1) == 1) return;

            try
            {
                if (string.IsNullOrWhiteSpace(_gameDir))
                {
                    Log("❌ Önce oyun klasörü seçin.");
                    return;
                }

                PatchManifest? manifest = App.Store.LoadManifest(_gameDir);
                if (manifest == null)
                {
                    Log("ℹ️ Manifest bulunamadı; yalnızca .bak yedekleri geri yüklenir.");
                }
                else
                {
                    // Güvenlik ağı (C): yedek sağlamlık kontrolü
                    var backupCheck = BackupValidator.Validate(manifest);
                    if (!backupCheck.Valid)
                    {
                        var lines = new List<string> { "Yedeklerden bazıları eksik veya bozuk — geri yükleme güvenli değil:", string.Empty };
                        lines.AddRange(backupCheck.Problems);
                        await new ConfirmDialog("Yedek doğrulama başarısız", lines, Loc.T("yes"), string.Empty).ShowDialog<bool>(this);
                        Log($"❌ Geri yükleme durduruldu: {backupCheck.Problems.Count} yedek sorunu var.");
                        return;
                    }
                }
                if (manifest != null)
                {
                    var lines = new List<string>
                    {
                        $"{manifest.Entries.Count} API yedeği geri yüklenir.",
                        $"{manifest.TextBackups.Count} metin dosyası yedeği geri yüklenir.",
                        $"{manifest.CreatedFiles.Count} araç dosyası silinir.",
                    };
                    if (!string.IsNullOrWhiteSpace(manifest.FirewallRuleName))
                    {
                        lines.Add($"Güvenlik duvarı kuralı kaldırılır: {manifest.FirewallRuleName}");
                    }

                    var confirm = new ConfirmDialog(Loc.T("restore_q"), lines, Loc.T("yes"), Loc.T("no"));
                    bool yes = await confirm.ShowDialog<bool>(this);
                    if (!yes)
                    {
                        Log("Geri yükleme iptal edildi.");
                        return;
                    }
                }

                var progress = new Progress<(double Percent, string Message)>(p =>
                {
                    if (_statusBarText != null) _statusBarText.Text = p.Message;
                });
                RestoreResult result = await App.Engine.RestoreGameAsync(_gameDir, manifest, progress);
                App.Store.DeleteManifest(_gameDir);
                Log($"🔄 {result.Message}");
            }
            catch (Exception ex)
            {
                Log($"HATA: {ErrorDoctor.Explain(ex)}");
            }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
                if (_statusBarText != null) _statusBarText.Text = string.Empty;
                await UpdateSystemStatusAsync();
            }
        }

        private async void BtnLaunch_Click(object? sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_exePath))
            {
                Log("❌ Başlatılacak çalıştırıcı seçilmedi.");
                return;
            }

            Log($"🚀 Oyun başlatılıyor: {Path.GetFileName(_exePath)}");
            await App.Services.LaunchGameAsync(_exePath, _gameDir);
        }

        private void BtnVpnDownload_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = App.Services.VpnDownloadUrl,
                    UseShellExecute = true
                });
                Log("🌐 VPN indirme sayfası açıldı.");
            }
            catch (Exception ex)
            {
                Log($"⚠️ Sayfa açılamadı: {ex.Message}");
            }
        }

        // ===================== Oyun başına profil (madde 1) =====================

        private void ApplyProfileToUi(GameProfile p)
        {
            if (_txtPlayerName != null) _txtPlayerName.Text = p.PlayerName;
            if (_txtAppId != null) _txtAppId.Text = p.AppId;
            if (_chkCustomBroadcast != null) _chkCustomBroadcast.IsChecked = p.BroadcastOn;
            if (_chkUnlockDlc != null) _chkUnlockDlc.IsChecked = p.DlcTemplateOn;
            if (_chkFirewall != null) _chkFirewall.IsChecked = p.FirewallOn;

            if (_cmbLanguage != null && !string.IsNullOrWhiteSpace(p.Language))
            {
                for (int i = 0; i < _cmbLanguage.Items.Count; i++)
                {
                    if ((_cmbLanguage.Items[i] as ComboBoxItem)?.Content as string == p.Language)
                    {
                        _cmbLanguage.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private GameProfile CurrentProfileFromUi()
        {
            return new GameProfile
            {
                GameDir = _gameDir,
                PlayerName = _txtPlayerName?.Text ?? string.Empty,
                AppId = _txtAppId?.Text ?? string.Empty,
                Language = (_cmbLanguage?.SelectedItem as ComboBoxItem)?.Content as string ?? "english",
                BroadcastOn = _chkCustomBroadcast?.IsChecked == true,
                DlcTemplateOn = _chkUnlockDlc?.IsChecked == true,
                FirewallOn = _chkFirewall?.IsChecked == true,
            };
        }

        // ===================== Paylaşılabilir profil (madde 5) =====================

        private async void BtnShareProfile_Click(object? sender, RoutedEventArgs e)
        {
            GameProfile current = CurrentProfileFromUi();
            string token = SessionProfileCodec.Encode(current);
            await CopyTextAsync(token);

            await new ConfirmDialog("🔗 Profil kodu (panoya kopyalandı)",
                new[] { "Bu kodu arkadaşına gönder; o Profil Al ile yapıştırıp aynı ayarlarla kurulum yapar." },
                Loc.T("yes"), string.Empty).ShowDialog<bool>(this);
            Log($"🔗 Profil kodu oluşturuldu ve panoya kopyalandı ({token.Length} karakter).");
        }

        private async void BtnImportProfile_Click(object? sender, RoutedEventArgs e)
        {
            var input = new InputDialog("📥 Profil Al", "Arkadaşından aldığın BL1... kodunu yapıştır");
            bool ok = await input.ShowDialog<bool>(this);
            if (!ok || string.IsNullOrWhiteSpace(input.ResultText))
            {
                return;
            }

            GameProfile? profile = SessionProfileCodec.Decode(input.ResultText.Trim());
            if (profile == null)
            {
                Log("❌ Geçersiz profil kodu — kod eksik kopyalanmış olabilir, yeniden isteyin.");
                await new ConfirmDialog("📥 Profil Al", new[] { "Bu kod geçersiz görünüyor. Lütfen kodun tamamını kopyalayıp yeniden deneyin." },
                    Loc.T("yes"), string.Empty).ShowDialog<bool>(this);
                return;
            }

            var preview = new ConfirmDialog("📥 Profil uygulansın mı?",
                new[]
                {
                    $"Oyuncu Adı: {profile.PlayerName}",
                    $"AppID: {profile.AppId}",
                    $"Dil: {profile.Language}",
                    $"VPN Lobi Sabitleme: {(profile.BroadcastOn ? "açık" : "kapalı")}",
                    $"DLC şablonu: {(profile.DlcTemplateOn ? "açık" : "kapalı")}",
                },
                Loc.T("yes"), Loc.T("no"));
            bool apply = await preview.ShowDialog<bool>(this);
            if (!apply)
            {
                Log("Profil alma iptal edildi.");
                return;
            }

            ApplyProfileToUi(profile);
            Log($"📥 Profil uygulandı — oyuncu: {profile.PlayerName}, AppID: {profile.AppId}.");
        }

        // ===================== Ayarlar =====================

        private void SaveUiToSettings()
        {
            _settings.GameDir = _gameDir;
            _settings.ExeName = Path.GetFileName(_exePath);
            _settings.PlayerName = _txtPlayerName?.Text ?? string.Empty;
            _settings.AppId = _txtAppId?.Text ?? string.Empty;
            _settings.FriendIp = _txtFriendIp?.Text ?? string.Empty;
            App.Store.SaveSettings(_settings);

            if (!string.IsNullOrWhiteSpace(_gameDir))
            {
                App.Profiles.Save(CurrentProfileFromUi());
            }
        }
    }
}
