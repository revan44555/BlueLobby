using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using BlueLobby.Core;
using BlueLobby.Platform;

namespace BlueLobby
{
    public partial class App : Application
    {
        public static IPlatformServices Services { get; private set; } = null!;
        public static PatchManifestStore Store { get; private set; } = null!;
        public static PatchEngine Engine { get; private set; } = null!;
        public static ProfileStore Profiles { get; private set; } = null!;
        public static CompatDatabase Compat { get; } = new();

        public override void Initialize()
        {
            // XAML derleme paketi (Avalonia.NET.Sdk) gerektirmeyen tema kurulumu
            Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
            RequestedThemeVariant = ThemeVariant.Dark;

            Services = PlatformServiceFactory.Create();
            Store = PatchManifestStore.CreateDefault(Services.Paths);
            Engine = new PatchEngine(Services);
            Profiles = new ProfileStore(Services.Paths.LocalDataDir);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
