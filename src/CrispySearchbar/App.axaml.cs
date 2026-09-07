using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.Dictionary;
using CrispySearchbar.Platform;
using CrispySearchbar.ViewModels;

namespace CrispySearchbar;

public partial class App : Application
{
    private MainWindow? _mainWindow;
    private IGlobalHotkeyService? _hotkeyService;
    private TrayIconService? _trayIconService;

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
            // 启动即后台加载两套词典资源；任务只创建一次，应用退出前不会重新加载。
            var dictionaryResources = new AppDictionaryResources(
                settings.DictionaryFilePath,
                settings.EcdictFilePath);
            var dictionaryLoadTask = dictionaryResources.LoadAsync(CancellationToken.None);
            var viewModel = new MainWindowViewModel(
                modes,
                BrowserLauncher.Open,
                clearQueryOnHide: settings.ClearQueryOnHide,
                loadDictionary: () => dictionaryLoadTask);
            var mainWindow = new MainWindow { DataContext = viewModel };
            _mainWindow = mainWindow;

            // 托盘常驻：没有窗口时也不退出，退出由托盘菜单显式触发。
            desktop.MainWindow = mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _hotkeyService = GlobalHotkeyServiceFactory.Create();
            _hotkeyService.Pressed += ToggleSearchBar;
            _hotkeyService.Start();

            _trayIconService = new TrayIconService(ToggleSearchBar, OpenConfigFile, ExitApplication);

            mainWindow.Show();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OpenConfigFile()
    {
        BrowserLauncher.OpenFileWithDefaultApplication(AppSettingsStore.GetSettingsFilePath());
    }

    private void ToggleSearchBar()
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (_mainWindow.IsVisible && _mainWindow.WindowState != WindowState.Minimized)
        {
            _mainWindow.HideToTray();
        }
        else
        {
            _mainWindow.ShowFromTray();
        }
    }

    private void ExitApplication()
    {
        if (_hotkeyService is not null)
        {
            _hotkeyService.Pressed -= ToggleSearchBar;
            _hotkeyService.Dispose();
            _hotkeyService = null;
        }

        _trayIconService?.Dispose();
        _trayIconService = null;

        _mainWindow?.AllowClose();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}



