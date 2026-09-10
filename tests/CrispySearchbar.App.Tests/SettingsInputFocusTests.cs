using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// 设置面板输入框的失焦行为：按下 Enter，或点击面板空白/非交互区域，输入框立即结束编辑态。
/// </summary>
public sealed class SettingsInputFocusTests
{
    [AvaloniaFact]
    public async Task Enter_ReleasesInputFocusAndNormalizesOffsetText()
    {
        TestStyles.Ensure();
        var window = new SettingsWindow();
        window.Show();
        await PumpAsync();

        var offsetField = FindOffsetField(window);
        var offsetInput = window
            .GetVisualDescendants()
            .OfType<TextBox>()
            .First(box => box.DataContext is OffsetSettingFieldViewModel);
        offsetField.XText = "abc";
        offsetInput.Focus();
        await PumpAsync();
        Assert.True(offsetInput.IsFocused);

        window.KeyPress(Key.Enter, RawInputModifiers.None, PhysicalKey.Enter, "\r");
        await PumpAsync();

        Assert.False(offsetInput.IsFocused);
        Assert.Equal("0", offsetInput.Text);
        Assert.Equal(0, offsetField.X);
    }

    [AvaloniaFact]
    public async Task ClickingBlankArea_ReleasesInputFocus()
    {
        TestStyles.Ensure();
        var window = new SettingsWindow();
        window.Show();
        await PumpAsync();

        var input = window
            .GetVisualDescendants()
            .OfType<TextBox>()
            .First(box => box.DataContext is TextSettingFieldViewModel);
        input.Focus();
        await PumpAsync();
        Assert.True(input.IsFocused);

        // 分类标题属于面板上的非交互区域：点击后输入框应立即失焦。
        var header = window
            .GetVisualDescendants()
            .OfType<TextBlock>()
            .First(block => block.DataContext is SettingsSectionViewModel);
        var point = header.TranslatePoint(
            new Point(header.Bounds.Width / 2, header.Bounds.Height / 2),
            window);
        Assert.NotNull(point);

        window.MouseDown(point!.Value, MouseButton.Left, RawInputModifiers.None);
        window.MouseUp(point.Value, MouseButton.Left, RawInputModifiers.None);
        await PumpAsync();

        Assert.False(input.IsFocused);
    }

    private static OffsetSettingFieldViewModel FindOffsetField(Window window)
        => window
            .GetVisualDescendants()
            .Select(visual => visual.DataContext)
            .OfType<OffsetSettingFieldViewModel>()
            .First();

    private static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.UIThread.InvokeAsync(() => { });
    }
}
