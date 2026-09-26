using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace SteamLANControlCenter
{
    public static class ThemeManager
    {
        private sealed class Theme
        {
            public string Window = "#181825";
            public string Card = "#1e1e2e";
            public string Input = "#11111b";
            public string Border = "#313244";
            public string BorderStrong = "#45475a";
            public string Text = "#cdd6f4";
            public string Muted = "#a6adc8";
            public string Title = "#f5e0dc";
            public string Log = "#a6e3a1";
            public string ButtonBg = "#45475a";
        }

        private static readonly Dictionary<string, Theme> Themes = new()
        {
            ["gece"] = new Theme(),
            ["gunduz"] = new Theme
            {
                Window = "#f3f4f8",
                Card = "#ffffff",
                Input = "#e9edf3",
                Border = "#d4d9e2",
                BorderStrong = "#b9c1cf",
                Text = "#20263a",
                Muted = "#5b6478",
                Title = "#6d28d9",
                Log = "#166534",
                ButtonBg = "#d7dce6",
            },
            ["pembe"] = new Theme
            {
                Window = "#2a1722",
                Card = "#3a2130",
                Input = "#1d1018",
                Border = "#5a3547",
                BorderStrong = "#7a4a5f",
                Text = "#ffe3ef",
                Muted = "#e7aec6",
                Title = "#ffb8d4",
                Log = "#a6e3a1",
                ButtonBg = "#7a4a5f",
            },
        };

        public static string Current { get; private set; } = "gece";

        public static void Apply(Window window, string theme)
        {
            if (!Themes.TryGetValue(theme, out Theme? t))
            {
                t = Themes["gece"];
                theme = "gece";
            }

            Current = theme;

            var map = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["#181825"] = t.Window,
                ["#1e1e2e"] = t.Card,
                ["#11111b"] = t.Input,
                ["#313244"] = t.Border,
                ["#45475a"] = t.BorderStrong,
                ["#cdd6f4"] = t.Text,
                ["#a6adc8"] = t.Muted,
                ["#f5e0dc"] = t.Title,
                ["#a6e3a1"] = t.Log,
            };

            ApplyRecursive(window, map, t);
        }

        private static void ApplyRecursive(DependencyObject node, Dictionary<string, string> map, Theme t)
        {
            if (node is FrameworkElement el)
            {
                if (el is Control baseControl)
                {
                    baseControl.Background = Remap(baseControl.Background, map, t);
                    baseControl.Foreground = Remap(baseControl.Foreground, map, t);
                    baseControl.BorderBrush = Remap(baseControl.BorderBrush, map, t);
                }
                else if (el is Panel panel)
                {
                    panel.Background = Remap(panel.Background, map, t);
                }
                else if (el is TextBlock textBlock)
                {
                    textBlock.Foreground = Remap(textBlock.Foreground, map, t);
                }

                if (el is Control control)
                {

                    if (control is Button && control.Background is SolidColorBrush buttonBrush &&
                        buttonBrush.Color == (Color)ColorConverter.ConvertFromString("#45475a"))
                    {
                        control.Background = Make(t.ButtonBg);
                    }

                    if (control is Button && control.Foreground is SolidColorBrush buttonFg &&
                        buttonFg.Color == (Color)ColorConverter.ConvertFromString("#cdd6f4"))
                    {
                        control.Foreground = Make(t.Text);
                    }
                }

                if (el is Border border)
                {
                    border.BorderBrush = Remap(border.BorderBrush, map, t);
                    border.Background = Remap(border.Background, map, t);
                }

                if (el is ProgressBar progress)
                {
                    progress.Background = Make(t.Input);
                    progress.BorderBrush = Remap(progress.BorderBrush, map, t);
                    progress.Foreground = Make("#89b4fa");
                }
            }

            int count = VisualTreeHelper.GetChildrenCount(node);
            for (int i = 0; i < count; i++)
            {
                if (VisualTreeHelper.GetChild(node, i) is DependencyObject child)
                {
                    ApplyRecursive(child, map, t);
                }
            }
        }

        private static Brush? Remap(Brush? brush, Dictionary<string, string> map, Theme t)
        {
            if (brush is SolidColorBrush solid)
            {
                string hex = solid.Color.ToString();
                if (map.TryGetValue(hex, out string? next))
                {
                    return Make(next);
                }
            }

            return brush;
        }

        private static SolidColorBrush Make(string hex)
        {
            var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
            brush.Freeze();
            return brush;
        }
    }
}
