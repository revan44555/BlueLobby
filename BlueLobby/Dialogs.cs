using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace BlueLobby
{
    /// <summary>Küçük onay/uyarı diyaloğu (Avalonia MessageBox eşdeğeri).</summary>
    public class ConfirmDialog : Window
    {
        public ConfirmDialog(string title, IEnumerable<string> lines, string yesText, string noText)
        {
            Title = title;
            Width = 540; Height = 340; MinWidth = 400; MinHeight = 220;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 8 };
            foreach (string line in lines)
            {
                panel.Children.Add(new TextBlock { Text = line, TextWrapping = TextWrapping.Wrap });
            }

            var yes = new Button { Content = yesText, MinWidth = 90 };
            yes.Click += (_, _) => Close(true);
            var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Margin = new Avalonia.Thickness(0, 10, 0, 0) };
            buttons.Children.Add(yes);
            if (!string.IsNullOrEmpty(noText))
            {
                var no = new Button { Content = noText, MinWidth = 90 };
                no.Click += (_, _) => Close(false);
                buttons.Children.Add(no);
            }
            panel.Children.Add(buttons);
            Content = panel;
        }
    }

    /// <summary>Metin girişi isteyen küçük diyalog (profil kodu yapıştırma için).</summary>
    public class InputDialog : Window
    {
        public string ResultText { get; private set; } = string.Empty;
        private readonly TextBox _box;

        public InputDialog(string title, string watermark)
        {
            Title = title;
            Width = 560; Height = 170; MinWidth = 440; MinHeight = 150;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;

            _box = new TextBox { Watermark = watermark };
            var ok = new Button { Content = Loc.T("yes"), MinWidth = 90 };
            ok.Click += (_, _) => { ResultText = _box.Text ?? string.Empty; Close(true); };
            var cancel = new Button { Content = Loc.T("no"), MinWidth = 90 };
            cancel.Click += (_, _) => Close(false);

            var panel = new StackPanel { Margin = new Avalonia.Thickness(16), Spacing = 10 };
            panel.Children.Add(_box);
            panel.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8,
                Children = { ok, cancel }
            });
            Content = panel;
        }
    }
}
