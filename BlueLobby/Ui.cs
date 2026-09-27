using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace BlueLobby
{
    /// <summary>
    /// Arayüz stil yardımcıları. Görsel tasarımı değiştirmek isteyen (AI dahil) yalnızca
    /// bu dosyadaki sabitleri ve metotları düzenlemeli; iş mantığı MainWindow'dadır.
    /// </summary>
    public static class Ui
    {
        // ---- Ölçüler (Steam Deck dokunmatik modda otomatik büyür) ----
        public static bool TouchMode { get; set; }
        public static Thickness CardPadding => TouchMode ? new Thickness(20) : new Thickness(14);
        public static readonly CornerRadius CardRadius = new(10);
        public static readonly CornerRadius ButtonRadius = new(8);
        public static Thickness ButtonPadding => TouchMode ? new Thickness(20, 14) : new Thickness(12, 8);
        public static double ButtonMinHeight => TouchMode ? 52 : 0;
        public static double FieldLabelWidth => TouchMode ? 150 : 110;

        // ---- Sabit vurgu renkleri (tema dışı) ----
        public static readonly IBrush Warn = Brush("#f59e0b");
        public static readonly IBrush Vpn = Brush("#f9e2af");
        public static readonly IBrush Ok = Brush("#22c55e");
        public static readonly IBrush Err = Brush("#ef4444");
        public static readonly IBrush Mid = Brush("#eab308");

        public static IBrush Brush(string hex) => new SolidColorBrush(Avalonia.Media.Color.Parse(hex));

        // ---- Temadan canlı renkler ----
        public static ThemeManager.Colors C => ThemeManager.Get(ThemeManager.Current);

        // ===================== Bileşen fabrikaları =====================

        public enum BtnKind { Primary, Secondary, Ghost }

        public static Button Btn(string text, BtnKind kind = BtnKind.Secondary)
        {
            var btn = new Button
            {
                Content = text,
                Padding = ButtonPadding,
                CornerRadius = ButtonRadius,
                MinHeight = ButtonMinHeight,
                HorizontalContentAlignment = HorizontalAlignment.Center,
            };
            ApplyBtnStyle(btn, kind);
            return btn;
        }

        public static void ApplyBtnStyle(Button btn, BtnKind kind)
        {
            switch (kind)
            {
                case BtnKind.Primary:
                    btn.Background = Brush(C.Accent);
                    btn.Foreground = Brush(C.Window);
                    btn.FontWeight = FontWeight.SemiBold;
                    break;
                case BtnKind.Secondary:
                    btn.Background = Brush(C.Card);
                    btn.Foreground = Brush(C.Text);
                    break;
                default:
                    btn.Background = Brush("transparent");
                    btn.Foreground = Brush(C.Muted);
                    break;
            }
        }

        /// <summary>Kart: köşeli, temalı zeminli konteyner.</summary>
        public static Border Card(Control child, double spacingAfter = 10)
        {
            return new Border
            {
                Background = Brush(C.Card),
                CornerRadius = CardRadius,
                Padding = CardPadding,
                Margin = new Avalonia.Thickness(0, 0, 0, spacingAfter),
                Child = child
            };
        }

        /// <summary>Durum çipinin metin kısmı (güncelleme için saklanır).</summary>
        public static TextBlock ChipText(string text)
        {
            return new TextBlock { Text = text, Foreground = Brush(C.Muted) };
        }

        /// <summary>Durum çipi (VPN/Ping/Emu/Patch göstergeleri).</summary>
        public static Border Chip(TextBlock inner)
        {
            return new Border
            {
                Background = Brush(C.Card),
                CornerRadius = new CornerRadius(9),
                Padding = new Avalonia.Thickness(10, 4),
                Margin = new Avalonia.Thickness(0, 0, 8, 8),
                Child = inner
            };
        }

        /// <summary>Etiket + kontrol satırı.</summary>
        public static Control FieldRow(string label, Control control, double labelWidth = 0)
        {
            if (labelWidth <= 0) labelWidth = FieldLabelWidth;
            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions($"{labelWidth},12,*") };
            var lbl = new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center };
            grid.Children.Add(lbl); Grid.SetColumn(lbl, 0);
            grid.Children.Add(control); Grid.SetColumn(control, 2);
            return grid;
        }

        public static TextBlock Muted(string text) => new TextBlock { Text = text, Foreground = Brush(C.Muted) };

        public static TextBlock Title(string text) => new TextBlock
        {
            Text = text,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = Brush(C.Title)
        };

        /// <summary>Yatay buton grubu.</summary>
        public static StackPanel BtnRow(params Control[] buttons)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
            foreach (Control b in buttons) row.Children.Add(b);
            return row;
        }
    }
}
