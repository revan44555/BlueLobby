
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using BlueLobby.Core;

namespace BlueLobby
{
    // Arayüz kurulumu — iş mantığı MainWindow.axaml.cs'dedir.
    // v6 "Meridian": sakin hiyerarşi, tek vurgu rengi, vektör ikonlar, az kart.
    public partial class MainWindow
    {
        // ---- Tema uygulanan referanslar ----
        private Border? _sidebarBorder;
        private WrapPanel? _compactNav;
        private TextBlock? _sidebarTargetText;
        private ColumnDefinitions? _mainColumns;
        private readonly List<Button> _compactNavButtons = new();
        private TextBlock? _titleBlock;
        private TextBlock? _subtitleBlock;
        private Border? _platformBlock;
        private TextBlock? _statusBarText;
        private TextBox? _logBox;
        private readonly List<TextBlock> _mutedBlocks = new();
        private readonly List<Button> _navButtons = new();
        private TextBlock? _mascotBlock;
        private Border? _topHeader;
        private TextBlock? _pageEyebrow;
        private TextBlock? _pageTitle;
        private TextBlock? _pageSubtitle;
        private Border? _statusBarBorder;
        private readonly List<Button> _allButtons = new();
        private readonly List<Border> _themedBorders = new();
        private TranslateTransform? _mascotTransform;
        private DispatcherTimer? _mascotTimer;

        // ---- Oyun bölümü ----
        private TextBox? _txtGameDir;
        private ComboBox? _cmbExes;
        private ListBox? _lstGames;
        private Button? _btnDiscoverGames;
        private TextBox? _txtPlayerName;
        private TextBox? _txtAppId;
        private TextBox? _txtFriendIp;
        private ComboBox? _cmbLanguage;
        private CheckBox? _chkUnlockDlc;
        private CheckBox? _chkCustomBroadcast;
        private CheckBox? _chkFirewall;
        private TextBlock? _chipVpn;
        private TextBlock? _sidebarVpnChip;
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
        private TextBlock? _lblWizardSub;
        private StackPanel? _wizardBody;
        private Button? _btnWizBack;
        private Button? _btnWizNext;
        private Button? _btnWizFinish;
        private readonly List<(Border Dot, TextBlock Label)> _wizardStepper = new();

        // ===================== UI kurulumu =====================

        private Control BuildUi()
        {
            _mutedBlocks.Clear();
            _navButtons.Clear();
            _compactNavButtons.Clear();
            _allButtons.Clear();
            _themedBorders.Clear();
            _wizardStepper.Clear();

            _titleBlock = Ui.Title(Loc.T("title"));
            _subtitleBlock = Ui.Muted(Loc.T("subtitle"));
            _subtitleBlock.FontSize = 10;

            _platformBlock = Ui.Badge(OperatingSystem.IsWindows() ? "WINDOWS" : "LINUX",
                Ui.Brush(Ui.C.Muted), Ui.Brush(Ui.C.Surface));

            _sidebarVpnChip = Ui.ChipText("VPN: --");

            // ---- Kenar çubuğu ----
            var sidebarPanel = new StackPanel { Margin = new Thickness(14, 20, 12, 14), Spacing = 2 };

            var brandRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            var brandMark = new Border
            {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(9),
                Background = Ui.Brush(Ui.C.SurfaceStrong),
                VerticalAlignment = VerticalAlignment.Center,
                Child = Ui.Owl(19, Ui.Brush(Ui.C.Accent)),
            };
            var brandText = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Spacing = 0 };
            _titleBlock.FontSize = 14.5;
            brandText.Children.Add(_titleBlock);
            brandText.Children.Add(_subtitleBlock);
            brandRow.Children.Add(brandMark);
            brandRow.Children.Add(brandText);

