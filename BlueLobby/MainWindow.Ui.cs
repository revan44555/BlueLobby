using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using BlueLobby.Core;

namespace BlueLobby
{
    // Arayüz kurulumu — iş mantığı MainWindow.axaml.cs'dedir.
    // Görsel düzen revizyonları bu dosyada (ve Ui.cs'de) yapılmalı.
    public partial class MainWindow
    {
        // ---- Tema uygulanan referanslar ----
        private Border? _sidebarBorder;
        private TextBlock? _titleBlock;
        private TextBlock? _subtitleBlock;
        private TextBlock? _platformBlock;
        private TextBlock? _statusBarText;
        private TextBox? _logBox;
        private readonly List<TextBlock> _mutedBlocks = new();
        private readonly List<Button> _navButtons = new();
        private TextBlock? _mascotBlock;
        private TranslateTransform? _mascotTransform;
        private DispatcherTimer? _mascotTimer;
        private int _mascotTick;

        // ---- Oyun bölümü ----
        private TextBox? _txtGameDir;
        private ComboBox? _cmbExes;
        private TextBox? _txtPlayerName;
        private TextBox? _txtAppId;
        private TextBox? _txtFriendIp;
        private ComboBox? _cmbLanguage;
        private CheckBox? _chkUnlockDlc;
        private CheckBox? _chkCustomBroadcast;
        private CheckBox? _chkFirewall;
        private TextBlock? _chipVpn;
        private TextBlock? _chipPing;
        private TextBlock? _chipEmu;
        private TextBlock? _chipPatch;
        private TextBlock? _chipCompat;

        // ---- Arkadaş bölümü ----
        private TextBox? _txtFriendName;
        private TextBox? _txtFriendIpNew;
        private ListBox? _lstFriends;

        // ---- Bölüm panelleri ----
        private Control? _pnlGame;
        private Control? _pnlFriends;
        private Control? _pnlSettings;
        private Control? _pnlLog;
        private Control? _pnlWizard;
        private Control? _pnlLearn;

        // ---- Sihirbaz ----
        private int _wizardStep = 1;
        private bool _wizardHasFolder;
        private TextBlock? _lblWizardTitle;
        private StackPanel? _wizardBody;
        private Button? _btnWizBack;
        private Button? _btnWizNext;
        private Button? _btnWizFinish;

        // ===================== UI kurulumu =====================

        private Control BuildUi()
        {
            _mutedBlocks.Clear();
            _navButtons.Clear();

            _titleBlock = Ui.Title(Loc.T("title"));
            _subtitleBlock = Ui.Muted(Loc.T("subtitle"));
            _subtitleBlock.Margin = new Avalonia.Thickness(0, 2, 0, 14);

            _platformBlock = Ui.Muted($"Platform: {(OperatingSystem.IsWindows() ? "Windows" : "Linux")}");
            _platformBlock.Margin = new Avalonia.Thickness(0, 16, 0, 0);
            _mutedBlocks.Add(_platformBlock);

            _chipVpn = Ui.ChipText("VPN: --");

            _mascotBlock = new TextBlock
            {
                Text = "🤖", FontSize = 34,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Avalonia.Thickness(0, 16, 0, 0),
                RenderTransform = _mascotTransform ??= new TranslateTransform(),
                IsVisible = _settings.ShowMascot
            };
            _mascotBlock.Tapped += (_, _) => _ = new ConfirmDialog("Mavi 🤖", new[] { Loc.T("mascot_tip") }, Loc.T("yes"), string.Empty).ShowDialog<bool>(this);

            var sidebarPanel = new StackPanel { Margin = new Avalonia.Thickness(14) };
            sidebarPanel.Children.Add(_titleBlock);
            sidebarPanel.Children.Add(_subtitleBlock);
            sidebarPanel.Children.Add(NavButton("nav_game", "game"));
            sidebarPanel.Children.Add(NavButton("nav_friends", "friends"));
            sidebarPanel.Children.Add(NavButton("nav_settings", "settings"));
            sidebarPanel.Children.Add(NavButton("nav_log", "log"));
            sidebarPanel.Children.Add(_platformBlock);
            sidebarPanel.Children.Add(Ui.Chip(_chipVpn));
            sidebarPanel.Children.Add(_mascotBlock);

            _sidebarBorder = new Border { Child = sidebarPanel };

            _pnlGame = BuildGameSection();
            _pnlFriends = BuildFriendsSection();
            _pnlSettings = BuildSettingsSection();
            _pnlLog = BuildLogSection();
            _pnlWizard = BuildWizardSection();
            _pnlLearn = BuildLearnSection();

            var sections = new Grid();
            sections.Children.Add(_pnlGame);
            sections.Children.Add(_pnlFriends);
            sections.Children.Add(_pnlSettings);
            sections.Children.Add(_pnlLog);
            sections.Children.Add(_pnlWizard);
            sections.Children.Add(_pnlLearn);

            _statusBarText = Ui.Muted(string.Empty);
            _mutedBlocks.Add(_statusBarText);

            var content = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), Margin = new Avalonia.Thickness(18) };
            content.Children.Add(sections); Grid.SetRow(sections, 0);
            content.Children.Add(_statusBarText); Grid.SetRow(_statusBarText, 1);

