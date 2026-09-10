using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// 回归测试：设置面板“外观”分类中的搜索框位置行复用统一的输入框样式，
/// 且“恢复默认位置”按钮始终显示、仅在已居中时置灰不可点。
/// </summary>
public sealed class SearchBarPositionFieldTests
{
    [AvaloniaFact]
    public async Task OffsetRow_ReusesSharedTextBoxStyleAndKeepsResetButtonVisible()
    {
        TestStyles.Ensure();
        var window = new SettingsWindow();
        window.Show();
        await PumpAsync();

        var application = Application.Current!;
        var field = window
            .GetVisualDescendants()
            .Select(visual => visual.DataContext)
            .OfType<OffsetSettingFieldViewModel>()
            .FirstOrDefault();
        Assert.NotNull(field);

        // 位置行的两个输入框与文件路径、网址模板输入框共用同一套样式。
        var inputs = window
            .GetVisualDescendants()
            .OfType<TextBox>()
            .Where(textBox => textBox.DataContext is OffsetSettingFieldViewModel)
            .ToList();
        Assert.Equal(2, inputs.Count);

        var sharedInput = window
            .GetVisualDescendants()
            .OfType<TextBox>()
            .First(textBox => textBox.DataContext is FilePathSettingFieldViewModel);
        Assert.Contains("settingsTextBox", sharedInput.Classes);
        Assert.All(inputs, input =>
        {
            Assert.Contains("settingsTextBox", input.Classes);
            // 解析后的样式必须与通用输入框逐项一致：内边距、圆角、描边、字号。
            Assert.Equal(sharedInput.Padding, input.Padding);
            Assert.Equal(sharedInput.MinHeight, input.MinHeight);
            Assert.Equal(sharedInput.CornerRadius, input.CornerRadius);
            Assert.Equal(sharedInput.BorderThickness, input.BorderThickness);
            Assert.Equal(sharedInput.FontSize, input.FontSize);
            Assert.Equal(
                Assert.IsType<SolidColorBrush>(sharedInput.BorderBrush).Color,
                Assert.IsType<SolidColorBrush>(input.BorderBrush).Color);
            // 输入框自身不叠加系统方形焦点框。
            Assert.Null(input.FocusAdorner);
        });
        Assert.Equal("0", inputs[0].Text);
        Assert.Equal("0", inputs[1].Text);

        // 聚焦时只换描边颜色，圆角与描边宽度保持不变（不出现矩形高亮）。
        inputs[0].Focus();
        await PumpAsync();
        Assert.True(application.TryFindResource(
            "AccentBrush",
            application.ActualThemeVariant,
            out var accent));
        Assert.True(inputs[0].IsFocused);
        Assert.Equal(sharedInput.CornerRadius, inputs[0].CornerRadius);
        Assert.Equal(sharedInput.BorderThickness, inputs[0].BorderThickness);
        Assert.Equal(
            Assert.IsType<SolidColorBrush>(accent).Color,
            Assert.IsType<SolidColorBrush>(inputs[0].BorderBrush).Color);

        var resetButton = window
            .GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(button => button.DataContext is OffsetSettingFieldViewModel);
        Assert.NotNull(resetButton);
        Assert.True(resetButton!.IsVisible);
        Assert.False(resetButton.IsEnabled);
        // 已居中时按钮文字置灰（与占位文本同色），且不再是强调色。
        Assert.True(application.TryFindResource(
            "TextPlaceholderBrush",
            application.ActualThemeVariant,
            out var placeholder));
        Assert.Equal(
            Assert.IsType<SolidColorBrush>(placeholder).Color,
            Assert.IsType<SolidColorBrush>(resetButton.Foreground).Color);

        field!.XText = "120";
        await PumpAsync();
        Assert.True(resetButton.IsEnabled);
        Assert.NotEqual(
            Assert.IsType<SolidColorBrush>(placeholder).Color,
            Assert.IsType<SolidColorBrush>(resetButton.Foreground).Color);

        // 重置回默认后按钮保留，但重新置灰且不可交互。
        field.ResetToDefault();
        await PumpAsync();
        Assert.True(resetButton.IsVisible);
        Assert.False(resetButton.IsEnabled);
        Assert.Equal("0", inputs[0].Text);
    }

    private static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.UIThread.InvokeAsync(() => { });
    }
}
