using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Media;

namespace BlueLobby
{
    /// <summary>
    /// BlueLobby v6 "Meridian" görsel dili: nötr yüzeyler, tek vurgu rengi,
    /// vektör çizgi ikonlar, sakin hiyerarşi. Süs yok — her öğe işlev görür.
    /// </summary>
    public static class Ui
    {
        public static bool TouchMode { get; set; }

        // ---- Ölçü token'ları (8px ızgara) ----
        public const double Space1 = 4;
        public const double Space2 = 8;
        public const double Space3 = 12;
        public const double Space4 = 16;
        public const double Space5 = 20;
        public const double Space6 = 24;
        public const double Space7 = 32;

        public static Thickness PagePadding => TouchMode ? new Thickness(16) : new Thickness(32, 24, 32, 18);
        public static Thickness CardPadding => TouchMode ? new Thickness(16) : new Thickness(20, 18);
        public static readonly CornerRadius CardRadius = new(10);
        public static readonly CornerRadius ButtonRadius = new(8);
        public static readonly CornerRadius BadgeRadius = new(6);
        public static Thickness ButtonPadding => TouchMode ? new Thickness(18, 12) : new Thickness(16, 9);
        public static double ButtonMinHeight => TouchMode ? 46 : 36;
        public static double FieldLabelWidth => TouchMode ? 150 : 120;

        // ---- Semantik renk kısayolları ----
        public static IBrush Warn => Brush(C.Warning);
        public static IBrush Vpn => Brush(C.Warning);
        public static IBrush Ok => Brush(C.Success);
        public static IBrush Err => Brush(C.Error);
        public static IBrush Mid => Brush(C.Warning);

        public static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
        public static ThemeManager.Colors C => ThemeManager.Get(ThemeManager.Current);
        private static FontFamily UiFont => new("Inter");

        public enum BtnKind { Primary, Secondary, Ghost, Danger }

        // =============== Vektör çizgi ikonlar (24 ızgara, stroke) ===============

        private static readonly Dictionary<string, string> IconPaths = new()
        {
            ["game"] = "M8.5 7H15.5C18.5 7 21 9.5 21 12.5C21 14.2 19.7 15.5 18 15.5C16.9 15.5 15.9 14.9 15.4 14L14.8 13H9.2L8.6 14C8.1 14.9 7.1 15.5 6 15.5C4.3 15.5 3 14.2 3 12.5C3 9.5 5.5 7 8.5 7Z M8 9.8V12.8 M6.5 11.3H9.5 M15.7 10.7H15.71 M17.7 12.7H17.71",
            ["users"] = "M17 21V19C17 16.8 15.2 15 13 15H7C4.8 15 3 16.8 3 19V21 M10 11C12.2 11 14 9.2 14 7C14 4.8 12.2 3 10 3C7.8 3 6 4.8 6 7C6 9.2 7.8 11 10 11Z M21 21V19C21 17.4 20.1 15.9 18.5 15.4 M15 3.4C16.7 3.9 17.9 5.4 17.9 7C17.9 8.6 16.7 10.1 15 10.6",
            ["learn"] = "M4 4H9C10.7 4 12 5.3 12 7V20C12 18.3 10.7 17 9 17H4Z M20 4H15C13.3 4 12 5.3 12 7V20C12 18.3 13.3 17 15 17H20Z",
            ["settings"] = "M4 21V14 M4 10V3 M12 21V12 M12 8V3 M20 21V16 M20 12V3 M1.5 14H6.5 M9.5 8H14.5 M17.5 16H22.5",
            ["log"] = "M14 3H7C5.9 3 5 3.9 5 5V19C5 20.1 5.9 21 7 21H17C18.1 21 19 20.1 19 19V8L14 3Z M14 3V8H19 M9 12.5H15 M9 16.5H15",
            ["folder"] = "M3 7C3 5.9 3.9 5 5 5H9L11 7H19C20.1 7 21 7.9 21 9V18C21 19.1 20.1 20 19 20H5C3.9 20 3 19.1 3 18V7Z",
            ["search"] = "M10.5 17.5C13.8 17.5 16.5 14.8 16.5 11.5C16.5 8.2 13.8 5.5 10.5 5.5C7.2 5.5 4.5 8.2 4.5 11.5C4.5 14.8 7.2 17.5 10.5 17.5Z M15.4 16.4L20 21",
            ["play"] = "M7.5 4.8V19.2L19 12L7.5 4.8Z",
            ["restore"] = "M2.5 5V10H7.5 M2.9 10C4.4 6.3 8 3.7 12 3.7C17.2 3.7 20.5 7.6 20.5 12C20.5 16.4 16.4 20.3 12 20.3C8.7 20.3 5.8 18.5 4.4 15.8",
            ["download"] = "M12 3.5V14.5 M7 10L12 14.5L17 10 M4.5 17.5V19C4.5 20.1 5.4 21 6.5 21H17.5C18.6 21 19.5 20.1 19.5 19V17.5",
            ["shield"] = "M12 21C12 21 19 17.5 19 11V6L12 3L5 6V11C5 17.5 12 21 12 21Z",
            ["zap"] = "M13 2.5L4.5 13.5H11L10 21.5L19.5 10.5H13L13 2.5Z",
            ["owl"] = "M12 4.6C15.7 4.6 18.5 7.5 18.5 11.2V14C18.5 17.4 15.6 19.9 12 19.9C8.4 19.9 5.5 17.4 5.5 14V11.2C5.5 7.5 8.3 4.6 12 4.6Z M9.7 11.2H9.71 M14.3 11.2H14.31 M11.1 12.9L12 14.5L12.9 12.9 M8.9 4.7L7.4 2.6 M15.1 4.7L16.6 2.6",
        };

