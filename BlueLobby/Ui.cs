using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace BlueLobby
{
    /// <summary>
    /// Shared visual language for BlueLobby. This class contains presentation helpers only.
    /// </summary>
    public static class Ui
    {
        public static bool TouchMode { get; set; }

        public const double Space1 = 4;
        public const double Space2 = 8;
        public const double Space3 = 12;
        public const double Space4 = 16;
        public const double Space5 = 20;
        public const double Space6 = 24;
        public const double Space7 = 32;

        public static Thickness PagePadding => TouchMode ? new Thickness(18) : new Thickness(28, 24, 28, 18);
        public static Thickness CardPadding => TouchMode ? new Thickness(18) : new Thickness(18, 16);
        public static readonly CornerRadius CardRadius = new(11);
        public static readonly CornerRadius ButtonRadius = new(8);
        public static readonly CornerRadius BadgeRadius = new(6);
        public static Thickness ButtonPadding => TouchMode ? new Thickness(18, 12) : new Thickness(14, 8);
        public static double ButtonMinHeight => TouchMode ? 46 : 36;
        public static double FieldLabelWidth => TouchMode ? 150 : 120;

        public static IBrush Warn => Brush(C.Warning);
        public static IBrush Vpn => Brush(C.Warning);
        public static IBrush Ok => Brush(C.Success);
        public static IBrush Err => Brush(C.Error);
        public static IBrush Mid => Brush(C.Warning);

        public static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
        public static ThemeManager.Colors C => ThemeManager.Get(ThemeManager.Current);
        private static FontFamily UiFont => new("Inter");

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
                FontFamily = UiFont,
                FontSize = 12.5,
                FontWeight = FontWeight.SemiBold,
                BorderThickness = new Thickness(1),
            };
            ApplyBtnStyle(btn, kind);
            btn.PointerEntered += (_, _) =>
            {
                if (!btn.IsEnabled) return;
                btn.Background = kind switch
                {
                    BtnKind.Primary => Brush(C.AccentHover),
                    BtnKind.Danger => Brush("#4A252D"),
                    BtnKind.Ghost => Brush(C.Hover),
                    _ => Brush(C.Hover),
                };
                if (kind != BtnKind.Ghost) btn.BorderBrush = Brush(C.Border);
            };
            btn.PointerExited += (_, _) => ApplyBtnStyle(btn, kind);
            return btn;
        }

        public static void ApplyBtnStyle(Button btn, BtnKind kind)
        {
            btn.BorderThickness = new Thickness(1);
            switch (kind)
            {
                case BtnKind.Primary:
                    btn.Background = Brush(C.Accent);
                    btn.Foreground = Brush(C.AccentText);
                    btn.BorderBrush = Brush(C.Accent);
                    break;
                case BtnKind.Danger:
                    btn.Background = Brush(C.SurfaceStrong);
                    btn.Foreground = Brush(C.Error);
                    btn.BorderBrush = Brush(C.Border);
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

        public static void StyleInput(Control control)
        {
            switch (control)
            {
                case TextBox textBox:
                    textBox.FontFamily = UiFont;
                    textBox.FontSize = 12.5;
                    textBox.Foreground = Brush(C.Text);
                    textBox.Background = Brush(C.Input);
                    textBox.BorderBrush = Brush(C.Border);
                    textBox.BorderThickness = new Thickness(1);
                    textBox.Padding = new Thickness(10, 7);
                    break;
                case ComboBox comboBox:
                    comboBox.FontFamily = UiFont;
                    comboBox.FontSize = 12.5;
                    comboBox.Foreground = Brush(C.Text);
                    comboBox.Background = Brush(C.Input);
                    comboBox.BorderBrush = Brush(C.Border);
                    comboBox.BorderThickness = new Thickness(1);
                    comboBox.Padding = new Thickness(8, 4);
                    break;
                case ListBox listBox:
                    listBox.FontFamily = UiFont;
                    listBox.FontSize = 12.5;
                    listBox.Foreground = Brush(C.Text);
                    listBox.Background = Brush(C.Input);
                    listBox.BorderBrush = Brush(C.BorderSubtle);
                    listBox.BorderThickness = new Thickness(1);
                    break;
                case CheckBox checkBox:
                    checkBox.FontFamily = UiFont;
                    checkBox.FontSize = 12;
                    checkBox.Foreground = Brush(C.Text);
                    break;
            }
        }

        public static Border Card(Control child, double spacingAfter = 12)
        {
            return new Border
            {
                Background = Brush(C.Card),
                BorderBrush = Brush(C.BorderSubtle),
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
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Child = child,
            };
        }

        public static Border Chip(TextBlock inner)
        {
            return new Border
            {
                Background = Brush(C.Surface),
                BorderBrush = Brush(C.BorderSubtle),
                BorderThickness = new Thickness(1),
                CornerRadius = BadgeRadius,
                Padding = new Thickness(9, 5),
                Margin = new Thickness(0, 0, 8, 8),
                Child = inner,
            };
        }

        public static TextBlock ChipText(string text) => new()
        {
            Text = text,
            FontFamily = UiFont,
            Foreground = Brush(C.Muted),
            FontSize = 11.5,
            FontWeight = FontWeight.Medium,
        };

        public static Border StatusTile(string label, TextBlock value, string detail)
        {
            value.FontFamily = UiFont;
            value.FontSize = 16;
            value.FontWeight = FontWeight.SemiBold;
            value.Foreground = Brush(C.Text);

            var body = new StackPanel { Spacing = 5 };
            body.Children.Add(new TextBlock
            {
                Text = label.ToUpperInvariant(),
                FontFamily = UiFont,
                Foreground = Brush(C.Muted),
                FontSize = 9.5,
                FontWeight = FontWeight.SemiBold,
                Opacity = 0.9,
            });
            body.Children.Add(value);
            body.Children.Add(new TextBlock
            {
                Text = detail,
                FontFamily = UiFont,
                Foreground = Brush(C.Muted),
                FontSize = 10.5,
                TextWrapping = TextWrapping.Wrap,
                Opacity = 0.82,
            });

            return new Border
            {
                Background = Brush(C.Surface),
                BorderBrush = Brush(C.BorderSubtle),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(9),
                Padding = new Thickness(13, 11),
                Margin = new Thickness(0, 0, 8, 8),
                Width = TouchMode ? 220 : 178,
                Child = body,
            };
        }

        public static Border FieldCard(string label, Control control)
        {
            StyleInput(control);
            var body = new StackPanel { Spacing = 7 };
            body.Children.Add(new TextBlock
            {
                Text = label,
                FontFamily = UiFont,
                Foreground = Brush(C.Muted),
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
            });
            body.Children.Add(control);

            return new Border
            {
                Background = Brush(C.Surface),
                BorderBrush = Brush(C.BorderSubtle),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(11, 10),
                Margin = new Thickness(0, 0, 8, 8),
                Width = TouchMode ? 300 : 250,
                Child = body,
            };
        }

        public static Control FieldRow(string label, Control control, double labelWidth = 0)
        {
            StyleInput(control);
            if (labelWidth <= 0) labelWidth = FieldLabelWidth;
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions($"{labelWidth},14,*") };
            var lbl = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brush(C.Muted),
                FontFamily = UiFont,
                FontSize = 12,
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
            FontFamily = UiFont,
            FontSize = 12,
            TextWrapping = TextWrapping.Wrap,
        };

        public static TextBlock Title(string text) => new()
        {
            Text = text,
            FontFamily = UiFont,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = Brush(C.Title),
        };

        public static TextBlock PageTitle(string text) => new()
        {
            Text = text,
            FontFamily = UiFont,
            FontSize = 27,
            FontWeight = FontWeight.Bold,
            Foreground = Brush(C.Text),
        };

        public static TextBlock SectionTitle(string text) => new()
        {
            Text = text,
            FontFamily = UiFont,
            FontSize = 15,
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
                Padding = new Thickness(8, 4),
                Child = new TextBlock
                {
                    Text = text,
                    FontFamily = UiFont,
                    Foreground = foreground ?? Brush(C.Muted),
                    FontSize = 10.5,
                    FontWeight = FontWeight.SemiBold,
                },
            };
        }

        public static Border AccentBar()
        {
            return new Border
            {
                Background = Brush(C.Accent),
                CornerRadius = new CornerRadius(2),
                Width = 3,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
        }

        public static WrapPanel BtnRow(params Control[] buttons)
        {
            var row = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (Control button in buttons)
            {
                button.Margin = new Thickness(0, 0, 8, 8);
                row.Children.Add(button);
            }
            return row;
        }
    }
}