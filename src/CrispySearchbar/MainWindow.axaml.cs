using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Interactivity;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.ViewModels;

namespace CrispySearchbar;

public partial class MainWindow : Window
{
    private const int ModeWheelHoldDelayMs = 200;
    private const int DeactivationGraceMs = 250;

    private readonly DispatcherTimer _tabHoldTimer;
    private bool _allowClose;
    private bool _tabDown;
    private bool _modeWheelOpened;
    private bool _suppressTabRelease;
    private DateTime _lastActivatedUtc = DateTime.MinValue;

    public MainWindow()
    {
        InitializeComponent();

        _tabHoldTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(ModeWheelHoldDelayMs),
        };
        _tabHoldTimer.Tick += OnTabHoldTimerTick;

        // 键盘事件用隧道方式拦截，保证 Tab 不会先移动焦点。
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnKeyUp, RoutingStrategies.Tunnel);
        Closing += OnClosing;
        Activated += OnActivated;
        Deactivated += OnDeactivated;
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    public void AllowClose() => _allowClose = true;

    /// <summary>隐藏到托盘；应用保持运行，等待全局快捷键或托盘菜单唤回。</summary>
    public void HideToTray()
    {
        CancelTabHold();
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
        CancelTabHold();
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

    private void OnTabHoldTimerTick(object? sender, EventArgs e)
    {
        _tabHoldTimer.Stop();
        if (!_tabDown || _suppressTabRelease)
        {
            return;
        }

        ViewModel.OpenModeWheel();
        _modeWheelOpened = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            OnTabKeyDown(e);
            return;
        }

        if (ViewModel.IsModeWheelOpen)
        {
            OnModeWheelKeyDown(e);
            return;
        }

        if (e.Key == Key.Escape)
        {
            CancelTabHold();
            HideToTray();
            e.Handled = true;
        }
        else if (e.Key is Key.Up or Key.Down && ViewModel.IsDictionaryMode)
        {
            ViewModel.MoveDictionarySelection(e.Key == Key.Down ? 1 : -1);
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            if (ViewModel.ExecuteCurrent())
            {
                // 搜索执行成功后自动隐藏，等待 Alt+Space/托盘再次唤出。
                HideToTray();
            }

            e.Handled = true;
        }
    }

    private void OnKeyUp(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Tab || !_tabDown)
        {
            return;
        }

        _tabHoldTimer.Stop();
        e.Handled = true;

        var opened = _modeWheelOpened;
        var suppress = _suppressTabRelease;
        _tabDown = false;
        _modeWheelOpened = false;
        _suppressTabRelease = false;

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

    private void OnTabKeyDown(KeyEventArgs e)
    {
        e.Handled = true;
        if (_tabDown)
        {
            // 按住期间的系统自动重复：轮盘打开后忽略，未到阈值时保持计时。
            return;
        }

        _tabDown = true;
        _modeWheelOpened = false;
        _suppressTabRelease = false;
        _tabHoldTimer.Stop();
        _tabHoldTimer.Start();
    }

    private void OnModeWheelKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Up:
                ViewModel.MoveModeWheelSelection(-1);
                break;
            case Key.Down:
                ViewModel.MoveModeWheelSelection(1);
                break;
            case Key.Enter:
                ViewModel.CommitModeWheel();
                _modeWheelOpened = false;
                _suppressTabRelease = _tabDown;
                break;
            case Key.Escape:
                ViewModel.CancelModeWheel();
                _modeWheelOpened = false;
                _suppressTabRelease = _tabDown;
                break;
            default:
                return;
        }

        e.Handled = true;
    }

    private void CancelTabHold()
    {
        _tabHoldTimer.Stop();
        _tabDown = false;
        _modeWheelOpened = false;
        _suppressTabRelease = false;
    }

    private void OnModeWheelItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: SearchMode mode }
            && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            ViewModel.CommitModeWheel(mode);
            _modeWheelOpened = false;
            _suppressTabRelease = _tabDown;
            e.Handled = true;
        }
    }

    private void OnCapsulePointerWheelChanged(object? sender, PointerWheelEventArgs e) => OnModeWheelScrolled(e);

    private void OnModeWheelPointerWheelChanged(object? sender, PointerWheelEventArgs e) => OnModeWheelScrolled(e);

    private void OnModeWheelScrolled(PointerWheelEventArgs e)
    {
        if (!ViewModel.IsModeWheelOpen)
        {
            // Tab 按住期间滚动：立即呼出轮盘，并把这次滚动用于移动高亮。
            if (_tabDown && !_suppressTabRelease)
            {
                _tabHoldTimer.Stop();
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