        public static Path Icon(string name, double size = 16, IBrush? color = null)
        {
            return new Path
            {
                Data = StreamGeometry.Parse(IconPaths.TryGetValue(name, out string? d) ? d : IconPaths["log"]),
                Stroke = color ?? Brush(C.Text),
                StrokeThickness = 1.7,
                Fill = null,
                StrokeJoin = PenLineJoin.Round,
                StrokeCap = PenLineCap.Round,
                Width = size,
                Height = size,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center,
            };
        }

        /// <summary>Tatlı geometrik baykuş markası: tombul gövde, yumuşak kulaklar, büyük yuvarlak gözler.</summary>
        public static Control Owl(double size = 20, IBrush? color = null)
        {
            IBrush brush = color ?? Brush(C.Accent);
            var canvas = new Canvas
            {
                Width = size,
                Height = size,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            Polygon Ear(double x1, double apex, double x2) => new()
            {
                Points = new List<Point>
                {
                    new(x1, 0.30 * size),
                    new(apex, 0.06 * size),
                    new(x2, 0.30 * size),
                },
                Fill = brush,
            };
            canvas.Children.Add(Ear(0.22 * size, 0.32 * size, 0.42 * size));
            canvas.Children.Add(Ear(0.58 * size, 0.68 * size, 0.78 * size));

            var body = new Border
            {
                Width = 0.70 * size,
                Height = 0.66 * size,
                CornerRadius = new CornerRadius(0.32 * size, 0.32 * size, 0.34 * size, 0.34 * size),
                Background = brush,
            };
            Canvas.SetLeft(body, 0.15 * size);
            Canvas.SetTop(body, 0.20 * size);
            canvas.Children.Add(body);

            var belly = new Border
            {
                Width = 0.34 * size,
                Height = 0.24 * size,
                CornerRadius = new CornerRadius(0.16 * size, 0.16 * size, 0.19 * size, 0.19 * size),
                Background = Brushes.White,
                Opacity = 0.35,
            };
            Canvas.SetLeft(belly, 0.33 * size);
            Canvas.SetTop(belly, 0.56 * size);
            canvas.Children.Add(belly);

            Canvas Eye(double cx, double px)
            {
                var white = new Ellipse { Width = 0.235 * size, Height = 0.235 * size, Fill = Brushes.White };
                Canvas.SetLeft(white, cx - 0.1175 * size);
                Canvas.SetTop(white, 0.31 * size);
                canvas.Children.Add(white);

                var pupil = new Ellipse { Width = 0.105 * size, Height = 0.105 * size, Fill = brush };
                Canvas.SetLeft(pupil, px - 0.0525 * size);
                Canvas.SetTop(pupil, 0.40 * size);
                canvas.Children.Add(pupil);

                var spark = new Ellipse { Width = 0.04 * size, Height = 0.04 * size, Fill = Brushes.White };
                Canvas.SetLeft(spark, px - 0.02 * size);
                Canvas.SetTop(spark, 0.415 * size);
                canvas.Children.Add(spark);
                return canvas;
            }
            Eye(0.355 * size, 0.375 * size);
            Eye(0.645 * size, 0.625 * size);

            var beak = new Polygon
            {
                Points = new List<Point>
                {
                    new(0.468 * size, 0.55 * size),
                    new(0.532 * size, 0.55 * size),
                    new(0.50 * size, 0.645 * size),
                },
                Fill = Brush("#F97316"),
            };
            canvas.Children.Add(beak);

            var beakGloss = new Ellipse
            {
                Width = 0.028 * size,
                Height = 0.014 * size,
                Fill = Brushes.White,
                Opacity = 0.55,
            };
            Canvas.SetLeft(beakGloss, 0.489 * size);
            Canvas.SetTop(beakGloss, 0.572 * size);
            canvas.Children.Add(beakGloss);

            return canvas;
        }

        // =============== Düğmeler ===============

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
                FontWeight = FontWeight.Medium,
                BorderThickness = new Thickness(1),
            };
            ApplyBtnStyle(btn, kind);
            btn.PointerEntered += (_, _) =>
            {
                if (!btn.IsEnabled) return;
                btn.Background = kind switch
                {
                    BtnKind.Primary => Brush(C.AccentHover),
                    BtnKind.Danger => Brush(C.Hover),
                    _ => Brush(C.Hover),
                };
            };
            btn.PointerExited += (_, _) => ApplyBtnStyle(btn, kind);
            return btn;
        }

