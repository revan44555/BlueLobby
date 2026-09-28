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
            public string Accent = "#7C3AED";
            public string Surface = "#181827";
            public string SurfaceStrong = "#202034";
            public string Border = "#31314A";
        }

        private static readonly Dictionary<string, Colors> Themes = new()
        {
            ["gece"] = new Colors(),
            ["gunduz"] = new Colors
            {
                Window = "#F4F5F9", Sidebar = "#EAECF2", Card = "#FFFFFF",
                Text = "#1F2430", Muted = "#667085", Title = "#4F46E5", Log = "#166534", Accent = "#4F46E5",
                Surface = "#F8F9FC", SurfaceStrong = "#EEF0F6", Border = "#D9DDE8",
            },
            ["pembe"] = new Colors
            {
                Window = "#24131D", Sidebar = "#190D14", Card = "#34202B",
                Text = "#FDE7F2", Muted = "#D8A9BD", Title = "#FFB8D4", Log = "#A6E3A1", Accent = "#F472B6",
                Surface = "#2B1924", SurfaceStrong = "#402330", Border = "#5A3044",
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