            var brandHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), Margin = new Thickness(4, 0, 0, 0) };
            brandHeader.Children.Add(brandRow);
            Grid.SetColumn(brandRow, 0);
            _platformBlock.VerticalAlignment = VerticalAlignment.Top;
            brandHeader.Children.Add(_platformBlock);
            Grid.SetColumn(_platformBlock, 1);

            sidebarPanel.Children.Add(brandHeader);
            sidebarPanel.Children.Add(new Border { Height = 14, Background = Brushes.Transparent });
            sidebarPanel.Children.Add(NavGroupLabel(Loc.T("nav_workspace")));
            sidebarPanel.Children.Add(NavButton("nav_game", "game", "game"));
            sidebarPanel.Children.Add(NavButton("nav_friends", "friends", "users"));
            sidebarPanel.Children.Add(NavButton("nav_learn", "learn", "learn"));
            sidebarPanel.Children.Add(NavGroupLabel(Loc.T("nav_tools")));
            sidebarPanel.Children.Add(NavButton("nav_settings", "settings", "settings"));
            sidebarPanel.Children.Add(NavButton("nav_log", "log", "log"));

            var sidebarFooter = new StackPanel { Margin = new Thickness(2, 18, 2, 0), Spacing = 8 };
            sidebarFooter.Children.Add(Ui.Divider());
            var statusRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            var statusDot = new Border
            {
                Width = 7,
                Height = 7,
                CornerRadius = new CornerRadius(4),
                Background = Ui.Ok,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var statusLabel = Ui.Muted(Loc.T("status_ready"));
            statusLabel.FontSize = 11;
            statusRow.Children.Add(statusDot);
            statusRow.Children.Add(statusLabel);
            var vpnChipHost = Ui.Chip(_sidebarVpnChip);
            vpnChipHost.Margin = new Thickness(0);
            statusRow.Children.Add(vpnChipHost);
            sidebarFooter.Children.Add(statusRow);
            sidebarFooter.Children.Add(new TextBlock
            {
                Text = Loc.T("active_target"),
                Foreground = Ui.Brush(Ui.C.Muted),
                FontSize = 9.5,
                FontWeight = FontWeight.SemiBold,
                Margin = new Thickness(0, 4, 0, -4),
            });
            _sidebarTargetText = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(_gameDir) ? Loc.T("game_target_empty") : _gameDir,
                Foreground = Ui.Brush(Ui.C.Text),
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 48,
                Opacity = 0.92,
            };
            sidebarFooter.Children.Add(_sidebarTargetText);
            if (_settings.ShowMascot)
            {
                var mascotRow = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 6,
                    Margin = new Thickness(0, 2, 0, 0),
                };
                mascotRow.Children.Add(Ui.Owl(11, Ui.Brush(Ui.C.Muted)));
                _mascotBlock = new TextBlock
                {
                    Text = "Mavi burada",
                    FontSize = 11.5,
                    Foreground = Ui.Brush(Ui.C.Muted),
                    RenderTransform = _mascotTransform = new TranslateTransform(),
                };
                mascotRow.Children.Add(_mascotBlock);
                sidebarFooter.Children.Add(mascotRow);
                _mascotBlock.Tapped += (_, _) => _ = new ConfirmDialog("Mavi", new[] { Loc.T("mascot_tip") }, Loc.T("yes"), string.Empty).ShowDialog<bool>(this);
            }

            var sidebarContent = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
                Children =
                {
                    new StackPanel { Children = { sidebarPanel } },
                    sidebarFooter,
                },
                Background = Ui.Brush(Ui.C.Sidebar),
            };
            _sidebarBorder = new Border
            {
                Child = sidebarContent,
                BorderBrush = Ui.Brush(Ui.C.Border),
                BorderThickness = new Thickness(0, 0, 1, 0),
            };
            _themedBorders.Add(_sidebarBorder);

            // ---- Bölüm panelleri ----
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

            // ---- Dar ekran alt gezinmesi ----
            _compactNav = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                IsVisible = false,
                Margin = new Thickness(0, 0, 0, 14),
            };
            foreach ((string key, string section, string icon) in new[]
            {
                ("nav_game", "game", "game"), ("nav_friends", "friends", "users"),
                ("nav_learn", "learn", "learn"), ("nav_settings", "settings", "settings"),
                ("nav_log", "log", "log"),
            })
            {
                var btn = NavButton(key, section, icon);
                btn.Margin = new Thickness(0, 0, 6, 6);
                _navButtons.Remove(btn);
                _compactNavButtons.Add(btn);
                _allButtons.Remove(btn);
                _compactNav.Children.Add(btn);
            }

            // ---- Sayfa başlığı ----
            _pageEyebrow = new TextBlock
            {
                Text = Loc.T("brand_eyebrow"),
                FontSize = 10,
                FontWeight = FontWeight.SemiBold,
                Foreground = Ui.Brush(Ui.C.Muted),
                Opacity = 0.9,
            };
            _pageTitle = Ui.PageTitle("Oyun");
            _pageSubtitle = Ui.Muted(Loc.T("page_game_subtitle"));
            _pageSubtitle.FontSize = 12.5;
            _pageSubtitle.Margin = new Thickness(0, 3, 0, 0);
            _mutedBlocks.Add(_pageSubtitle);

            var pageHeader = new StackPanel { Spacing = 0 };
            pageHeader.Children.Add(_pageEyebrow);
            pageHeader.Children.Add(_pageTitle);
            pageHeader.Children.Add(_pageSubtitle);
            _topHeader = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = Ui.Brush(Ui.C.BorderSubtle),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 0, 0, 14),
                Child = pageHeader,
                Margin = new Thickness(0, 0, 0, 16),
            };
            _themedBorders.Add(_topHeader);

            // ---- Durum çubuğu ----
            _statusBarText = Ui.Muted("");
            _statusBarText.FontSize = 11.5;
            _mutedBlocks.Add(_statusBarText);
            _statusBarBorder = new Border
            {
                Background = Ui.Brush(Ui.C.Surface),
                BorderBrush = Ui.Brush(Ui.C.Border),
                BorderThickness = new Thickness(1),
                CornerRadius = Ui.CardRadius,
                Padding = new Thickness(12, 8),
                Child = _statusBarText,
                Margin = new Thickness(0, 12, 0, 0),
            };
            _themedBorders.Add(_statusBarBorder);

            var sectionScroll = new ScrollViewer
            {
                Content = sections,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            };

            var contentGrid = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto,*,Auto") };
            contentGrid.Children.Add(_compactNav);
            Grid.SetRow(_compactNav, 0);
            contentGrid.Children.Add(_topHeader);
            Grid.SetRow(_topHeader, 1);
            contentGrid.Children.Add(sectionScroll);
            Grid.SetRow(sectionScroll, 2);
            contentGrid.Children.Add(_statusBarBorder);
            Grid.SetRow(_statusBarBorder, 3);

            var content = new Border
            {
                Background = Ui.Brush(Ui.C.Window),
                Padding = Ui.PagePadding,
                Child = contentGrid,
            };

            _mainColumns = new ColumnDefinitions("240,*");
            var layout = new Grid { ColumnDefinitions = _mainColumns };
            layout.Children.Add(_sidebarBorder);
            Grid.SetColumn(_sidebarBorder, 0);
            layout.Children.Add(content);
            Grid.SetColumn(content, 1);

            SetSection(_section);
            ApplyResponsiveLayout();
            return layout;
        }

        // ===================== Gezinti yardımcıları =====================

        private void UpdateSidebarTarget(string path)
        {
            if (_sidebarTargetText == null) return;
            _sidebarTargetText.Text = string.IsNullOrWhiteSpace(path)
                ? Loc.T("game_target_empty")
                : path;
        }

        private void StyleNavButtonState(Button button, bool selected)
        {
            button.Background = selected ? Ui.Brush(Ui.C.SurfaceStrong) : Brushes.Transparent;
            button.Foreground = selected ? Ui.Brush(Ui.C.Text) : Ui.Brush(Ui.C.Muted);
            button.BorderBrush = selected ? Ui.Brush(Ui.C.Accent) : Brushes.Transparent;
            button.FontWeight = selected ? FontWeight.SemiBold : FontWeight.Medium;
            if (button.Content is StackPanel stack && stack.Children.Count > 0 && stack.Children[0] is Path icon)
                icon.Stroke = selected ? Ui.Brush(Ui.C.Accent) : Ui.Brush(Ui.C.Muted);
        }

        private void ConfigureNavigationButton(Button button)
        {
            button.PointerEntered += (_, _) =>
            {
                button.Background = Ui.Brush(Ui.C.Hover);
                button.Foreground = Ui.Brush(Ui.C.Text);
            };
            button.PointerExited += (_, _) =>
            {
                bool selected = Equals(button.Tag as string, _section);
                StyleNavButtonState(button, selected);
            };
        }

        private TextBlock NavGroupLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                Foreground = Ui.Brush(Ui.C.Muted),
                FontSize = 9.5,
                FontWeight = FontWeight.SemiBold,
                Opacity = 0.75,
                Margin = new Avalonia.Thickness(12, 10, 0, 4),
            };
        }

        private Button NavButton(string locKey, string section, string icon)
        {
            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
            };
            content.Children.Add(Ui.Icon(icon, 15, Ui.Brush(Ui.C.Muted)));
            content.Children.Add(new TextBlock
            {
                Text = Loc.T(locKey),
                FontFamily = new FontFamily("Inter"),
                FontSize = 12.5,
                FontWeight = FontWeight.Medium,
                VerticalAlignment = VerticalAlignment.Center,
            });

            var btn = new Button
            {
                Content = content,
                Tag = section,
                Background = Brushes.Transparent,
                Foreground = Ui.Brush(Ui.C.Muted),
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(1),
                CornerRadius = Ui.ButtonRadius,
                Padding = new Thickness(12, 8),
                MinHeight = 38,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left,
            };
            ConfigureNavigationButton(btn);
            btn.Click += Nav_Click;
            _navButtons.Add(btn);
            _allButtons.Add(btn);
            return btn;
        }

        private void Nav_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string section)
                SetSection(section);
        }

        // ===================== Kart / alan yardımcıları =====================

        private Control BuildCardHeader(string title, string subtitle, string icon, Control? badge = null)
        {
            var header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            var left = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            var label = new StackPanel { Spacing = 2 };
            label.Children.Add(Ui.SectionTitle(title));
            if (!string.IsNullOrEmpty(subtitle))
                label.Children.Add(Ui.Muted(subtitle));
            left.Children.Add(Ui.Icon(icon, 16, Ui.Brush(Ui.C.Accent)));
            left.Children.Add(label);
            header.Children.Add(left);
            Grid.SetColumn(left, 0);
            if (badge != null)
            {
                badge.VerticalAlignment = VerticalAlignment.Center;
                header.Children.Add(badge);
                Grid.SetColumn(badge, 1);
            }
            return new Border { Child = header, Margin = new Thickness(0, 0, 0, 12) };
        }

        private Control BuildTwoColumn(Control left, Control right)
        {
            if (Ui.TouchMode)
            {
                left.Margin = new Thickness(0, 0, 0, 12);
                right.Margin = new Thickness(0);
                var single = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto") };
                single.Children.Add(left);
                Grid.SetRow(left, 0);
                single.Children.Add(right);
                Grid.SetRow(right, 1);
                return single;
            }
            left.Margin = new Thickness(0, 0, 6, 0);
            right.Margin = new Thickness(6, 0, 0, 0);
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
            grid.Children.Add(left);
            Grid.SetColumn(left, 0);
            grid.Children.Add(right);
            Grid.SetColumn(right, 1);
            return grid;
        }

        private static void StyleField(Control control)
        {
            Ui.StyleInput(control);
            switch (control)
            {
                case TextBox textBox:
                    textBox.CornerRadius = new CornerRadius(8);
                    break;
                case ComboBox comboBox:
                    comboBox.CornerRadius = new CornerRadius(8);
                    break;
                case ListBox listBox:
                    listBox.CornerRadius = new CornerRadius(8);
                    break;
            }
        }

        private Grid FieldRow(string label, Control field)
        {
            StyleField(field);
            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions($"{Ui.FieldLabelWidth},*"),
            };
            var lbl = new TextBlock
            {
                Text = label,
                Foreground = Ui.Brush(Ui.C.Muted),
                FontSize = 11.5,
                FontWeight = FontWeight.Medium,
                VerticalAlignment = VerticalAlignment.Center,
            };
            grid.Children.Add(lbl);
            Grid.SetColumn(lbl, 0);
            grid.Children.Add(field);
            Grid.SetColumn(field, 1);
            return grid;
        }

        // ===================== Oyun bölümü =====================

        private Control StatusCell(string label, TextBlock value, string detail)
        {
            var cell = new StackPanel { Spacing = 3 };
            cell.Children.Add(new TextBlock
            {
                Text = label.ToUpperInvariant(),
                Foreground = Ui.Brush(Ui.C.Muted),
                FontSize = 9.5,
                FontWeight = FontWeight.SemiBold,
            });
            cell.Children.Add(value);
            cell.Children.Add(new TextBlock
            {
                Text = detail,
                Foreground = Ui.Brush(Ui.C.Muted),
                FontSize = 10.5,
                TextWrapping = TextWrapping.Wrap,
            });
            return cell;
        }

        private Control VerticalRule()
        {
            return new Border
            {
                Width = 1,
                Background = Ui.Brush(Ui.C.BorderSubtle),
                Margin = new Thickness(10, 2, 10, 2),
                VerticalAlignment = VerticalAlignment.Stretch,
            };
        }

        private Control BuildGameSection()
        {
            var panel = new StackPanel { Spacing = 0 };

            // (a) Durum şeridi: tek kart, beş istatistik bloğu.
            // Not: her çip AYRI TextBlock örneğidir; Avalonia bir görsel öğenin iki
            // parent altında bulunmasına izin vermez (kenar çubuğu kendi VPN çipine sahip).
            _chipPatch = Ui.ChipText(Loc.T("patch_none"));
            _chipEmu = Ui.ChipText(Loc.T("emu_missing"));
            _chipCompat = Ui.ChipText("Uyumluluk: --");
            _chipPing = Ui.ChipText("Ping: -- ms");
            _chipVpn = Ui.ChipText("VPN: --");

            var statusRow = new StackPanel { Orientation = Orientation.Horizontal };
            statusRow.Children.Add(StatusCell(Loc.T("status_component"), _chipEmu, Loc.T("status_component_detail")));
            statusRow.Children.Add(VerticalRule());
            statusRow.Children.Add(StatusCell(Loc.T("status_compat"), _chipCompat, Loc.T("status_compat_detail")));
            statusRow.Children.Add(VerticalRule());
            statusRow.Children.Add(StatusCell(Loc.T("status_patch"), _chipPatch, Loc.T("status_patch_detail")));
            statusRow.Children.Add(VerticalRule());
            statusRow.Children.Add(StatusCell(Loc.T("status_ping"), _chipPing, Loc.T("status_ping_detail")));
            statusRow.Children.Add(VerticalRule());
            statusRow.Children.Add(StatusCell(Loc.T("status_vpn"), _chipVpn, Loc.T("status_vpn_detail")));
            panel.Children.Add(Ui.Card(statusRow));

            // (b) Kütüphane + hedef kartları
            _lstGames = new ListBox
            {
                MinHeight = 84,
                MaxHeight = 190,
                BorderThickness = new Thickness(0),
            };
            StyleField(_lstGames);
            _lstGames.SelectionChanged += DiscoveredGame_SelectionChanged;

            _btnDiscoverGames = Ui.BtnIcon(Loc.T("discover_games"), "search", Ui.BtnKind.Secondary);
            _btnDiscoverGames.Click += BtnDiscoverGames_Click;

            var libraryCard = new StackPanel { Spacing = 0 };
            libraryCard.Children.Add(BuildCardHeader(Loc.T("section_installed_games"), Loc.T("discover_subtitle"), "search", _btnDiscoverGames));
            libraryCard.Children.Add(_lstGames);

            _txtGameDir = new TextBox
            {
                Watermark = Loc.T("tip_drop"),
                Text = _gameDir,
                MinHeight = Ui.ButtonMinHeight,
            };
            StyleField(_txtGameDir);
            var btnBrowse = Ui.BtnIcon(Loc.T("gamefolder"), "folder", Ui.BtnKind.Secondary);
            btnBrowse.Click += BtnBrowse_Click;

            var folderRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,Auto") };
            folderRow.Children.Add(_txtGameDir);
            Grid.SetColumn(_txtGameDir, 0);
            folderRow.Children.Add(btnBrowse);
            Grid.SetColumn(btnBrowse, 2);

            _cmbExes = new ComboBox
            {
                PlaceholderText = Loc.T("exe"),
                MinHeight = Ui.ButtonMinHeight,
                Margin = new Thickness(0, 10, 0, 0),
            };
            StyleField(_cmbExes);
            _cmbExes.SelectionChanged += CmbExes_SelectionChanged;

            var targetCard = new StackPanel { Spacing = 0 };
            var platformBadge = Ui.Badge(OperatingSystem.IsWindows() ? "WINDOWS x64" : "LINUX x64");
            targetCard.Children.Add(BuildCardHeader(Loc.T("section_game_setup"), Loc.T("game_manage_subtitle"), "folder", platformBadge));
            targetCard.Children.Add(folderRow);
            targetCard.Children.Add(_cmbExes);

            panel.Children.Add(BuildTwoColumn(libraryCard, targetCard));

            // (c) Oturum kartı
            _txtPlayerName = new TextBox { Text = _settings.PlayerName, MinHeight = Ui.ButtonMinHeight };
            _txtAppId = new TextBox { Text = _settings.AppId, MinHeight = Ui.ButtonMinHeight };
            _txtAppId.TextChanged += (_, _) => _ = UpdateSystemStatusAsync();
            _txtFriendIp = new TextBox
            {
                Text = _settings.FriendIp,
                Watermark = Loc.T("tip_friendip"),
                MinHeight = Ui.ButtonMinHeight,
            };
            _cmbLanguage = new ComboBox { SelectedIndex = 0, MinHeight = Ui.ButtonMinHeight };
            foreach (string lang in new[] { "english", "turkish", "german" })
                _cmbLanguage.Items.Add(new ComboBoxItem { Content = lang });

            var sessionLeft = new StackPanel { Spacing = 8 };
            sessionLeft.Children.Add(FieldRow(Loc.T("player"), _txtPlayerName));
            sessionLeft.Children.Add(FieldRow("AppID", _txtAppId));
            var sessionRight = new StackPanel { Spacing = 8 };
            sessionRight.Children.Add(FieldRow(Loc.T("friendip"), _txtFriendIp));
            sessionRight.Children.Add(FieldRow(Loc.T("gamelang"), _cmbLanguage));

            _chkUnlockDlc = new CheckBox { Content = Loc.T("dlc") };
            _chkCustomBroadcast = new CheckBox { Content = Loc.T("broadcast"), IsChecked = true };
            _chkFirewall = new CheckBox
            {
                Content = Loc.T("firewall"),
                IsChecked = true,
                IsVisible = App.Services.SupportsFirewall,
            };
            Ui.StyleInput(_chkUnlockDlc);
            Ui.StyleInput(_chkCustomBroadcast);
            Ui.StyleInput(_chkFirewall);

            var advancedBody = new StackPanel { Spacing = 8 };
            advancedBody.Children.Add(Ui.Muted(Loc.T("advanced_subtitle")));
            advancedBody.Children.Add(_chkUnlockDlc);
            advancedBody.Children.Add(_chkCustomBroadcast);
            advancedBody.Children.Add(_chkFirewall);
            var advanced = new Expander
            {
                Header = Loc.T("advanced_options"),
                IsExpanded = false,
                Content = Ui.Card(advancedBody, 0),
                Margin = new Thickness(0, 12, 0, 0),
                Foreground = Ui.Brush(Ui.C.Text),
            };

            var sessionInner = new StackPanel { Spacing = 0 };
            sessionInner.Children.Add(BuildCardHeader(Loc.T("section_session"), Loc.T("session_subtitle"), "play"));
            var sessionColumns = BuildTwoColumn(sessionLeft, sessionRight);
            sessionColumns.Margin = new Thickness(0);
            sessionInner.Children.Add(sessionColumns);
            sessionInner.Children.Add(advanced);

            // (d) Aksiyon çubuğu
            var btnApply = Ui.BtnIcon(Loc.T("apply"), "zap", Ui.BtnKind.Primary);
            btnApply.MinWidth = 170;
            btnApply.Click += BtnApply_Click;
            var btnLaunch = Ui.BtnIcon(Loc.T("launch"), "play", Ui.BtnKind.Secondary);
            btnLaunch.MinWidth = 150;
            btnLaunch.Click += BtnLaunch_Click;
            var btnRestore = Ui.BtnIcon(Loc.T("restore"), "restore", Ui.BtnKind.Secondary);
            btnRestore.MinWidth = 130;
            btnRestore.Click += BtnRestore_Click;

            var actionRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            var actionLeft = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            actionLeft.Children.Add(btnRestore);
            actionLeft.Children.Add(btnLaunch);
            actionRow.Children.Add(actionLeft);
            Grid.SetColumn(actionLeft, 0);
            actionRow.Children.Add(btnApply);
            Grid.SetColumn(btnApply, 1);

            var actionInner = new StackPanel { Spacing = 0 };
            actionInner.Children.Add(BuildCardHeader(Loc.T("section_main_actions"), Loc.T("actions_subtitle"), "zap"));
            actionInner.Children.Add(actionRow);
            panel.Children.Add(Ui.Card(actionInner));

            // (e) Uyarı notu
            var warnRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 4, 12),
            };
            warnRow.Children.Add(Ui.Icon("shield", 13, Ui.Warn));
            warnRow.Children.Add(Ui.Muted(Loc.T("warn")));
            panel.Children.Add(warnRow);

            // (f) Araçlar
            var btnFindEmu = Ui.BtnIcon(Loc.T("find_emu"), "search", Ui.BtnKind.Secondary);
            btnFindEmu.Click += BtnFindEmu_Click;
            var btnAuto = Ui.BtnIcon(Loc.T("autoscan"), "settings", Ui.BtnKind.Secondary);
            btnAuto.Click += BtnAutoSetup_Click;
            var btnPing = Ui.BtnIcon(Loc.T("ping_btn"), "zap", Ui.BtnKind.Secondary);
            btnPing.Click += BtnPingTest_Click;
            var btnShare = Ui.BtnIcon(Loc.T("share_profile"), "users", Ui.BtnKind.Secondary);
            btnShare.Click += BtnShareProfile_Click;
            var btnImport = Ui.BtnIcon(Loc.T("import_profile"), "download", Ui.BtnKind.Secondary);
            btnImport.Click += BtnImportProfile_Click;
            var btnVpn = Ui.BtnIcon(Loc.T("dl_radmin"), "download", Ui.BtnKind.Secondary);
            btnVpn.Click += BtnVpnDownload_Click;

            var toolBody = new StackPanel { Spacing = 8 };
            toolBody.Children.Add(Ui.Muted(Loc.T("tools_subtitle")));
            toolBody.Children.Add(Ui.BtnRow(btnFindEmu, btnAuto, btnPing, btnShare, btnImport, btnVpn));
            var toolExpander = new Expander
            {
                Header = Loc.T("tools"),
                IsExpanded = false,
                Content = Ui.Card(toolBody, 0),
                Margin = new Thickness(0, 0, 0, 12),
                Foreground = Ui.Brush(Ui.C.Text),
            };
            panel.Children.Add(toolExpander);

            return panel;
        }

        // ===================== Arkadaşlar bölümü =====================

        private Control BuildFriendsSection()
        {
            _txtFriendName = new TextBox { Watermark = Loc.T("fname"), MinHeight = Ui.ButtonMinHeight };
            StyleField(_txtFriendName);
            _txtFriendIpNew = new TextBox { Watermark = Loc.T("fip"), MinHeight = Ui.ButtonMinHeight };
            StyleField(_txtFriendIpNew);

            var btnAdd = Ui.BtnIcon(Loc.T("add"), "users", Ui.BtnKind.Primary);
            btnAdd.Click += BtnAddFriend_Click;

            var formRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,10,*,12,Auto") };
            formRow.Children.Add(_txtFriendName);
            Grid.SetColumn(_txtFriendName, 0);
            formRow.Children.Add(_txtFriendIpNew);
            Grid.SetColumn(_txtFriendIpNew, 2);
            formRow.Children.Add(btnAdd);
            Grid.SetColumn(btnAdd, 4);
            btnAdd.VerticalAlignment = VerticalAlignment.Center;

            _lstFriends = new ListBox
            {
                MinHeight = 300,
                MaxHeight = 420,
                BorderThickness = new Thickness(0),
            };
            StyleField(_lstFriends);
            _lstFriends.DoubleTapped += LstFriends_DoubleTapped;

            var btnUse = Ui.BtnIcon(Loc.T("use"), "restore", Ui.BtnKind.Secondary);
            btnUse.Click += (_, _) =>
            {
                if (_lstFriends?.SelectedItem is not FriendEntry selected || _txtFriendIp == null)
                    return;
                _txtFriendIp.Text = selected.Ip;
                SetSection("game");
                Log($"{Loc.T("friend_used")}: {selected.Ip}");
            };

            var btnRemove = Ui.BtnIcon(Loc.T("remove"), "log", Ui.BtnKind.Danger);
            btnRemove.Click += BtnRemoveFriend_Click;

            var listHeader = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto") };
            var listTitle = Ui.SectionTitle("Kayıtlı oyuncular");
            listTitle.VerticalAlignment = VerticalAlignment.Center;
            listHeader.Children.Add(listTitle);
            Grid.SetColumn(listTitle, 0);
            listHeader.Children.Add(btnUse);
            Grid.SetColumn(btnUse, 1);
            listHeader.Children.Add(btnRemove);
            Grid.SetColumn(btnRemove, 2);

            var inner = new StackPanel { Spacing = 10 };
            inner.Children.Add(BuildCardHeader(
                Loc.T("friends"),
                "İsim ve IP adresini kaydedin; çift tıklama hızlı kullanım için desteklenir.",
                "users"));
            inner.Children.Add(formRow);
            inner.Children.Add(Ui.Divider());
            inner.Children.Add(listHeader);
            inner.Children.Add(Ui.Muted("Seçilen oyuncunun IP'si Oyun sekmesindeki Arkadaş IP alanına aktarılır."));
            inner.Children.Add(_lstFriends);

            return Ui.Card(inner, 0);
        }

        // ===================== Ayarlar bölümü =====================

        private Control BuildSettingsSection()
        {
            var cmbTheme = new ComboBox { MinHeight = Ui.ButtonMinHeight };
            foreach ((string tag, string key) in new[] { ("gece", "theme_gece"), ("gunduz", "theme_gunduz"), ("pembe", "theme_pembe") })
            {
                var item = new ComboBoxItem { Content = Loc.T(key), Tag = tag };
                cmbTheme.Items.Add(item);
                if (tag == _settings.Theme) cmbTheme.SelectedItem = item;
            }
            cmbTheme.SelectionChanged += CmbTheme_SelectionChanged;

            var cmbLang = new ComboBox { MinHeight = Ui.ButtonMinHeight };
            foreach ((string tag, string key) in new[] { ("tr", "lang_tr"), ("en", "lang_en"), ("de", "lang_de") })
            {
                var item = new ComboBoxItem { Content = Loc.T(key), Tag = tag };
                cmbLang.Items.Add(item);
                if (tag == _settings.UiLanguage) cmbLang.SelectedItem = item;
            }
            cmbLang.SelectionChanged += CmbUiLang_SelectionChanged;

            var chkMascot = new CheckBox { Content = Loc.T("mascot"), IsChecked = _settings.ShowMascot };
            chkMascot.IsCheckedChanged += ChkMascot_Changed;
            var chkTouch = new CheckBox { Content = Loc.T("touch_mode"), IsChecked = _settings.TouchMode };
            chkTouch.IsCheckedChanged += ChkTouch_Changed;

            var btnFullscreen = Ui.Btn(Loc.T("fullscreen"), Ui.BtnKind.Secondary);
            btnFullscreen.Click += (_, _) => ToggleFullscreen();
            var btnWizard = Ui.BtnIcon(Loc.T("setup_wizard_again"), "learn", Ui.BtnKind.Secondary);
            btnWizard.Click += (_, _) => { _wizardStep = 1; SetSection("wizard"); RenderWizardStep(); };

            var appearance = new StackPanel { Spacing = 4 };
            appearance.Children.Add(BuildCardHeader("Görünüm", "Tema ve dil değişiklikleri anında uygulanır.", "owl"));
            appearance.Children.Add(FieldRow(Loc.T("theme"), cmbTheme));
            appearance.Children.Add(FieldRow(Loc.T("uilang"), cmbLang));

            var behavior = new StackPanel { Spacing = 10 };
            behavior.Children.Add(BuildCardHeader("Davranış", "Ekranın kullanım biçimini ve yardımcı öğeleri belirle.", "settings"));
            behavior.Children.Add(chkTouch);
            behavior.Children.Add(chkMascot);
            behavior.Children.Add(Ui.BtnRow(btnFullscreen));

            var setup = new StackPanel { Spacing = 8 };
            setup.Children.Add(BuildCardHeader("Kurulum", "İlk kurulum sihirbazını yeniden açabilirsin; normal Oyun ekranına dönersin.", "restore"));
            setup.Children.Add(Ui.BtnRow(btnWizard));

            var appearanceCard = Ui.Card(appearance);
            var behaviorCard = Ui.Card(behavior);

            var panel = new StackPanel { Spacing = 0 };
            panel.Children.Add(BuildTwoColumn(appearanceCard, behaviorCard));
            panel.Children.Add(Ui.Card(setup, 0));
            return panel;
        }

        // ===================== Log bölümü =====================

        private Control BuildLogSection()
        {
            _logBox = new TextBox
            {
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                Text = _logBuffer,
                MinHeight = 460,
                VerticalAlignment = VerticalAlignment.Stretch,
                FontFamily = new FontFamily("Consolas, monospace"),
                Foreground = Ui.Brush(Ui.C.Log),
                Background = Ui.Brush(Ui.C.Input),
                BorderThickness = new Thickness(0),
                Padding = new Thickness(6),
            };
            StyleField(_logBox);
            _logBox.Foreground = Ui.Brush(Ui.C.Log);
            _logBox.FontFamily = new FontFamily("Consolas, monospace");
            ScrollViewer.SetVerticalScrollBarVisibility(_logBox, ScrollBarVisibility.Auto);

            var btnCopy = Ui.Btn(Loc.T("copy"));
            btnCopy.Click += async (_, _) => await CopyTextAsync(_logBox.Text);
            var btnExport = Ui.Btn(Loc.T("export"));
            btnExport.Click += BtnExportLog_Click;
            var btnClear = Ui.Btn(Loc.T("clear"), Ui.BtnKind.Danger);
            btnClear.Click += async (_, _) =>
            {
                var confirm = new ConfirmDialog(
                    Loc.T("log_clear_q"),
                    new[] { Loc.T("log_clear_detail") },
                    Loc.T("yes"),
                    Loc.T("no"));
                if (!await confirm.ShowDialog<bool>(this)) return;
                _logBuffer = string.Empty;
                _logBox.Text = string.Empty;
            };

            var inner = new StackPanel { Spacing = 10 };
            inner.Children.Add(BuildCardHeader(Loc.T("section_activity"), Loc.T("activity_subtitle"), "log"));
            inner.Children.Add(_logBox);
            inner.Children.Add(Ui.BtnRow(btnCopy, btnExport, btnClear));

            return Ui.Card(inner, 0);
        }

        // ===================== Kurulum sihirbazı =====================

        private Control BuildWizardSection()
        {
            _lblWizardTitle = Ui.Title(string.Empty);
            _lblWizardSub = Ui.Muted(string.Empty);

            _wizardBody = new StackPanel { Spacing = 10 };

            _btnWizBack = Ui.Btn("← Geri", Ui.BtnKind.Secondary);
            _btnWizBack.Click += (_, _) => { if (_wizardStep > 1) { _wizardStep--; RenderWizardStep(); } };
            _btnWizNext = Ui.Btn("İleri →", Ui.BtnKind.Primary);
            _btnWizNext.Click += (_, _) => { if (_wizardStep < 5) { _wizardStep++; RenderWizardStep(); } };
            _btnWizFinish = Ui.Btn("Bitir — Panele geç", Ui.BtnKind.Primary);
            _btnWizFinish.Click += (_, _) => WizFinish();

            var navRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
            navRow.Children.Add(_btnWizBack);
            Grid.SetColumn(_btnWizBack, 0);
            var navRight = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
            navRight.Children.Add(_btnWizNext);
            navRight.Children.Add(_btnWizFinish);
            navRow.Children.Add(navRight);
            Grid.SetColumn(navRight, 1);

            var inner = new StackPanel { Spacing = 14 };
            inner.Children.Add(_lblWizardTitle);
            inner.Children.Add(_lblWizardSub);
            inner.Children.Add(BuildWizardStepper());
            inner.Children.Add(Ui.Divider());
            inner.Children.Add(_wizardBody);
            inner.Children.Add(navRow);

            return Ui.Card(inner, 0);
        }

        private static string WizardStepName(int step) => step switch
        {
            1 => "Klasör",
            2 => "Bileşen",
            3 => "Ağ",
            4 => "Uygulama",
            _ => "Oyna",
        };

        private StackPanel BuildWizardStepper()
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0, Margin = new Thickness(0, 4, 0, 4) };
            for (int i = 0; i < 5; i++)
            {
                var num = new TextBlock
                {
                    Text = (i + 1).ToString(),
                    FontSize = 11.5,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Ui.Brush(Ui.C.Muted),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                var dot = new Border
                {
                    Width = 24,
                    Height = 24,
                    CornerRadius = new CornerRadius(12),
                    Background = Ui.Brush(Ui.C.SurfaceStrong),
                    BorderBrush = Ui.Brush(Ui.C.BorderSubtle),
                    BorderThickness = new Thickness(1),
                    Child = num,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                var label = new TextBlock
                {
                    Text = WizardStepName(i + 1),
                    FontSize = 11,
                    Foreground = Ui.Brush(Ui.C.Muted),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(7, 0, 0, 0),
                };
                var step = new StackPanel { Orientation = Orientation.Horizontal };
                step.Children.Add(dot);
                step.Children.Add(label);
                row.Children.Add(step);
                if (i < 4)
                {
                    row.Children.Add(new Border
                    {
                        Width = 22,
                        Height = 1,
                        Background = Ui.Brush(Ui.C.BorderSubtle),
                        VerticalAlignment = VerticalAlignment.Center,
                        Margin = new Thickness(9, 0, 9, 0),
                    });
                }
                _wizardStepper.Add((dot, label));
            }
            return row;
        }

        private void UpdateWizardStepper()
        {
            for (int i = 0; i < _wizardStepper.Count; i++)
            {
                (Border dot, TextBlock label) = _wizardStepper[i];
                bool done = i + 1 < _wizardStep;
                bool active = i + 1 == _wizardStep;
                dot.Background = done ? Ui.Ok : active ? Ui.Brush(Ui.C.Accent) : Ui.Brush(Ui.C.SurfaceStrong);
                dot.BorderBrush = done ? Ui.Ok : active ? Ui.Brush(Ui.C.Accent) : Ui.Brush(Ui.C.BorderSubtle);
                if (dot.Child is TextBlock num)
                {
                    num.Text = done ? "\u2713" : (i + 1).ToString();
                    num.Foreground = done || active ? Brushes.White : Ui.Brush(Ui.C.Muted);
                }
                label.Foreground = done || active ? Ui.Brush(Ui.C.Text) : Ui.Brush(Ui.C.Muted);
                label.FontWeight = active ? FontWeight.SemiBold : FontWeight.Medium;
            }
        }

        private void RenderWizardStep()
        {
            if (_wizardBody == null || _lblWizardTitle == null) return;
            UpdateWizardStepper();

            _wizardBody.Children.Clear();
            _lblWizardTitle.Text = $"Kurulum Sihirbazı — Adım {_wizardStep}/5";
            if (_lblWizardSub != null)
            {
                _lblWizardSub.Text = _wizardStep switch
                {
                    1 => "Oyunun kurulu olduğu klasörü seçin.",
                    2 => "Bileşen dosyasını bilgisayarınızda bulun.",
                    3 => "VPN istemcisini hazırlayın ve ağ ayarlarını uygulayın.",
                    4 => "Uygula her şeyi tek tıkla güvenle hazırlar.",
                    _ => "Profilini paylaşın ve arkadaşınla oynamaya başlayın.",
                };
            }
            if (_btnWizBack != null) _btnWizBack.IsVisible = _wizardStep > 1;
            if (_btnWizNext != null) _btnWizNext.IsVisible = _wizardStep < 5;
            if (_btnWizFinish != null) _btnWizFinish.IsVisible = _wizardStep == 5;
            if (_btnWizNext != null) _btnWizNext.IsEnabled = _wizardStep != 1 || _wizardHasFolder;

            switch (_wizardStep)
            {
                case 1:
                    _wizardBody.Children.Add(Ui.Muted("Oynamak istediğin oyunun kurulu olduğu klasörü seçin. Steam'de varsayılan yol genellikle:"));
                    _wizardBody.Children.Add(Ui.Muted(OperatingSystem.IsWindows()
                        ? @"C:\Program Files (x86)\Steam\steamapps\common\<OyunAdı>"
                        : "~/.local/share/Steam/steamapps/common/<OyunAdı>"));
                    var btnPick = Ui.BtnIcon("Oyun Klasörünü Seç", "folder", Ui.BtnKind.Primary);
                    btnPick.Click += BtnBrowse_Click;
                    _wizardBody.Children.Add(btnPick);

                    if (OperatingSystem.IsLinux())
                    {
                        var heroic = GameLibrary.FindHeroicGames(
                            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                        if (heroic.Count > 0)
                        {
                            _wizardBody.Children.Add(Ui.Muted("Heroic ile kurulu oyunlar (tıkla ve seç):"));
                            foreach (HeroicGame g in heroic)
                            {
                                var gbtn = Ui.Btn(g.Title, Ui.BtnKind.Secondary);
                                string path = g.InstallPath;
                                gbtn.Click += async (_, _) => await SelectGameFolderAsync(path);
                                _wizardBody.Children.Add(gbtn);
                            }
                        }
                    }

                    _wizardBody.Children.Add(Ui.Muted(string.IsNullOrWhiteSpace(_gameDir)
                        ? "Henüz klasör seçilmedi — seçince İleri açılır."
                        : $"Seçildi: {_gameDir}"));
                    break;
                case 2:
                    _wizardBody.Children.Add(Ui.Muted("Oyunun Steam kütüphanesini taklit edecek bileşen dosyası gerekiyor (senin sorumluluğunda temin edilir — program dağıtmaz)."));
                    _wizardBody.Children.Add(Ui.Muted("Dosyalar bilgisayarındaysa (İndirilenler, Masaüstü, Belgeler) aşağıdaki buton bulup kopyalar:"));
                    var btnFind = Ui.BtnIcon("Yerel Bileşenleri Ara ve Kopyala", "search", Ui.BtnKind.Secondary);
                    btnFind.Click += BtnFindEmu_Click;
                    _wizardBody.Children.Add(btnFind);
                    break;
                case 3:
                    _wizardBody.Children.Add(Ui.Muted("Arkadaşınla aynı ağda olman gerekiyor. VPN istemcisi kurulu değilse önce indirin:"));
                    var btnVpn = Ui.BtnIcon("VPN İstemcisini İndir", "download", Ui.BtnKind.Secondary);
                    btnVpn.Click += BtnVpnDownload_Click;
                    _wizardBody.Children.Add(btnVpn);
                    var btnAuto = Ui.BtnIcon("Standart LAN Ayarlarını Uygula", "zap", Ui.BtnKind.Secondary);
                    btnAuto.Click += BtnAutoSetup_Click;
                    _wizardBody.Children.Add(btnAuto);
                    _wizardBody.Children.Add(Ui.Muted("İleri'ye bas — uygulama adımında görüşürüz."));
                    break;
                case 4:
                    _wizardBody.Children.Add(Ui.Muted("Ana ekrana döndüğünde Uygula'ya bas. Program seni koruyor:"));
                    _wizardBody.Children.Add(Ui.Muted("– Yanlış klasör seçersen durur ve sorar"));
                    _wizardBody.Children.Add(Ui.Muted("– Oyun açıksa yamayı engeller"));
                    _wizardBody.Children.Add(Ui.Muted("– Her dosyanın yedeğini alır — bir sorun olursa kendini geri alır"));
                    _wizardBody.Children.Add(Ui.Muted("Uyguladıktan sonra durum şeritindeki renklere bak:"));
                    _wizardBody.Children.Add(Ui.Muted("Yeşil = oyun LAN'da çalışıyor · Bileşen hazır · hedef = kaç dosya değişti"));
                    _wizardBody.Children.Add(Ui.Muted("Turuncu 'yama sıfırlanmış' görürsen (Steam güncellediyse) Uygula'ya tekrar bas."));
                    var btnGo = Ui.BtnIcon("Oyun paneline git", "game", Ui.BtnKind.Primary);
                    btnGo.Click += (_, _) => SetSection("game");
                    _wizardBody.Children.Add(btnGo);
                    break;
                case 5:
                    _wizardBody.Children.Add(Ui.Muted("Son adım: arkadaşınla aynı ayarlarda buluş."));
                    var btnShare = Ui.BtnIcon("Profili Paylaş — kodu arkadaşına gönder", "users", Ui.BtnKind.Secondary);
                    btnShare.Click += BtnShareProfile_Click;
                    _wizardBody.Children.Add(btnShare);
                    _wizardBody.Children.Add(Ui.Muted("Arkadaşın 'Profil Al' ile kodu yapıştırır, aynı ayarlarla Uygular."));
                    _wizardBody.Children.Add(Ui.Muted("VPN çipiniz ikisi de yeşilse, oyunda lobiden veya IP ile buluşun."));
                    _wizardBody.Children.Add(Ui.Muted("Oynamayı bitirince Geri Yükle ile oyununu eski haline döndür — ne yapılacağını önce gösterir."));
                    _wizardBody.Children.Add(Ui.Muted("Takıldığında sol menüden Mavi'nin Okulu'na bak."));
                    break;
            }
        }

        private void WizFinish()
        {
            _settings.WizardDone = true;
            App.Store.SaveSettings(_settings);
            SetSection("game");
            Log("Sihirbaz tamamlandı. Şimdi 'Uygula' ile oyununu hazırlayabilirsin.");
        }

        // ===================== Mavi'nin Okulu =====================

        private static Control MaviQuote(string text)
        {
            return new Border
            {
                Background = Ui.Brush(Ui.C.Surface),
                BorderBrush = Ui.Brush(Ui.C.BorderSubtle),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 9),
                Margin = new Thickness(0, 6, 0, 0),
                Child = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 9,
                    Children =
                    {
                        Ui.Owl(15, Ui.Brush(Ui.C.Accent)),
                        new TextBlock
                        {
                            Text = $"Mavi: {text}",
                            Foreground = Ui.Brush(Ui.C.Muted),
                            FontSize = 12,
                            FontStyle = FontStyle.Italic,
                            TextWrapping = TextWrapping.Wrap,
                            VerticalAlignment = VerticalAlignment.Center,
                        },
                    },
                },
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
            var panel = new StackPanel { Spacing = 0 };

            panel.Children.Add(BuildCardHeader(Loc.T("nav_learn"), Loc.T("page_learn_subtitle"), "learn"));

            var btnWiz = Ui.BtnIcon("Kurulum sihirbazını başlat", "zap", Ui.BtnKind.Primary);
            btnWiz.Click += (_, _) =>
            {
                _wizardStep = 1;
                SetSection("wizard");
                RenderWizardStep();
            };

            panel.Children.Add(LearnCard("BlueLobby nedir?",
                Ui.Muted("– Sahip olduğun oyunları LAN veya VPN üzerinden arkadaşlarınla oynatır."),
                Ui.Muted("– Oyunun ağ ayarlarını hazırlar, değiştirdiği her dosyayı güvenle yedekler."),
                Ui.Muted("– İstersen her şeyi tek tıkla eski haline döndürür."),
                MaviQuote("Hazırlığın her adımında yanındayım; takıldığın yerde buradayım."),
                new Border { Height = 4, Background = Brushes.Transparent },
                btnWiz));

            panel.Children.Add(LearnCard("Ekran yerleşimi",
                Ui.Muted("– Oyun: klasör seçimi, ayarlar ve Uygulama / Geri Yükle / Başlat butonları."),
                Ui.Muted("– Arkadaşlar: isim ve IP kaydet; çift tıkla IP'yi forma doldurur."),
                Ui.Muted("– Ayarlar: tema, dil, maskot, dokunmatik mod, tam ekran."),
                Ui.Muted("– Log: yapılan her işlemin kaydı; sorun olursa buradan bakılır.")));

            panel.Children.Add(LearnCard("Durum göstergeleri",
                Ui.Muted("– Yeşil: her şey yolunda / hazır."),
                Ui.Muted("– Sarı: eksik var — küçük bir aksiyon gerekli."),
                Ui.Muted("– Kırmızı: bağlantı veya sistem sorunu."),
                Ui.Muted("– Hedef rozeti: kaç dosyanın hazırlandığını gösterir."),
                MaviQuote("Durum şeridine tek bakışta her şeyin yolunda olup olmadığını görebilirsin.")));

            panel.Children.Add(LearnCard("Veri güvenliği",
                Ui.Muted("– Değiştirilen her dosyanın .bak yedeği alınır."),
                Ui.Muted("– Bir hata olursa yarım işlem kalmaz — otomatik geri alma devreye girer."),
                Ui.Muted("– Geri yüklemeden önce yedekler kontrol edilir, bozuksa durulur."),
                Ui.Muted("– Yanlış klasör seçilirse sorulur, emin olmadan devam edilmez."),
                MaviQuote("En kötü senaryoda bile oyununu ilk günkü hâline döndürebilirsin.")));

            panel.Children.Add(LearnCard("Sorun giderme",
                Ui.Muted("– VPN kırmızı: VPN istemcisi açık mı? 'Standart LAN Ayarları'na bas."),
                Ui.Muted("– Bileşen sarı: dosyalar İndirilenler'de mi? Ara butonuyla kopyala."),
                Ui.Muted("– Ping yok: arkadaşın IP'si doğru mu? (100.x Tailscale / 26.x Radmin)"),
                Ui.Muted("– Oyuna katılamıyorsun: arkadaşının IP'sini ping ile test et; yeşil olmalı."),
                Ui.Muted("– 'Bu yöntemle çalışmaz': oyun sunucu tabanlı; başka oyun dene."),
                Ui.Muted("– Hiçbir şey çözülmezse: Log'u dışa aktarıp paylaş.")));

            panel.Children.Add(LearnCard("Kısayollar ve pratik bilgiler",
                Ui.Muted("– Klasörü seçmek için pencereye sürükle-bırak da yapabilirsin."),
                Ui.Muted("– Arkadaş listesinde çift tık = IP otomatik forma gelir."),
                Ui.Muted("– 'Standart LAN Ayarları' tek tıkla çoğu ayarı senin yerine yapar."),
                Ui.Muted("– F11 = tam ekran."),
                Ui.Muted("– Her oyunun ayarını ayrı ayrı hatırlar — bir kez yaz, unut."),
                Ui.Muted("– Geri Yükle her zaman güvenli: önce ne yapılacağını gösterir.")));

            return panel;
        }

        // ===================== Bölüm / tema / maskot =====================

        private void ApplyResponsiveLayout()
        {
            if (_mainColumns == null || _sidebarBorder == null || _compactNav == null) return;
            bool compact = Bounds.Width > 0 && Bounds.Width < 920;
            _mainColumns[0].Width = compact ? new GridLength(0) : new GridLength(240);
            _sidebarBorder.IsVisible = !compact;
            _compactNav.IsVisible = compact;
        }

        private void UpdateCompactNavSelection(string section)
        {
            foreach (Button btn in _compactNavButtons)
                StyleNavButtonState(btn, Equals(btn.Tag as string, section));
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

            (string title, string subtitle) = section switch
            {
                "game" => (Loc.T("page_game_title"), Loc.T("page_game_subtitle")),
                "friends" => (Loc.T("page_friends_title"), Loc.T("page_friends_subtitle")),
                "settings" => (Loc.T("page_settings_title"), Loc.T("page_settings_subtitle")),
                "log" => (Loc.T("page_log_title"), Loc.T("page_log_subtitle")),
                "wizard" => (Loc.T("page_wizard_title"), Loc.T("page_wizard_subtitle")),
                "learn" => (Loc.T("page_learn_title"), Loc.T("page_learn_subtitle")),
                _ => ("BlueLobby", "Windows ve Linux için yerel ağ oyun oturumu yardımcısı."),
            };
            if (_pageTitle != null) _pageTitle.Text = title;
            if (_pageSubtitle != null) _pageSubtitle.Text = subtitle;

            foreach (Button btn in _navButtons)
                StyleNavButtonState(btn, Equals(btn.Tag as string, section));
            UpdateCompactNavSelection(section);
        }

        private void ApplyTheme()
        {
            ThemeManager.Colors c = Ui.C;
            if (Application.Current != null)
            {
                Application.Current.RequestedThemeVariant = ThemeManager.Current == "gunduz"
                    ? ThemeVariant.Light
                    : ThemeVariant.Dark;
            }
            Background = Ui.Brush(c.Window);
            if (_sidebarBorder != null)
            {
                _sidebarBorder.Background = Ui.Brush(c.Sidebar);
                _sidebarBorder.BorderBrush = Ui.Brush(c.Border);
            }
            if (_topHeader != null)
            {
                _topHeader.Background = Brushes.Transparent;
                _topHeader.BorderBrush = Ui.Brush(c.BorderSubtle);
            }
            if (_statusBarBorder != null)
            {
                _statusBarBorder.Background = Ui.Brush(c.Surface);
                _statusBarBorder.BorderBrush = Ui.Brush(c.Border);
            }
            if (_statusBarText != null) _statusBarText.Foreground = Ui.Brush(c.Muted);
            if (_pageTitle != null) _pageTitle.Foreground = Ui.Brush(c.Text);
            if (_pageSubtitle != null) _pageSubtitle.Foreground = Ui.Brush(c.Muted);
            if (_pageEyebrow != null) _pageEyebrow.Foreground = Ui.Brush(c.Muted);
            if (_sidebarTargetText != null) _sidebarTargetText.Foreground = Ui.Brush(c.Text);
            if (_logBox != null)
            {
                _logBox.Background = Ui.Brush(c.Input);
                _logBox.Foreground = Ui.Brush(c.Log);
            }
            foreach (TextBlock block in _mutedBlocks) block.Foreground = Ui.Brush(c.Muted);
            foreach (Button btn in _navButtons)
                StyleNavButtonState(btn, Equals(btn.Tag as string, _section));
            UpdateCompactNavSelection(_section);
            SetSection(_section);
        }

        private void RebuildUi()
        {
            SaveUiToSettings();
            ThemeManager.Get(_settings.Theme);
            Content = BuildUi();
            ApplyTheme();
            RefreshFriendList();
            _ = UpdateSystemStatusAsync();
        }

        private void StartMascot()
        {
            _mascotTimer?.Stop();
            _mascotTimer = null;
            if (_mascotTransform != null) _mascotTransform.Y = 0;
        }
    }
}
