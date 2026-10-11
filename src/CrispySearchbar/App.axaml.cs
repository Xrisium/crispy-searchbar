using System.Diagnostics;
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
using CrispySearchbar.Core.Emoji;
using CrispySearchbar.Emoji;
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
    private EmojiHotkeyService? _emojiHotkey;
    private EmojiModeViewModel? _emojiViewModel;
    private IGlobalMouseWheelService? _mouseWheelService;
    private TrayIconService? _trayIconService;
    private IStartupRegistrationService? _startupRegistrationService;
    private IFullscreenAppDetector? _fullscreenAppDetector;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _settings = AppSettingsStore.LoadOrDefault();
            _strings = TranslationCatalog.Default.Resolve(_settings.Language);
            ApplyTheme(_settings.Theme);

            _startupRegistrationService = StartupRegistrationServiceFactory.Create();
            // 便携版可能被移动过：启用自启时自愈注册项路径，禁用时清理注册项。
            if (!_startupRegistrationService.TryEnsureCurrentPath(_settings.LaunchAtStartup))
            {
                Trace.TraceWarning("启动时同步开机自启设置失败。");
            }

            _fullscreenAppDetector = FullscreenAppDetectorFactory.Create();
            var shortcuts = ShortcutCatalog.Create(_settings);
            var modes = SearchModeCatalog.Create(_settings, _strings);
            // 启动即后台加载两套词典资源；仅在配置里的词典路径改变时重建加载任务。
            _dictionaryResources = new AppDictionaryResources(
                _strings,
                _settings.DictionaryFilePath,
                _settings.EcdictFilePath);
            _dictionaryLoadTask = _dictionaryResources.LoadAsync(CancellationToken.None);
            _emojiViewModel = new EmojiModeViewModel(_strings, _settings,
                Task.Run(EmojiResources.LoadIndex), new EmojiHistoryStore(), new EmojiImageCache());
            _viewModel = new MainWindowViewModel(
                _strings,
                modes,
                BrowserLauncher.Open,
                clearQueryOnHide: _settings.ClearQueryOnHide,
                dictionaryEmptyHint: ShortcutTextFormatter.Format(
                    _strings.DictionaryEmptyHint,
                    shortcuts,
                    _strings.SettingsTexts.ShortcutUnsetText),
                loadDictionary: () => _dictionaryLoadTask, emoji: _emojiViewModel);
            var mainWindow = new MainWindow { DataContext = _viewModel };
            _mainWindow = mainWindow;
            _mouseWheelService = GlobalMouseWheelServiceFactory.Create();
            mainWindow.AttachModeWheelCapture(_mouseWheelService);
            mainWindow.ApplyConfiguration(
                shortcuts,
                _settings.HideOnEscape,
                _settings.SearchBarOffset);

            // 托盘常驻：没有窗口时也不退出，退出由托盘菜单显式触发。
            desktop.MainWindow = mainWindow;
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            _hotkeyService = GlobalHotkeyServiceFactory.Create();
            _hotkeyService.Pressed += ToggleSearchBarFromHotkey;
            if (shortcuts.TryGetBinding(ShortcutAction.ToggleVisibility, out var initialGlobal))
            {
                _hotkeyService.TryStart(initialGlobal);
            }

            _emojiHotkey = new EmojiHotkeyService(() => _settings.SkipWhenFullscreenAppActive
                && _fullscreenAppDetector?.IsFullscreenAppActive() == true);
            _emojiHotkey.Pressed += ToggleEmoji;
            ApplyEmojiHotkey(shortcuts);

            _trayIconService = new TrayIconService(
                _strings,
                ToggleSearchBarFromTray,
                OpenSettingsWindow,
                ExitApplication);

            // 开机自启只驻留托盘；普通启动仍直接显示并聚焦输入框。
            if (!IsAutostartLaunch())
            {
                mainWindow.ShowFromTray();
            }
            if (AppSettingsStore.LoadWarnings.Count > 0)
                OpenSettingsWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 设置窗口保存成功后从磁盘重读 settings.json，并让运行中的应用组件按新文件刷新。
    /// 配置文件始终是唯一权威来源，运行中的组件只消费它的快照。
    /// 全局热键注册失败不阻断应用：运行中的组件照常刷新，失败键位由设置界面黄色提醒。
    /// </summary>
    private void ApplyConfiguration(AppSettings settings)
    {
        var shortcuts = ShortcutCatalog.Create(settings);
        _hotkeyService?.TryApply(shortcuts.TryGetBinding(
            ShortcutAction.ToggleVisibility,
            out var globalBinding)
            ? globalBinding
            : null);

        var dictionaryPathsChanged =
            !SameNormalizedPath(_settings.DictionaryFilePath, settings.DictionaryFilePath)
            || !SameNormalizedPath(_settings.EcdictFilePath, settings.EcdictFilePath);

        _settings = settings;
        _strings = TranslationCatalog.Default.Resolve(settings.Language);
        _emojiViewModel?.ApplyConfiguration(_strings, settings);
        ApplyEmojiHotkey(shortcuts);
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

        var modes = SearchModeCatalog.Create(settings, _strings);
        var dictionaryEmptyHint = ShortcutTextFormatter.Format(
            _strings.DictionaryEmptyHint,
            shortcuts,
            _strings.SettingsTexts.ShortcutUnsetText);
        _viewModel?.ApplyConfiguration(
            _strings,
            modes,
            clearQueryOnHide: settings.ClearQueryOnHide,
            dictionaryEmptyHint: dictionaryEmptyHint);
        _mainWindow?.ApplyConfiguration(
            shortcuts,
            settings.HideOnEscape,
            settings.SearchBarOffset);
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

        var probe = GlobalHotkeyProbeFactory.Create();
        var window = new SettingsWindow(
            candidate => ShortcutParser.TryParse(candidate, out var parsed) && EmojiHotkeyService.IsShellEmojiBinding(parsed)
                ? EmojiHotkeyService.CanInstallShellHook() : probe.CanRegister(candidate),
            candidate => (_emojiHotkey?.IsRegistered == true && string.Equals(_emojiHotkey.CurrentBinding?.ToStorageString(), candidate, StringComparison.OrdinalIgnoreCase))
                || _hotkeyService?.IsRegistered == true
                && string.Equals(
                    _hotkeyService.CurrentBinding?.ToStorageString(),
                    candidate,
                    StringComparison.OrdinalIgnoreCase),
            enabled => _startupRegistrationService?.TrySetEnabled(enabled) ?? true);
        window.SettingsSaved += OnSettingsSaved;
        window.Closed += (_, _) =>
        {
            _settingsWindow = null;
            probe.Dispose();
        };
        _settingsWindow = window;
        window.Show();
    }

    private void OnSettingsSaved(object? sender, AppSettings saved)
    {
        var fromDisk = AppSettingsStore.LoadOrDefault();
        ApplyConfiguration(fromDisk);
        _settingsWindow?.ReloadFromConfiguration(_settings, _strings);
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

    private static bool IsAutostartLaunch()
        => Environment.GetCommandLineArgs()
            .Skip(1)
            .Any(argument => string.Equals(
                argument,
                StartupRegistrationCommand.AutostartArgument,
                StringComparison.OrdinalIgnoreCase));

    private void ApplyEmojiHotkey(ShortcutCatalog shortcuts)
    {
        if (_emojiHotkey is null)
            return;
        var enabled = ModePreferenceNormalizer.Normalize(_settings.ModePreferences)
            .Any(p => p.Key == ModePreferenceDefaults.Emoji && p.Enabled);
        var binding = enabled && shortcuts.TryGetBinding(ShortcutAction.ShowEmoji, out var value) ? value : null;
        if (!_emojiHotkey.TryApply(binding))
            _viewModel?.SetApplicationWarning(_strings.Emoji.HotkeyFailed);
        else
            _viewModel?.SetApplicationWarning("");
    }
    private void ToggleEmoji()
    {
        if (_mainWindow is null || _viewModel is null)
            return;
        if (_mainWindow.IsVisible && _viewModel.IsEmojiMode)
        {
            _mainWindow.HideToTray();
            return;
        }
        if (!_viewModel.SelectMode(ModePreferenceDefaults.Emoji))
            return;
        _mainWindow.ShowFromTray();
    }

    private void ToggleSearchBarFromHotkey()
        => ToggleSearchBar(
            suppressWhenFullscreen: _settings.SkipWhenFullscreenAppActive);

    private void ToggleSearchBarFromTray()
        => ToggleSearchBar(suppressWhenFullscreen: false);

    private void ToggleSearchBar(bool suppressWhenFullscreen)
    {
        if (_mainWindow is null)
        {
            return;
        }

        if (_mainWindow.IsVisible && _mainWindow.WindowState != WindowState.Minimized)
        {
            _mainWindow.HideToTray();
            return;
        }

        if (suppressWhenFullscreen
            && _fullscreenAppDetector?.IsFullscreenAppActive() == true)
        {
            return;
        }

        _mainWindow.ShowFromTray();
    }

    private void ExitApplication()
    {
        _emojiHotkey?.Dispose();
        _emojiViewModel?.Dispose();
        if (_mouseWheelService is not null)
        {
            _mouseWheelService.Dispose();
            _mouseWheelService = null;
        }

        if (_hotkeyService is not null)
        {
            _hotkeyService.Pressed -= ToggleSearchBarFromHotkey;
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
