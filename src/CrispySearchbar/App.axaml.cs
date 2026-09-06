using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.ViewModels;

namespace CrispySearchbar;

public partial class App : Application
{
    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = AppSettingsStore.LoadOrDefault();
            RequestedThemeVariant = settings.Theme switch
            {
                ThemePreference.Light => ThemeVariant.Light,
                ThemePreference.Dark => ThemeVariant.Dark,
                _ => ThemeVariant.Default,
            };

            var modes = SearchModeCatalog.CreateDefault(settings);
            var viewModel = new MainWindowViewModel(modes, BrowserLauncher.Open);
            desktop.MainWindow = new MainWindow { DataContext = viewModel };
        }

        base.OnFrameworkInitializationCompleted();
    }
}

