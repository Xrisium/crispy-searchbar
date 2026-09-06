using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CrispySearchbar.ViewModels;

namespace CrispySearchbar;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // 键盘事件用隧道方式拦截，保证 Tab 不会先移动焦点。
        AddHandler(KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    private void OnOpened(object? sender, EventArgs e)
    {
        QueryBox.Focus();
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
            // 开发版：Esc 直接退出。接入全局快捷键与托盘常驻后改为隐藏窗口。
            Close();
            e.Handled = true;
        }
        else if (e.Key == Key.Enter)
        {
            ViewModel.ExecuteCurrent();
            e.Handled = true;
        }
    }
}

