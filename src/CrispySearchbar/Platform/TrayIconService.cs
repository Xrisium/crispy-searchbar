using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using CrispySearchbar.Core.Localization;

namespace CrispySearchbar.Platform;

/// <summary>系统托盘图标：单击/菜单显示隐藏，菜单提供打开配置文件与退出。</summary>
public sealed class TrayIconService : IDisposable
{
    private readonly TrayIcon _trayIcon;
    private bool _disposed;

    public TrayIconService(
        AppStrings strings,
        Action toggleWindow,
        Action openConfigFile,
        Action exitApplication)
    {
        ArgumentNullException.ThrowIfNull(strings);

        var menu = new NativeMenu();

        var toggleItem = new NativeMenuItem(strings.ShowHideSearchBar);
        toggleItem.Click += (_, _) => toggleWindow();
        menu.Items.Add(toggleItem);

        var configItem = new NativeMenuItem(strings.OpenConfigFile);
        configItem.Click += (_, _) => openConfigFile();
        menu.Items.Add(configItem);

        menu.Items.Add(new NativeMenuItemSeparator());

        var exitItem = new NativeMenuItem(strings.Exit);
        exitItem.Click += (_, _) => exitApplication();
        menu.Items.Add(exitItem);

        _trayIcon = new TrayIcon
        {
            Icon = LoadIcon(),
            Menu = menu,
            ToolTipText = strings.TrayToolTip,
            IsVisible = true,
        };

        // 单击托盘图标等同于菜单里的“显示 / 隐藏”。
        _trayIcon.Clicked += (_, _) => toggleWindow();

        TrayIcon.SetIcons(Application.Current!, new TrayIcons { _trayIcon });
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _trayIcon.IsVisible = false;
        _trayIcon.Menu = null;
        _trayIcon.Dispose();
    }

    private static WindowIcon LoadIcon()
    {
        var uri = new Uri("avares://CrispySearchbar/Assets/icon.png");
        using var stream = AssetLoader.Open(uri);
        return new WindowIcon(stream);
    }
}
