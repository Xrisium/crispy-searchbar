using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.Input;
using Avalonia.Interactivity;
using CrispySearchbar.ViewModels;

namespace CrispySearchbar;

public partial class MainWindow : Window
{
    private bool _allowClose;

    public MainWindow()
    {
        InitializeComponent();

        // 键盘事件用隧道方式拦截，保证 Tab 不会先移动焦点。
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        Closing += OnClosing;
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    public void AllowClose() => _allowClose = true;

    /// <summary>隐藏到托盘；应用保持运行，等待全局快捷键或托盘菜单唤回。</summary>
    public void HideToTray()
    {
        ViewModel.OnWindowHidden();
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
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose)
        {
            return;
        }

        // 托盘常驻应用：关闭请求（Alt+F4 等）一律转为隐藏。
        e.Cancel = true;
        ViewModel.OnWindowHidden();
        Hide();
    }


    private void OnDictionaryPopupPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // 鼠标只是候补：点击浮层后立即把输入焦点还给搜索框。
        Dispatcher.UIThread.Post(() => QueryBox.Focus(), DispatcherPriority.Input);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Tab)
        {
            ViewModel.CycleMode();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape)
        {
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
}

