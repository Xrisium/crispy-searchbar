using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Interactivity;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.Input;
using CrispySearchbar.ViewModels;

namespace CrispySearchbar;

public partial class MainWindow : Window
{
    private const int ModeWheelHoldDelayMs = 200;
    private const int DeactivationGraceMs = 250;

    private readonly DispatcherTimer _modeKeyHoldTimer;
    private ShortcutCatalog _shortcuts = ShortcutCatalog.Default;
    private bool _allowClose;
    private bool _hideOnEscape = true;
    private bool _modeKeyDown;
    private bool _modeWheelOpened;
    private bool _suppressModeKeyRelease;
    private DateTime _lastActivatedUtc = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();

        _modeKeyHoldTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(ModeWheelHoldDelayMs),
        };
        _modeKeyHoldTimer.Tick += OnModeKeyHoldTimerTick;

        // 键盘事件用隧道方式拦截，保证模式切换键不会先移动焦点。
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
        AddHandler(PointerPressedEvent, OnWindowPointerPressed, RoutingStrategies.Tunnel);
        Closing += OnClosing;
        Activated += OnActivated;
        Deactivated += OnDeactivated;
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    public void AllowClose() => _allowClose = true;

    /// <summary>设置保存后由 App 下发最新键位与 Esc 行为；未完成的长按状态一并取消。</summary>
    public void ApplyConfiguration(ShortcutCatalog shortcuts, bool hideOnEscape)
    {
        ArgumentNullException.ThrowIfNull(shortcuts);

        if (!ReferenceEquals(_shortcuts, shortcuts))
        {
            _shortcuts = shortcuts;
        }

        _hideOnEscape = hideOnEscape;
        CancelModeKeyHold();
    }

    /// <summary>隐藏到托盘；应用保持运行，等待全局快捷键或托盘菜单唤回。</summary>
    public void HideToTray()
    {
        CancelModeKeyHold();
        ViewModel.CancelModeWheel();
        ViewModel.OnWindowHidden();
        DictionaryPopup.IsOpen = false;
        Hide();
    }

    /// <summary>从托盘/全局快捷键唤出：显示、恢复、激活并聚焦输入框。</summary>
    public void ShowFromTray()
    {
        if (!IsVisible)
        {
            Show();
        }

        if (WindowState == WindowState.Minimized)
        {
            WindowState = WindowState.Normal;
        }

        Activate();
        QueryBox.Focus();
        QueryBox.CaretIndex = QueryBox.Text?.Length ?? 0;
        ViewModel.OnWindowShown();
        DictionaryPopup.IsOpen = ViewModel.IsDictionaryPopupOpen;
    }

    private void OnActivated(object? sender, EventArgs e) => _lastActivatedUtc = DateTime.UtcNow;

    /// <summary>鼠标点击其它窗口/桌面使搜索框失焦时，按“点击外部即隐藏”收起。</summary>
    private void OnDeactivated(object? sender, EventArgs e)
    {
        // 短暂宽限期避免窗口刚 Show 时被其它前台窗口抢焦点导致立即隐藏。
        if (IsVisible
            && _lastActivatedUtc != DateTime.MinValue
            && DateTime.UtcNow - _lastActivatedUtc > TimeSpan.FromMilliseconds(DeactivationGraceMs))
        {
            HideToTray();
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        // 托盘常驻应用：关闭请求（Alt+F4 等）一律转为隐藏。
        e.Cancel = true;
        CancelModeKeyHold();
        ViewModel.CancelModeWheel();
        ViewModel.OnWindowHidden();
        DictionaryPopup.IsOpen = false;
        Hide();
    }

    private void OnDictionaryCandidatePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: DictionaryCandidateViewModel candidate }
            && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ViewModel.OpenDictionaryCandidate(candidate);
            e.Handled = true;
        }
    }

    private void OnDictionaryPopupPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 鼠标只是候补：点击浮层后立即把输入焦点还给搜索框。
        Dispatcher.UIThread.Post(() => QueryBox.Focus(), DispatcherPriority.Input);
    }

    private void OnModeKeyHoldTimerTick(object? sender, EventArgs e)
    {
        _modeKeyHoldTimer.Stop();
        if (!_modeKeyDown || _suppressModeKeyRelease)
        {
            return;
        }

        ViewModel.OpenModeWheel();
        _modeWheelOpened = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && e.KeyModifiers == KeyModifiers.None)
        {
            if (ViewModel.IsModeWheelOpen)
            {
                CancelModeWheelAndSuppressModeKeyRelease();
                e.Handled = true;
                return;
            }

            if (_hideOnEscape)
            {
                HideToTray();
                e.Handled = true;
            }

            return;
        }

        if (IsModeSwitchKey(e.Key, e.KeyModifiers))
        {
            OnModeSwitchKeyDown(e);
            return;
        }

        if (ShortcutInputMapper.TryCreate(e.Key, e.KeyModifiers, out var pressed)
            && _shortcuts.FindAction(pressed) is { } action)
        {
            HandleShortcutAction(action, e);
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (!_modeKeyDown || !IsModeSwitchKey(e.Key, e.KeyModifiers))
        {
            return;
        }

        _modeKeyHoldTimer.Stop();
        e.Handled = true;

        var opened = _modeWheelOpened;
        var suppress = _suppressModeKeyRelease;
        _modeKeyDown = false;
        _modeWheelOpened = false;
        _suppressModeKeyRelease = false;

        if (suppress)
        {
            return;
        }

        if (opened)
        {
            ViewModel.CommitModeWheel();
        }
        else
        {
            ViewModel.CycleMode();
        }
    }

    private void OnModeSwitchKeyDown(KeyEventArgs e)
    {
        e.Handled = true;
        if (_modeKeyDown)
        {
            // 按住期间的系统自动重复：轮盘打开后忽略，未到阈值时保持计时。
            return;
        }

        _modeKeyDown = true;
        _modeWheelOpened = false;
        _suppressModeKeyRelease = false;
        _modeKeyHoldTimer.Stop();
        _modeKeyHoldTimer.Start();
    }

    private void HandleShortcutAction(ShortcutAction action, RoutedEventArgs e)
    {
        if (ViewModel.IsModeWheelOpen)
        {
            HandleModeWheelAction(action, e);
            return;
        }

        switch (action)
        {
            case ShortcutAction.Execute:
                if (ViewModel.ExecuteCurrent())
                {
                    // 搜索执行成功后自动隐藏，等待全局快捷键/托盘再次唤出。
                    HideToTray();
                }

                e.Handled = true;
                break;
            case ShortcutAction.SelectPrevious when ViewModel.IsDictionaryMode:
                ViewModel.MoveDictionarySelection(-1);
                e.Handled = true;
                break;
            case ShortcutAction.SelectNext when ViewModel.IsDictionaryMode:
                ViewModel.MoveDictionarySelection(1);
                e.Handled = true;
                break;
        }
    }

    private void HandleModeWheelAction(ShortcutAction action, RoutedEventArgs e)
    {
        switch (action)
        {
            case ShortcutAction.SelectPrevious:
                ViewModel.MoveModeWheelSelection(-1);
                break;
            case ShortcutAction.SelectNext:
                ViewModel.MoveModeWheelSelection(1);
                break;
            case ShortcutAction.Execute:
                ViewModel.CommitModeWheel();
                _modeWheelOpened = false;
                _suppressModeKeyRelease = _modeKeyDown;
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private bool IsModeSwitchKey(Key key, KeyModifiers modifiers)
        => modifiers == KeyModifiers.None
            && _shortcuts.TryGetBinding(ShortcutAction.CycleMode, out var binding)
            && binding.Modifiers == ShortcutModifiers.None
            && string.Equals(
                binding.Key,
                ShortcutInputMapper.ToKeyToken(key),
                StringComparison.Ordinal);

    private void CancelModeKeyHold()
    {
        _modeKeyHoldTimer.Stop();
        _modeKeyDown = false;
        _modeWheelOpened = false;
        _suppressModeKeyRelease = false;
    }

    private void CancelModeWheelAndSuppressModeKeyRelease()
    {
        ViewModel.CancelModeWheel();
        _modeWheelOpened = false;
        _suppressModeKeyRelease = _modeKeyDown;
    }

    private void OnModeWheelItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: SearchMode mode }
            && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ViewModel.CommitModeWheel(mode);
            _modeWheelOpened = false;
            _suppressModeKeyRelease = _modeKeyDown;
            e.Handled = true;
        }
    }

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(this).Properties;
        if (!properties.IsXButton1Pressed && !properties.IsXButton2Pressed)
        {
            return;
        }

        if (ShortcutInputMapper.TryCreatePointer(e.KeyModifiers, properties, out var pressed)
            && _shortcuts.FindAction(pressed) is { } action
            && action != ShortcutAction.ToggleVisibility)
        {
            // 全局呼出/隐藏由系统热键处理；框内动作在这里直接分发。
            HandleShortcutAction(action, e);
        }
    }

    private void OnCapsulePointerWheelChanged(object? sender, PointerWheelEventArgs e) => OnModeWheelScrolled(e);

    private void OnModeWheelPointerWheelChanged(object? sender, PointerWheelEventArgs e) => OnModeWheelScrolled(e);

    private void OnModeWheelScrolled(PointerWheelEventArgs e)
    {
        if (!ViewModel.IsModeWheelOpen)
        {
            // 模式键按住期间滚动：立即呼出轮盘，并把这次滚动用于移动高亮。
            if (_modeKeyDown && !_suppressModeKeyRelease)
            {
                _modeKeyHoldTimer.Stop();
                ViewModel.OpenModeWheel();
                _modeWheelOpened = true;
            }
            else
            {
                return;
            }
        }

        ViewModel.MoveModeWheelSelection(e.Delta.Y > 0 ? -1 : 1);
        e.Handled = true;
    }

    private void OnCapsulePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 点击胶囊任意位置（含右侧模式标签）都把输入焦点还给输入框。
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            QueryBox.Focus();
        }
    }
}