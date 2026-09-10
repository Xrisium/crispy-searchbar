using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Layout;
using Avalonia.VisualTree;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// 回归测试：全应用共用同一套纤细圆角滚动条样式（设置面板与候选框一致的界面规范）。
/// </summary>
public sealed class ScrollbarStyleTests
{
    [AvaloniaFact]
    public void SharedStyleProvidesSlimVerticalScrollBar()
    {
        TestStyles.Ensure();
        var scrollBar = new ScrollBar { Orientation = Orientation.Vertical };
        var window = new Window { Content = scrollBar, Width = 200, Height = 200 };
        window.Show();

        Assert.Equal(10, scrollBar.Width);
        var thumb = scrollBar
            .GetVisualDescendants()
            .OfType<Thumb>()
            .FirstOrDefault();
        Assert.NotNull(thumb);
        Assert.Equal(6, thumb!.Width);
        Assert.Contains("slimScrollThumb", thumb.Classes);
    }

}
