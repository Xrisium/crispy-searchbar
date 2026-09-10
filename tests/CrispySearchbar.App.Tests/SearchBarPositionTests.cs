using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// 主窗口定位：默认在主显示器工作区居中，配置的偏移按缩放换算后生效，越界夹回工作区内，
/// 落到屏幕下半部时浮层改为向上弹出。
/// </summary>
public sealed class SearchBarPositionTests
{
    [AvaloniaFact]
    public async Task SearchBarOffset_MovesWindowRelativeToCenteredDefault()
    {
        TestStyles.Ensure();
        var window = CreateMainWindow();
        window.Show();
        await PumpAsync();

        var screen = window.Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var workArea = screen.WorkingArea;
        var size = WindowPixelSize(window, screen.Scaling);
        var centered = CenteredIn(workArea, size);

        window.ApplyConfiguration(
            ShortcutCatalog.Default,
            hideOnEscape: true,
            ScreenOffset.Default);
        Assert.Equal(centered, window.Position);

        const int offsetX = 60;
        const int offsetY = 40;
        var target = new PixelPoint(
            centered.X + (int)Math.Round(offsetX * screen.Scaling),
            centered.Y + (int)Math.Round(offsetY * screen.Scaling));

        window.ApplyConfiguration(
            ShortcutCatalog.Default,
            hideOnEscape: true,
            new ScreenOffset(offsetX, offsetY));
        Assert.Equal(
            FitsInside(workArea, size, target) ? target : BottomRightOf(workArea, size),
            window.Position);

        window.ApplyConfiguration(
            ShortcutCatalog.Default,
            hideOnEscape: true,
            ScreenOffset.Default);
        Assert.Equal(centered, window.Position);
    }

    [AvaloniaFact]
    public async Task OutOfScreenOffset_ClampsToWorkAreaAndFlipsOverlays()
    {
        TestStyles.Ensure();
        var window = CreateMainWindow();
        window.Show();
        await PumpAsync();

        var screen = window.Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var workArea = screen.WorkingArea;
        var size = WindowPixelSize(window, screen.Scaling);
        if (!FitsInside(workArea, size, new PixelPoint(workArea.X, workArea.Y)))
        {
            // 屏幕比搜索框还小时夹取退化为贴左上角，无法验证四角行为。
            return;
        }

        window.ApplyConfiguration(
            ShortcutCatalog.Default,
            hideOnEscape: true,
            new ScreenOffset(5000, 5000));
        Assert.Equal(BottomRightOf(workArea, size), window.Position);
        AssertOverlayPlacement(window, PlacementMode.Top);

        window.ApplyConfiguration(
            ShortcutCatalog.Default,
            hideOnEscape: true,
            new ScreenOffset(-5000, -5000));
        Assert.Equal(new PixelPoint(workArea.X, workArea.Y), window.Position);
        AssertOverlayPlacement(window, PlacementMode.Bottom);
    }

    /// <summary>启动顺序：配置在窗口首次显示之前下发，首次显示也必须落到配置的位置。</summary>
    [AvaloniaFact]
    public async Task ConfigurationBeforeFirstShow_IsAppliedOnShow()
    {
        TestStyles.Ensure();
        var window = CreateMainWindow();
        window.ApplyConfiguration(
            ShortcutCatalog.Default,
            hideOnEscape: true,
            new ScreenOffset(70, 30));

        window.ShowFromTray();
        await PumpAsync();

        var screen = window.Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var workArea = screen.WorkingArea;
        var size = WindowPixelSize(window, screen.Scaling);
        var expected = new PixelPoint(
            CenteredIn(workArea, size).X + (int)Math.Round(70 * screen.Scaling),
            CenteredIn(workArea, size).Y + (int)Math.Round(30 * screen.Scaling));

        Assert.Equal(
            FitsInside(workArea, size, expected) ? expected : BottomRightOf(workArea, size),
            window.Position);
    }

    private static PixelSize WindowPixelSize(Window window, double scaling)
        => new(
            (int)Math.Round(window.Width * scaling),
            (int)Math.Round(window.Height * scaling));

    private static PixelPoint CenteredIn(PixelRect workArea, PixelSize size)
        => new(
            workArea.X + (workArea.Width - size.Width) / 2,
            workArea.Y + (workArea.Height - size.Height) / 2);

    private static PixelPoint BottomRightOf(PixelRect workArea, PixelSize size)
        => new(workArea.Right - size.Width, workArea.Bottom - size.Height);

    private static bool FitsInside(PixelRect workArea, PixelSize size, PixelPoint point)
        => point.X >= workArea.X
            && point.Y >= workArea.Y
            && point.X + size.Width <= workArea.Right
            && point.Y + size.Height <= workArea.Bottom;

    private static void AssertOverlayPlacement(Window window, PlacementMode expected)
    {
        var popups = window
            .GetVisualDescendants()
            .OfType<Popup>()
            .Where(popup => popup.Name is "DictionaryPopup" or "ModeWheelPopup")
            .ToList();

        Assert.Equal(2, popups.Count);
        Assert.All(popups, popup => Assert.Equal(expected, popup.Placement));
    }

    private static MainWindow CreateMainWindow()
    {
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.English);
        var viewModel = new MainWindowViewModel(
            strings,
            [SearchMode.WebSearch(strings.WebSearchMode, "https://example.com/?q={0}")],
            _ => { },
            dictionaryEmptyHint: strings.DictionaryEmptyHint);
        return new MainWindow { DataContext = viewModel };
    }

    private static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.UIThread.InvokeAsync(() => { });
    }
}
