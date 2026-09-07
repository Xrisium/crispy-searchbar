using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Dictionary;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.Dictionary;
using CrispySearchbar.Platform;
using CrispySearchbar.ViewModels;

namespace CrispySearchbar;

public partial class App : Application
{
    private AppSettings _settings = null!;
    private AppStrings _strings = null!;
    private MainWindow? _mainWindow;
    private MainWindowViewModel? _viewModel;
    private SettingsWindow? _settingsWindow;
    private AppDictionaryResources? _dictionaryResources;
    private Task<DictionaryLoadResult>? _dictionaryLoadTask;
    private IGlobalHotkeyService? _hotkeyService;
    private TrayIconService? _trayIconService;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _settings = AppSettingsStore.LoadOrDefault();
            _strings = AppStrings.For(_settings.Language);
            ApplyTheme(_settings.Theme);

            var modes = SearchModeCatalog.CreateDefault(_settings, _strings);
            // 启动即后台加载两套词典资源；仅在配置里的词典路径改变时重建加载任务。
            _dictionaryResources = new AppDictionaryResources(
                _strings,
                _settings.DictionaryFilePath,
                _settings.EcdictFilePath);
            _dictionaryLoadTask = _dictionaryResources.LoadAsync(CancellationToken.None);
            _viewModel = new MainWindowViewModel(
                _strings,
                modes,
                BrowserLauncher.Open,
                clearQueryOnHide: _settings.ClearQueryOnHide,
                loadDictionary: () => _dictionaryLoadTask);
            var mainWindow = new MainWindow { DataContext = _viewModel };
            _mainWindow = mainWindow;

            // 托盘常驻：没有窗口时也不退出，退出由托盘菜单显式触发。
            desktop.MainWindow = mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _hotkeyService = GlobalHotkeyServiceFactory.Create();
            _hotkeyService.Pressed += ToggleSearchBar;
            _hotkeyService.Start();

            _trayIconService = new TrayIconService(
                _strings,
                ToggleSearchBar,
                OpenSettingsWindow,
                ExitApplication);

            // 首次启动也直接显示并聚焦输入框。
            mainWindow.ShowFromTray();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 设置窗口保存成功后从磁盘重读 settings.json，并让运行中的应用组件按新文件刷新。
    /// 配置文件始终是唯一权威来源，运行中的组件只消费它的快照。
    /// </summary>
    private void ApplyConfiguration(AppSettings settings)
    {
        var dictionaryPathsChanged =
            !SameNormalizedPath(_settings.DictionaryFilePath, settings.DictionaryFilePath)
            || !SameNormalizedPath(_settings.EcdictFilePath, settings.EcdictFilePath);

        _settings = settings;
        _strings = AppStrings.For(settings.Language);
        ApplyTheme(settings.Theme);

        if (dictionaryPathsChanged)
        {
            _dictionaryResources = new AppDictionaryResources(
                _strings,
                settings.DictionaryFilePath,
                settings.EcdictFilePath);
            _dictionaryLoadTask = _dictionaryResources.LoadAsync(CancellationToken.None);
            _viewModel?.ReloadDictionarySource(() => _dictionaryLoadTask);
        }

        var modes = SearchModeCatalog.CreateDefault(settings, _strings);
        _viewModel?.ApplyConfiguration(
            _strings,
            modes,
            clearQueryOnHide: settings.ClearQueryOnHide);
        _trayIconService?.UpdateStrings(_strings);
    }

    private void OpenSettingsWindow()
    {
        if (_settingsWindow is { } existing)
        {
            if (!existing.IsVisible)
            {
                existing.Show();
            }

            existing.Activate();
            return;
        }

        var window = new SettingsWindow();
        window.SettingsSaved += OnSettingsSaved;
        window.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow = window;
        window.Show();
    }

    private void OnSettingsSaved(object? sender, AppSettings saved)
    {
        var fromDisk = AppSettingsStore.LoadOrDefault();
        ApplyConfiguration(fromDisk);
        _settingsWindow?.ReloadFromConfiguration(fromDisk, _strings);
    }

    private static bool SameNormalizedPath(string? left, string? right)
    {
        var normalizedLeft = string.IsNullOrWhiteSpace(left)
            ? null
            : Path.GetFullPath(left);
        var normalizedRight = string.IsNullOrWhiteSpace(right)
            ? null
            : Path.GetFullPath(right);
        return string.Equals(
            normalizedLeft,
            normalizedRight,
            StringComparison.OrdinalIgnoreCase);
    }

    private void ApplyTheme(ThemePreference preference)
    {
        RequestedThemeVariant = preference switch
        {
            ThemePreference.Light => ThemeVariant.Light,
            ThemePreference.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
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

        _settingsWindow?.Close();
        _settingsWindow = null;
        _mainWindow?.AllowClose();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }
}
