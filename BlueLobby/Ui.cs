using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace BlueLobby
{
    /// <summary>
    /// BlueLobby görsel sisteminin merkezi. İş mantığı içermez.
    /// Desktop-first Windows/Linux görünümünü burada tutar.
    /// </summary>
    public static class Ui
    {
        public static bool TouchMode { get; set; }

        public static Thickness PagePadding => TouchMode ? new Thickness(18) : new Thickness(24);
        public static Thickness CardPadding => TouchMode ? new Thickness(18) : new Thickness(16);
        public static readonly CornerRadius CardRadius = new(14);
        public static readonly CornerRadius ButtonRadius = new(9);
        public static readonly CornerRadius BadgeRadius = new(8);
        public static Thickness ButtonPadding => TouchMode ? new Thickness(18, 12) : new Thickness(14, 9);
        public static double ButtonMinHeight => TouchMode ? 48 : 40;
        public static double FieldLabelWidth => TouchMode ? 150 : 120;

        public static readonly IBrush Warn = Brush("#F59E0B");
        public static readonly IBrush Vpn = Brush("#F9E2AF");
        public static readonly IBrush Ok = Brush("#22C55E");
        public static readonly IBrush Err = Brush("#EF4444");
        public static readonly IBrush Mid = Brush("#EAB308");

        public static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
        public static ThemeManager.Colors C => ThemeManager.Get(ThemeManager.Current);

        public enum BtnKind { Primary, Secondary, Ghost, Danger }

        public static Button Btn(string text, BtnKind kind = BtnKind.Secondary)
        {
            var btn = new Button
            {
                Content = text,
                Padding = ButtonPadding,
                CornerRadius = ButtonRadius,
                MinHeight = ButtonMinHeight,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontWeight = FontWeight.SemiBold,
            };
            ApplyBtnStyle(btn, kind);
            return btn;
        }

        public static void ApplyBtnStyle(Button btn, BtnKind kind)
        {
            btn.BorderThickness = new Thickness(1);
            switch (kind)
            {
                case BtnKind.Primary:
                    btn.Background = Brush(C.Accent);
                    btn.Foreground = Brush(C.Window);
                    btn.BorderBrush = Brush(C.Accent);
                    break;
                case BtnKind.Danger:
                    btn.Background = Brush("#351B20");
                    btn.Foreground = Brush("#FCA5A5");
                    btn.BorderBrush = Brush("#7F1D1D");
                    break;
                case BtnKind.Secondary:
                    btn.Background = Brush(C.Card);
                    btn.Foreground = Brush(C.Text);
                    btn.BorderBrush = Brush(C.Border);
                    break;
                default:
                    btn.Background = Brushes.Transparent;
                    btn.Foreground = Brush(C.Muted);
                    btn.BorderBrush = Brushes.Transparent;
                    btn.FontWeight = FontWeight.Medium;
                    break;
            }
        }

        public static Border Card(Control child, double spacingAfter = 12)
        {
            return new Border
            {
                Background = Brush(C.Card),
                BorderBrush = Brush(C.Border),
                BorderThickness = new Thickness(1),
                CornerRadius = CardRadius,
                Padding = CardPadding,
                Margin = new Thickness(0, 0, 0, spacingAfter),
                Child = child,
            };
        }

        public static Border Surface(Control child)
        {
            return new Border
            {
                Background = Brush(C.Surface),
                BorderBrush = Brush(C.Border),
                BorderThickness = new Thickness(1),
                CornerRadius = CardRadius,
                Padding = CardPadding,
                Child = child,
            };
        }

        public static Border Chip(TextBlock inner)
        {
            return new Border
            {
                Background = Brush(C.Surface),
                BorderBrush = Brush(C.Border),
                BorderThickness = new Thickness(1),
                CornerRadius = BadgeRadius,
                Padding = new Thickness(10, 6),
                Margin = new Thickness(0, 0, 8, 8),
                Child = inner,
            };
        }

        public static TextBlock ChipText(string text) => new()
        {
            Text = text,
            Foreground = Brush(C.Muted),
            FontSize = 12.5,
            FontWeight = FontWeight.Medium,
        };

        public static Control FieldRow(string label, Control control, double labelWidth = 0)
        {
            if (labelWidth <= 0) labelWidth = FieldLabelWidth;
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions($"{labelWidth},14,*") };
            var lbl = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brush(C.Muted),
                FontSize = 12.5,
                FontWeight = FontWeight.Medium,
            };
            grid.Children.Add(lbl); Grid.SetColumn(lbl, 0);
            grid.Children.Add(control); Grid.SetColumn(control, 2);
            return grid;
        }

        public static TextBlock Muted(string text) => new()
        {
            Text = text,
            Foreground = Brush(C.Muted),
            TextWrapping = TextWrapping.Wrap,
        };

        public static TextBlock Title(string text) => new()
        {
            Text = text,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = Brush(C.Title),
        };

        public static TextBlock PageTitle(string text) => new()
        {
            Text = text,
            FontSize = 27,
            FontWeight = FontWeight.Bold,
            Foreground = Brush(C.Text),
        };

        public static TextBlock SectionTitle(string text) => new()
        {
            Text = text,
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush(C.Text),
        };

        public static Border Badge(string text, IBrush? foreground = null, IBrush? background = null)
        {
            return new Border
            {
                Background = background ?? Brush(C.SurfaceStrong),
                BorderBrush = Brush(C.Border),
                BorderThickness = new Thickness(1),
                CornerRadius = BadgeRadius,
                Padding = new Thickness(9, 5),
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = foreground ?? Brush(C.Muted),
                    FontSize = 11.5,
                    FontWeight = FontWeight.SemiBold,
                },
            };
        }

        public static Border AccentBar()
        {
            return new Border
            {
                Background = Brush(C.Accent),
                CornerRadius = new CornerRadius(3),
                Width = 4,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
        }

        public static WrapPanel BtnRow(params Control[] buttons)
        {
            var row = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (Control b in buttons)
            {
                b.Margin = new Thickness(0, 0, 8, 8);
                row.Children.Add(b);
            }
            return row;
        }
    }
}
