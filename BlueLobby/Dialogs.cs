using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace BlueLobby
{
    /// <summary>Shared, theme-aware confirmation and information dialog.</summary>
    public class ConfirmDialog : Window
    {
        public ConfirmDialog(string title, IEnumerable<string> lines, string yesText, string noText)
        {
            Title = title;
            Width = 520;
            Height = 310;
            MinWidth = 390;
            MinHeight = 210;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Ui.Brush(Ui.C.Window);
            Foreground = Ui.Brush(Ui.C.Text);
            CanResize = true;

            var content = new StackPanel { Spacing = 12 };
            var heading = Ui.SectionTitle(title);
            heading.FontSize = 17;
            content.Children.Add(heading);
            content.Children.Add(new Border
            {
                Height = 1,
                Background = Ui.Brush(Ui.C.BorderSubtle),
                Margin = new Thickness(0, 0, 0, 2),
            });

            var message = new StackPanel { Spacing = 8 };
            foreach (string line in lines)
            {
                message.Children.Add(new TextBlock
                {
                    Text = line,
                    Foreground = Ui.Brush(Ui.C.Muted),
                    FontFamily = new FontFamily("Inter"),
                    FontSize = 12.5,
                    TextWrapping = TextWrapping.Wrap,
                });
            }
            var messageSurface = Ui.Surface(message);
            content.Children.Add(new ScrollViewer
            {
                Content = messageSurface,
                MaxHeight = 180,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            });

            var yes = Ui.Btn(yesText, Ui.BtnKind.Primary);
            yes.MinWidth = 100;
            yes.Click += (_, _) => Close(true);
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Margin = new Thickness(0, 4, 0, 0),
            };
            if (!string.IsNullOrEmpty(noText))
            {
                var no = Ui.Btn(noText, Ui.BtnKind.Secondary);
                no.MinWidth = 90;
                no.Click += (_, _) => Close(false);
                buttons.Children.Add(no);
            }
            buttons.Children.Add(yes);
            content.Children.Add(buttons);

            Content = new Border
            {
                Padding = new Thickness(20),
                Child = content,
            };
        }
    }

    /// <summary>Text entry dialog used by profile import and other existing flows.</summary>
    public class InputDialog : Window
    {
        public string ResultText { get; private set; } = string.Empty;
        private readonly TextBox _box;

        public InputDialog(string title, string watermark)
        {
            Title = title;
            Width = 540;
            Height = 190;
            MinWidth = 420;
            MinHeight = 160;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Background = Ui.Brush(Ui.C.Window);
            Foreground = Ui.Brush(Ui.C.Text);

            _box = new TextBox { Watermark = watermark, MinHeight = Ui.ButtonMinHeight };
            Ui.StyleInput(_box);
            var ok = Ui.Btn(Loc.T("yes"), Ui.BtnKind.Primary);
            ok.MinWidth = 90;
            ok.Click += (_, _) => { ResultText = _box.Text ?? string.Empty; Close(true); };
            var cancel = Ui.Btn(Loc.T("no"), Ui.BtnKind.Secondary);
            cancel.MinWidth = 90;
            cancel.Click += (_, _) => Close(false);

            var panel = new StackPanel { Spacing = 12 };
            var heading = Ui.SectionTitle(title);
            heading.FontSize = 16;
            panel.Children.Add(heading);
            panel.Children.Add(_box);
            panel.Children.Add(new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Children = { cancel, ok },
            });
            Content = new Border
            {
                Padding = new Thickness(20),
                Child = panel,
            };
        }
    }
}