            var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("240,*") };
            layout.Children.Add(_sidebarBorder); Grid.SetColumn(_sidebarBorder, 0);
            layout.Children.Add(content); Grid.SetColumn(content, 1);

            SetSection(_section);
            return layout;
        }

        private Button NavButton(string locKey, string section)
        {
            var btn = Ui.Btn(Loc.T(locKey), Ui.BtnKind.Ghost);
            btn.Tag = section;
            btn.HorizontalAlignment = HorizontalAlignment.Stretch;
            btn.HorizontalContentAlignment = HorizontalAlignment.Left;
            btn.Margin = new Avalonia.Thickness(0, 2);
            btn.Click += Nav_Click;
            _navButtons.Add(btn);
            return btn;
        }

        private Control BuildGameSection()
        {
            var panel = new StackPanel();

            // Uyarı kartı
            panel.Children.Add(Ui.Card(new TextBlock { Text = Loc.T("warn"), Foreground = Ui.Warn }));

            // Klasör kartı
            _txtGameDir = new TextBox { Watermark = Loc.T("tip_drop"), Text = _gameDir };
            var btnBrowse = Ui.Btn("📁", Ui.BtnKind.Secondary);
            btnBrowse.Click += BtnBrowse_Click;
            var folderGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,Auto") };
            folderGrid.Children.Add(_txtGameDir); Grid.SetColumn(_txtGameDir, 0);
            folderGrid.Children.Add(btnBrowse); Grid.SetColumn(btnBrowse, 2);
            var folderPanel = new StackPanel { Spacing = 8 };
            folderPanel.Children.Add(folderGrid);
            _cmbExes = new ComboBox { PlaceholderText = Loc.T("exe") };
            _cmbExes.SelectionChanged += CmbExes_SelectionChanged;
            folderPanel.Children.Add(_cmbExes);
            panel.Children.Add(Ui.Card(folderPanel));

            // Durum çipleri
            _chipPing = Ui.ChipText("Ping: -- ms");
            _chipEmu = Ui.ChipText(Loc.T("emu_missing"));
            _chipPatch = Ui.ChipText(Loc.T("patch_none"));
            _chipCompat = Ui.ChipText("Uyumluluk: --");
            var chips = new WrapPanel();
            chips.Children.Add(Ui.Chip(_chipPing));
            chips.Children.Add(Ui.Chip(_chipEmu));
            chips.Children.Add(Ui.Chip(_chipPatch));
            chips.Children.Add(Ui.Chip(_chipCompat));
            panel.Children.Add(chips);

            // Form kartı
            _txtPlayerName = new TextBox { Text = _settings.PlayerName };
            _txtAppId = new TextBox { Text = _settings.AppId };
            _txtAppId.TextChanged += (_, _) => _ = UpdateSystemStatusAsync();
            _txtFriendIp = new TextBox { Text = _settings.FriendIp, Watermark = Loc.T("tip_friendip") };
            _cmbLanguage = new ComboBox { SelectedIndex = 0 };
            foreach (string lang in new[] { "english", "turkish", "german" })
            {
                _cmbLanguage.Items.Add(new ComboBoxItem { Content = lang });
            }
            _chkUnlockDlc = new CheckBox { Content = Loc.T("dlc") };
            _chkCustomBroadcast = new CheckBox { Content = Loc.T("broadcast"), IsChecked = true };
            _chkFirewall = new CheckBox { Content = Loc.T("firewall"), IsChecked = true, IsVisible = App.Services.SupportsFirewall };

            var form = new StackPanel { Spacing = 8 };
            form.Children.Add(Ui.FieldRow(Loc.T("player"), _txtPlayerName));
            form.Children.Add(Ui.FieldRow("AppID:", _txtAppId));
            form.Children.Add(Ui.FieldRow(Loc.T("friendip"), _txtFriendIp));
            form.Children.Add(Ui.FieldRow(Loc.T("gamelang"), _cmbLanguage));
            form.Children.Add(_chkUnlockDlc);
            form.Children.Add(_chkCustomBroadcast);
            form.Children.Add(_chkFirewall);
            panel.Children.Add(Ui.Card(form));

            // Yardımcı butonlar
            var btnFindEmu = Ui.Btn(Loc.T("find_emu"), Ui.BtnKind.Secondary);
            btnFindEmu.Click += BtnFindEmu_Click;
            var btnAuto = Ui.Btn(Loc.T("autoscan"), Ui.BtnKind.Secondary);
            btnAuto.Click += BtnAutoSetup_Click;
            var btnPing = Ui.Btn(Loc.T("ping_btn"), Ui.BtnKind.Secondary);
            btnPing.Click += BtnPingTest_Click;
            var btnShare = Ui.Btn("🔗 Profili Paylaş", Ui.BtnKind.Secondary);
            btnShare.Click += BtnShareProfile_Click;
            var btnImport = Ui.Btn("📥 Profil Al", Ui.BtnKind.Secondary);
            btnImport.Click += BtnImportProfile_Click;
            panel.Children.Add(Ui.BtnRow(btnFindEmu, btnAuto, btnPing, btnShare, btnImport));
            panel.Children.Add(new Border { Height = 8 });

            // Ana aksiyonlar
            var btnApply = Ui.Btn("⚡ " + Loc.T("apply"), Ui.BtnKind.Primary);
            btnApply.Click += BtnApply_Click;
            var btnRestore = Ui.Btn("🔄 " + Loc.T("restore"), Ui.BtnKind.Secondary);
            btnRestore.Click += BtnRestore_Click;
            var btnLaunch = Ui.Btn("🚀 " + Loc.T("launch"), Ui.BtnKind.Secondary);
            btnLaunch.Click += BtnLaunch_Click;
            var btnVpn = Ui.Btn("⬇ " + Loc.T("dl_radmin"), Ui.BtnKind.Secondary);
            btnVpn.Click += BtnVpnDownload_Click;
            panel.Children.Add(Ui.BtnRow(btnApply, btnRestore, btnLaunch, btnVpn));

            return panel;
        }

        private Control BuildFriendsSection()
        {
            _txtFriendName = new TextBox { Watermark = Loc.T("fname") };
            _txtFriendIpNew = new TextBox { Watermark = Loc.T("fip") };
            var btnAdd = Ui.Btn(Loc.T("add"), Ui.BtnKind.Primary);
            btnAdd.Click += BtnAddFriend_Click;
            var addGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,12,*,12,Auto") };
            addGrid.Children.Add(_txtFriendName); Grid.SetColumn(_txtFriendName, 0);
            addGrid.Children.Add(_txtFriendIpNew); Grid.SetColumn(_txtFriendIpNew, 2);
            addGrid.Children.Add(btnAdd); Grid.SetColumn(btnAdd, 4);

            _lstFriends = new ListBox { MinHeight = 220 };
            _lstFriends.DoubleTapped += LstFriends_DoubleTapped;

            var btnRemove = Ui.Btn(Loc.T("remove"), Ui.BtnKind.Secondary);
            btnRemove.Click += BtnRemoveFriend_Click;

            var inner = new StackPanel { Spacing = 8 };
            inner.Children.Add(addGrid);
            inner.Children.Add(_lstFriends);
            inner.Children.Add(btnRemove);
            var card = Ui.Card(inner);
            var panel = new StackPanel();
            panel.Children.Add(Ui.Muted(Loc.T("friends")));
            ((TextBlock)panel.Children[0]).FontWeight = FontWeight.Bold;
            panel.Children.Add(card);
            return panel;
        }

        private Control BuildSettingsSection()
        {
            var cmbTheme = new ComboBox();
            foreach ((string tag, string key) in new[] { ("gece", "theme_gece"), ("gunduz", "theme_gunduz"), ("pembe", "theme_pembe") })
            {
                var item = new ComboBoxItem { Content = Loc.T(key), Tag = tag };
                cmbTheme.Items.Add(item);
                if (tag == _settings.Theme) cmbTheme.SelectedItem = item;
            }
            cmbTheme.SelectionChanged += CmbTheme_SelectionChanged;

            var cmbLang = new ComboBox();
            foreach ((string tag, string key) in new[] { ("tr", "lang_tr"), ("en", "lang_en"), ("de", "lang_de") })
            {
                var item = new ComboBoxItem { Content = Loc.T(key), Tag = tag };
                cmbLang.Items.Add(item);
                if (tag == _settings.UiLanguage) cmbLang.SelectedItem = item;
            }
            cmbLang.SelectionChanged += CmbUiLang_SelectionChanged;

            var chkMascot = new CheckBox { Content = Loc.T("mascot"), IsChecked = _settings.ShowMascot };
            chkMascot.IsCheckedChanged += ChkMascot_Changed;

            var chkTouch = new CheckBox { Content = "🖐 Dokunmatik mod (Steam Deck)", IsChecked = _settings.TouchMode };
            chkTouch.IsCheckedChanged += ChkTouch_Changed;

            var btnFullscreen = Ui.Btn(Loc.T("fullscreen"), Ui.BtnKind.Secondary);
            btnFullscreen.Click += (_, _) => ToggleFullscreen();

            var btnWizard = Ui.Btn("🧙 Kurulum sihirbazını tekrar göster", Ui.BtnKind.Secondary);
            btnWizard.Click += (_, _) =>
            {
                _wizardStep = 1;
                SetSection("wizard");
                RenderWizardStep();
            };

            var inner = new StackPanel { Spacing = 10 };
            inner.Children.Add(Ui.FieldRow(Loc.T("theme"), cmbTheme));
            inner.Children.Add(Ui.FieldRow(Loc.T("uilang"), cmbLang));
            inner.Children.Add(chkMascot);
            inner.Children.Add(chkTouch);
            inner.Children.Add(btnFullscreen);
            inner.Children.Add(btnWizard);

            var panel = new StackPanel();
            panel.Children.Add(Ui.Card(inner));
            return panel;
        }

        private Control BuildLogSection()
        {
            _logBox = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Text = _logBuffer,
                MinHeight = 400,
                VerticalAlignment = VerticalAlignment.Stretch,
                FontFamily = new FontFamily("monospace")
            };
            ScrollViewer.SetVerticalScrollBarVisibility(_logBox, ScrollBarVisibility.Auto);

            var btnCopy = Ui.Btn(Loc.T("copy"));
            btnCopy.Click += async (_, _) => await CopyTextAsync(_logBox.Text);
            var btnExport = Ui.Btn(Loc.T("export"));
            btnExport.Click += BtnExportLog_Click;
            var btnClear = Ui.Btn(Loc.T("clear"));
            btnClear.Click += (_, _) => { _logBuffer = string.Empty; _logBox.Text = string.Empty; };

            var inner = new StackPanel { Spacing = 8 };
            inner.Children.Add(_logBox);
            inner.Children.Add(Ui.BtnRow(btnCopy, btnExport, btnClear));

            var panel = new StackPanel();
            panel.Children.Add(Ui.Card(inner, 0));
            return panel;
        }

        // ===================== Bölüm / tema / maskot =====================

        private void Nav_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string section)
            {
                SetSection(section);
            }
        }

        // ===================== Kurulum sihirbazı (madde A) =====================

        private Control BuildWizardSection()
        {
            _lblWizardTitle = Ui.Title(string.Empty);

            _wizardBody = new StackPanel { Spacing = 10 };

            _btnWizBack = Ui.Btn("← Geri", Ui.BtnKind.Secondary);
            _btnWizBack.Click += (_, _) => { if (_wizardStep > 1) { _wizardStep--; RenderWizardStep(); } };
            _btnWizNext = Ui.Btn("İleri →", Ui.BtnKind.Primary);
            _btnWizNext.Click += (_, _) => { if (_wizardStep < 5) { _wizardStep++; RenderWizardStep(); } };
            _btnWizFinish = Ui.Btn("✅ Bitir — Panele geç", Ui.BtnKind.Primary);
            _btnWizFinish.Click += (_, _) => WizFinish();

            var inner = new StackPanel { Spacing = 12 };
            inner.Children.Add(_lblWizardTitle);
            inner.Children.Add(_wizardBody);
            inner.Children.Add(Ui.BtnRow(_btnWizBack, _btnWizNext, _btnWizFinish));

            var panel = new StackPanel();
            panel.Children.Add(Ui.Card(inner, 0));
            return panel;
        }

        private void RenderWizardStep()
        {
            if (_wizardBody == null || _lblWizardTitle == null) return;

            _wizardBody.Children.Clear();
            _lblWizardTitle.Text = $"Kurulum Sihirbazı — Adım {_wizardStep}/5";
            if (_btnWizBack != null) _btnWizBack.IsVisible = _wizardStep > 1;
            if (_btnWizNext != null) _btnWizNext.IsVisible = _wizardStep < 5;
            if (_btnWizFinish != null) _btnWizFinish.IsVisible = _wizardStep == 5;
            if (_btnWizNext != null) _btnWizNext.IsEnabled = _wizardStep != 1 || _wizardHasFolder;

            switch (_wizardStep)
            {
                case 1:
                    _wizardBody.Children.Add(Ui.Muted("Oynamak istediğin oyunun kurulu olduğu klasörü seç. Steam'de genelde:"));
                    _wizardBody.Children.Add(Ui.Muted(OperatingSystem.IsWindows()
                        ? @"C:\Program Files (x86)\Steam\steamapps\common\<OyunAdı>"
                        : "~/.local/share/Steam/steamapps/common/<OyunAdı>"));
                    var btnPick = Ui.Btn("📁 Oyun Klasörünü Seç", Ui.BtnKind.Primary);
                    btnPick.Click += BtnBrowse_Click;
                    _wizardBody.Children.Add(btnPick);

                    if (OperatingSystem.IsLinux())
                    {
                        var heroic = GameLibrary.FindHeroicGames(
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                        if (heroic.Count > 0)
                        {
                            _wizardBody.Children.Add(Ui.Muted("Heroic ile kurulu oyunların (tıkla ve seç):"));
                            foreach (HeroicGame g in heroic)
                            {
                                var gbtn = Ui.Btn($"🎮 {g.Title}", Ui.BtnKind.Secondary);
                                string path = g.InstallPath;
                                gbtn.Click += async (_, _) => await SelectGameFolderAsync(path);
                                _wizardBody.Children.Add(gbtn);
                            }
                        }
                    }

                    _wizardBody.Children.Add(Ui.Muted(string.IsNullOrWhiteSpace(_gameDir)
                        ? "Henüz klasör seçilmedi — seçince İleri açılır."
                        : $"✅ Seçildi: {_gameDir}"));
                    break;
                case 2:
                    _wizardBody.Children.Add(Ui.Muted("Oyunun Steam kütüphanesini taklit edecek bileşen dosyası lazım (senin sorumluluğunda temin edilir — program dağıtmaz)."));
                    _wizardBody.Children.Add(Ui.Muted("Dosyalar bilgisayarındaysa (İndirilenler, Masaüstü, Belgeler) aşağıdaki buton bulup kopyalar:"));
                    var btnFind = Ui.Btn("🔍 Yerel Bileşenleri Ara ve Kopyala", Ui.BtnKind.Secondary);
                    btnFind.Click += BtnFindEmu_Click;
                    _wizardBody.Children.Add(btnFind);
                    break;
                case 3:
                    _wizardBody.Children.Add(Ui.Muted("Arkadaşınla aynı ağda olman gerek. VPN istemcisi kurulu değilse önce indir:"));
                    var btnVpn = Ui.Btn("⬇ VPN İstemcisini İndir", Ui.BtnKind.Secondary);
                    btnVpn.Click += BtnVpnDownload_Click;
                    _wizardBody.Children.Add(btnVpn);
                    var btnAuto = Ui.Btn("⚡ Standart LAN Ayarlarını Uygula", Ui.BtnKind.Secondary);
                    btnAuto.Click += BtnAutoSetup_Click;
                    _wizardBody.Children.Add(btnAuto);
                    _wizardBody.Children.Add(Ui.Muted("İleri'ye bas — uygulama adımında görüşürüz."));
                    break;
                case 4:
                    _wizardBody.Children.Add(Ui.Muted("Ana ekrana döndüğünde ⚡ Uygula'ya bas. Program seni koruyor:"));
                    _wizardBody.Children.Add(Ui.Muted("• Yanlış klasör seçersen durur ve sorar"));
                    _wizardBody.Children.Add(Ui.Muted("• Oyun açıksa yamayı engeller"));
                    _wizardBody.Children.Add(Ui.Muted("• Her dosyanın yedeğini alır — bir sorun olursa kendini geri alır"));
                    _wizardBody.Children.Add(Ui.Muted("Uyguladıktan sonra renkli çiplere bak:"));
                    _wizardBody.Children.Add(Ui.Muted("🟢 Uyumluluk = oyun LAN'da çalışıyor · 🟢 Bileşen = hazır · 🟢 hedef = kaç dosya değişti"));
                    _wizardBody.Children.Add(Ui.Muted("🟠 'Yama sıfırlanmış' görürsen (Steam güncellediyse) Uygula'ya tekrar bas."));
                    var btnGo = Ui.Btn("🎮 Oyun paneline git", Ui.BtnKind.Primary);
                    btnGo.Click += (_, _) => SetSection("game");
                    _wizardBody.Children.Add(btnGo);
                    break;
                case 5:
                    _wizardBody.Children.Add(Ui.Muted("Şimdi arkadaşınla aynı ayarlarda buluş:"));
                    var btnShare = Ui.Btn("🔗 Profili Paylaş — kodu arkadaşına gönder", Ui.BtnKind.Secondary);
                    btnShare.Click += BtnShareProfile_Click;
                    _wizardBody.Children.Add(btnShare);
                    _wizardBody.Children.Add(Ui.Muted("Arkadaşın 'Profil Al' ile kodu yapıştırır, aynı ayarlarla Uygular."));
                    _wizardBody.Children.Add(Ui.Muted("VPN çipiniz ikisinizde de 🟢 yeşilse, oyunda lobiden veya IP ile buluşun."));
                    _wizardBody.Children.Add(Ui.Muted("Oynamayı bitirince 🔄 Geri Yükle ile oyununu eski haline döndür — ne yapılacağını önce gösterir."));
                    _wizardBody.Children.Add(Ui.Muted("Takıldığında sol menüden 🎓 Mavi'nin Okulu'na bak."));
                    break;
            }
        }

        // ===================== Mavi'nin Okulu =====================

        private static Control MaviQuote(string text)
        {
            return new Border
            {
                Background = Ui.Brush(Ui.C.Card),
                CornerRadius = new Avalonia.CornerRadius(8),
                Padding = new Avalonia.Thickness(10, 8),
                Margin = new Avalonia.Thickness(0, 6, 0, 0),
                Child = new TextBlock
                {
                    Text = $"🤖 Mavi der ki: {text}",
                    Foreground = Ui.Brush(Ui.C.Accent),
                    TextWrapping = TextWrapping.Wrap,
                    FontStyle = FontStyle.Italic,
                }
            };
        }

        private Control LearnCard(string title, params Control[] children)
        {
            var inner = new StackPanel { Spacing = 6 };
            var t = Ui.Title(title);
            t.FontSize = 15;
            inner.Children.Add(t);
            foreach (Control c in children) inner.Children.Add(c);
            return Ui.Card(inner);
        }

        private Control BuildLearnSection()
        {
            var panel = new StackPanel();

            panel.Children.Add(LearnCard("👋 Bu uygulama ne yapar?",
                Ui.Muted("BlueLobby, sahip olduğun oyunları arkadaşlarınla yerel ağ (LAN) veya VPN üzerinden oynayabilmen için hazırlar:"),
                Ui.Muted("• Oyunun ağ ayarlarını yapar ve dosyalarını güvenle yedekler"),
                Ui.Muted("• Arkadaşlarınla aynı ağda buluşmanı sağlar"),
                Ui.Muted("• İstediğinde her şeyi tek tıkla eski haline getirir"),
                MaviQuote("Ben Mavi! Bu ekranda senin rehberinim. Takıldığın her yerde yanındayım. 🤖")));

            var btnWiz = Ui.Btn("🧙 Kurulum sihirbazını başlat", Ui.BtnKind.Primary);
            btnWiz.Click += (_, _) =>
            {
                _wizardStep = 1;
                SetSection("wizard");
                RenderWizardStep();
            };
            panel.Children.Add(LearnCard("🧙 İlk kurulum (5 dakika)",
                Ui.Muted("1. Oyun klasörünü seç → 2. Bileşen dosyanı bul → 3. VPN'i hazırla → 4. Uygula → 5. Arkadaşınla oyna."),
                btnWiz));

            panel.Children.Add(LearnCard("🗺️ Ekranın yerleri",
                Ui.Muted("🎮 Oyun — klasör seçme, ayarlar, Uygula/Geri Yükle/Başlat butonları burada."),
                Ui.Muted("🎓 Mavi'nin Okulu — burası! Yardım ve rehber."),
                Ui.Muted("👥 Arkadaşlar — isim + IP kaydet; çift tıkla IP'yi forma doldurur."),
                Ui.Muted("⚙️ Ayarlar — tema, dil, maskot, dokunmatik mod, tam ekran."),
                Ui.Muted("📋 Log — yapılan her işlemin kaydı; sorun olursa buradan bakılır.")));

            panel.Children.Add(LearnCard("🚦 Renkli çipler ne anlatıyor?",
                Ui.Muted("VPN: 🟢 bağlı · 🔴 bağlı değil"),
                Ui.Muted("Ping: 🟢 iyi (<60ms) · 🟡 orta · 🔴 kötü/erişilemiyor"),
                Ui.Muted("Bileşen: 🟢 hazır · 🟡 eksik — 'Yerel Bileşenleri Ara'ya bas"),
                Ui.Muted("Uyumluluk: 🟢 çalışır · 🟡 ek adım gerekir · 🔴 çalışmaz · ⚪ bilinmiyor"),
                Ui.Muted("Hedef: 🟢 kaç dosya hazır · 🟠 yama sıfırlanmış (tekrar Uygula)"),
                MaviQuote("Çiplere her bakışında her şeyin yolunda olup olmadığını 2 saniyede görürsün.")));

            panel.Children.Add(LearnCard("🔧 Sorun mu var? İlk bakılacaklar",
                Ui.Muted("VPN kırmızı → VPN istemcisi açık mı? 'Standart LAN Ayarları'na bas."),
                Ui.Muted("Bileşen sarı → dosyalar İndirilenler'de mi? Ara butonuyla kopyala."),
                MutedFix(),
                Ui.Muted("Oyuna katılamıyorsun → arkadaşın IP'sini ping ile test et; 🟢 olmalı."),
                Ui.Muted("'Bu yöntemle çalışmaz' → oyun sunucu tabanlı; başka oyun dene."),
                Ui.Muted("Hiçbir şey çözülmezse Ayarlar → sorun giderme: Log'u dışa aktarıp paylaş.")));

            panel.Children.Add(LearnCard("🛡️ Güvenlik: verilerin emin ellerde",
                Ui.Muted("• Değiştirilen her dosyanın .bak yedeği alınır"),
                Ui.Muted("• Bir hata olursa yarım işlem kalmaz — otomatik geri alma devreye girer"),
                Ui.Muted("• Geri yüklemeden önce yedekler kontrol edilir, bozuksa durulur"),
                Ui.Muted("• Yanlış klasör seçilirse sorulur, emin olmadan devam edilmez"),
                MaviQuote("En kötü senaryoda bile oyununu ilk günkü hâline getirebilirsin. Söz veriyorum.")));

            panel.Children.Add(LearnCard("💡 Mavi'nin ipuçları",
                Ui.Muted("• Klasörü seçmek için pencereye sürükle-bırak da yapabilirsin"),
                Ui.Muted("• Arkadaş listesinde çift tık = IP otomatik forma gelir"),
                Ui.Muted("• 'Standart LAN Ayarları' tek tıkla çoğu ayarı senin yerine yapar"),
                Ui.Muted("• F11 = tam ekran"),
                Ui.Muted("• Her oyunun ayarını ayrı ayrı hatırlar — bir kez yaz, unut"),
                Ui.Muted("• 🔄 Geri Yükle her zaman güvenli: önce ne yapılacağını gösterir")));

            return panel;
        }

        private static Control MutedFix() =>
            Ui.Muted("Ping yok → arkadaşın IP'si doğru mu? (100.x Tailscale / 26.x Radmin)");

        private void WizFinish()
        {
            _settings.WizardDone = true;
            App.Store.SaveSettings(_settings);
            SetSection("game");
            Log("🧙 Sihirbaz tamamlandı. Şimdi '⚡ Uygula' ile oyununu hazırlayabilirsin.");
        }

        private void SetSection(string section)
        {
            _section = section;
            _pnlGame!.IsVisible = section == "game";
            _pnlFriends!.IsVisible = section == "friends";
            _pnlSettings!.IsVisible = section == "settings";
            _pnlLog!.IsVisible = section == "log";
            _pnlWizard!.IsVisible = section == "wizard";
            _pnlLearn!.IsVisible = section == "learn";

            // Seçili nav vurgusu
            foreach (Button btn in _navButtons)
            {
                bool selected = Equals(btn.Tag as string, section);
                btn.Background = selected ? Ui.Brush(Ui.C.Card) : Ui.Brush("transparent");
                btn.Foreground = selected ? Ui.Brush(Ui.C.Title) : Ui.Brush(Ui.C.Muted);
            }
        }

        private void ApplyTheme()
        {
            ThemeManager.Colors c = Ui.C;
            Background = Ui.Brush(c.Window);
            if (_sidebarBorder != null) _sidebarBorder.Background = Ui.Brush(c.Sidebar);
            if (_titleBlock != null) _titleBlock.Foreground = Ui.Brush(c.Title);
            if (_subtitleBlock != null) _subtitleBlock.Foreground = Ui.Brush(c.Muted);
            if (_statusBarText != null) _statusBarText.Foreground = Ui.Brush(c.Muted);
            if (_logBox != null)
            {
                _logBox.Background = Ui.Brush(c.Card);
                _logBox.Foreground = Ui.Brush(c.Log);
            }
            foreach (TextBlock block in _mutedBlocks)
            {
                block.Foreground = Ui.Brush(c.Muted);
            }
            SetSection(_section);
        }

        private void RebuildUi()
        {
            SaveUiToSettings();
            Content = BuildUi();
            ApplyTheme();
            RefreshFriendList();
            _ = UpdateSystemStatusAsync();
        }

        private void StartMascot()
        {
            _mascotTimer?.Stop();
            _mascotTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _mascotTimer.Tick += (_, _) =>
            {
                if (_mascotTransform == null || _mascotBlock == null || !_mascotBlock.IsVisible) return;
                _mascotTick++;
                _mascotTransform.Y = (_mascotTick % 2 == 0) ? 0 : -7;
            };
            _mascotTimer.Start();
        }
    }
}
