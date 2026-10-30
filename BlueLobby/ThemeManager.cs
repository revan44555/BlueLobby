using System.Collections.Generic;
using Avalonia.Media;

namespace BlueLobby
{
    /// <summary>Centralized semantic palettes for the application shell and controls.</summary>
    public static class ThemeManager
    {
        public sealed class Colors
        {
            public string Window = "#0D1015";
            public string Sidebar = "#10151C";
            public string Card = "#151B23";
            public string Text = "#EDF2F8";
            public string Muted = "#9AA6B5";
            public string Title = "#EDF2F8";
            public string Log = "#B7D8C0";
            public string Accent = "#6F9FF6";
            public string AccentHover = "#82ADFA";
            public string AccentText = "#0B1422";
            public string Surface = "#111821";
            public string SurfaceStrong = "#1B2633";
            public string Hover = "#202D3B";
            public string Input = "#101720";
            public string Border = "#293442";
            public string BorderSubtle = "#202A36";
            public string Success = "#58B88A";
            public string Warning = "#D9A653";
            public string Error = "#D97777";
            public string Info = "#6F9FF6";
        }

        private static readonly Dictionary<string, Colors> Themes = new()
        {
            ["gece"] = new Colors(),
            ["gunduz"] = new Colors
            {
                Window = "#F4F6F9", Sidebar = "#EBEFF4", Card = "#FFFFFF",
                Text = "#1D2733", Muted = "#637184", Title = "#1D2733", Log = "#276247",
                Accent = "#356FC5", AccentHover = "#285FAE", AccentText = "#FFFFFF",
                Surface = "#F8FAFC", SurfaceStrong = "#E8EEF5", Hover = "#E5ECF5",
                Input = "#FFFFFF", Border = "#D6DEE8", BorderSubtle = "#E4E9F0",
                Success = "#23845A", Warning = "#A86B16", Error = "#B84545", Info = "#356FC5",
            },
            ["pembe"] = new Colors
            {
                Window = "#171219", Sidebar = "#1D151D", Card = "#241B25",
                Text = "#F1EAF2", Muted = "#B7A8B9", Title = "#F1EAF2", Log = "#B7D8C0",
                Accent = "#C58AB2", AccentHover = "#D39CC0", AccentText = "#211420",
                Surface = "#1D1720", SurfaceStrong = "#302432", Hover = "#382A3A",
                Input = "#1A141C", Border = "#473747", BorderSubtle = "#362A38",
                Success = "#69B88E", Warning = "#D9A653", Error = "#D97788", Info = "#B08CCB",
            },
        };

        public static string Current { get; private set; } = "gece";
        public static IReadOnlyCollection<string> Names => Themes.Keys;

        public static Colors Get(string theme)
        {
            if (!Themes.TryGetValue(theme, out Colors? palette))
            {
                palette = Themes["gece"];
            }

            Current = Themes.ContainsKey(theme) ? theme : "gece";
            return palette;
        }

        public static IBrush Brush(string hex) => new SolidColorBrush(Color.Parse(hex));
    }
}