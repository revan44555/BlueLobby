using System.Collections.Generic;
using Avalonia.Media;

namespace BlueLobby
{
    /// <summary>Üç tema (gece/gündüz/pembe) — renk verisi Legacy WPF ThemeManager'dan taşındı.</summary>
    public static class ThemeManager
    {
        public sealed class Colors
        {
            public string Window = "#181825";
            public string Sidebar = "#11111b";
            public string Card = "#1e1e2e";
            public string Text = "#cdd6f4";
            public string Muted = "#a6adc8";
            public string Title = "#f5e0dc";
            public string Log = "#a6e3a1";
            public string Accent = "#89b4fa";
        }

        private static readonly Dictionary<string, Colors> Themes = new()
        {
            ["gece"] = new Colors(),
            ["gunduz"] = new Colors
            {
                Window = "#f3f4f8", Sidebar = "#e9edf3", Card = "#ffffff",
                Text = "#20263a", Muted = "#5b6478", Title = "#6d28d9", Log = "#166534", Accent = "#6d28d9",
            },
            ["pembe"] = new Colors
            {
                Window = "#2a1722", Sidebar = "#1d1018", Card = "#3a2130",
                Text = "#ffe3ef", Muted = "#e7aec6", Title = "#ffb8d4", Log = "#a6e3a1", Accent = "#f472b6",
            },
        };

        public static string Current { get; private set; } = "gece";
        public static IReadOnlyCollection<string> Names => Themes.Keys;

        public static Colors Get(string theme)
        {
            if (!Themes.TryGetValue(theme, out Colors? t))
            {
                t = Themes["gece"];
            }
            Current = Themes.ContainsKey(theme) ? theme : "gece";
            return t;
        }

        public static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
    }
}