        /// <summary>İkon + metinli düğme; ikon rengi düğme türüne göre ayarlanır.</summary>
        public static Button BtnIcon(string text, string icon, BtnKind kind = BtnKind.Secondary, double iconSize = 15)
        {
            var iconColor = kind switch
            {
                BtnKind.Primary => Brush(C.AccentText),
                BtnKind.Ghost => Brush(C.Accent),
                BtnKind.Danger => Brush(C.Error),
                _ => Brush(C.Muted),
            };
            var content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            content.Children.Add(Icon(icon, iconSize, iconColor));
            content.Children.Add(new TextBlock
            {
                Text = text,
                FontFamily = UiFont,
                FontSize = 12.5,
                FontWeight = FontWeight.Medium,
                VerticalAlignment = VerticalAlignment.Center,
            });

            var btn = new Button
            {
                Content = content,
                Padding = ButtonPadding,
                CornerRadius = ButtonRadius,
                MinHeight = ButtonMinHeight,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                BorderThickness = new Thickness(1),
            };
            ApplyBtnStyle(btn, kind);
            btn.PointerEntered += (_, _) =>
            {
                if (!btn.IsEnabled) return;
                btn.Background = kind switch
                {
                    BtnKind.Primary => Brush(C.AccentHover),
                    BtnKind.Danger => Brush(C.Hover),
                    _ => Brush(C.Hover),
                };
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
                    btn.BorderBrush = Brushes.Transparent;
                    btn.FontWeight = FontWeight.SemiBold;
                    break;
                case BtnKind.Danger:
                    btn.Background = Brush(C.Card);
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
                    break;
            }
        }

        // =============== Girdiler ===============

        public static void StyleInput(Control control)
        {
            switch (control)
            {
                case TextBox textBox:
                    textBox.FontFamily = UiFont;
                    textBox.FontSize = 12.5;
                    textBox.Foreground = Brush(C.Text);
                    textBox.Background = Brush(C.Input);
                    textBox.BorderBrush = Brush(C.BorderSubtle);
                    textBox.BorderThickness = new Thickness(1);
                    textBox.Padding = new Thickness(11, 8);
                    break;
                case ComboBox comboBox:
                    comboBox.FontFamily = UiFont;
                    comboBox.FontSize = 12.5;
                    comboBox.Foreground = Brush(C.Text);
                    comboBox.Background = Brush(C.Input);
                    comboBox.BorderBrush = Brush(C.BorderSubtle);
                    comboBox.BorderThickness = new Thickness(1);
                    comboBox.Padding = new Thickness(9, 5);
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

        // =============== Yüzeyler ===============

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
                BorderBrush = Brush(C.BorderSubtle),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(13),
                Child = child,
            };
        }

        /// <summary>İnce yatay ayırıcı çizgi.</summary>
        public static Control Divider()
        {
            return new Border
            {
                Background = Brush(C.BorderSubtle),
                Height = 1,
                Margin = new Thickness(0, 4, 0, 4),
            };
        }

        // =============== Rozet / çip ===============

        public static Border Chip(TextBlock inner)
        {
            return new Border
            {
                Background = Brush(C.Surface),
                BorderBrush = Brush(C.BorderSubtle),
                BorderThickness = new Thickness(1),
                CornerRadius = BadgeRadius,
                Padding = new Thickness(9, 5),
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

        public static WrapPanel BtnRow(params Control[] buttons)
        {
            var row = new WrapPanel { Orientation = Orientation.Horizontal };
            foreach (Control button in buttons)
            {
                button.Margin = new Thickness(0, 0, 10, 10);
                row.Children.Add(button);
            }
            return row;
        }

        // =============== Tipografi ===============

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
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush(C.Title),
        };

        public static TextBlock PageTitle(string text) => new()
        {
            Text = text,
            FontFamily = UiFont,
            FontSize = 22,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush(C.Text),
        };

        public static TextBlock SectionTitle(string text) => new()
        {
            Text = text,
            FontFamily = UiFont,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush(C.Text),
        };
    }
}
