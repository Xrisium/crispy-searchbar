using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// 回归测试：设置面板“外观”分类中的搜索框位置行渲染两个数字输入框，
/// 且“恢复默认位置”按钮只在偏离默认位置时出现。
/// </summary>
public sealed class SearchBarPositionFieldTests
{
    [AvaloniaFact]
    public async Task OffsetRow_ExposesTwoNumericInputsAndResetButton()
    {
        TestStyles.Ensure();
        var window = new SettingsWindow();
        window.Show();
        await PumpAsync();

        var field = window
            .GetVisualDescendants()
            .Select(visual => visual.DataContext)
            .OfType<OffsetSettingFieldViewModel>()
            .FirstOrDefault();
        Assert.NotNull(field);

        var inputs = window
            .GetVisualDescendants()
            .OfType<NumericUpDown>()
            .ToList();
        Assert.Equal(2, inputs.Count);
        Assert.All(inputs, input => Assert.False(input.ShowButtonSpinner));
        Assert.Equal(0m, inputs[0].Value);
        Assert.Equal(0m, inputs[1].Value);
        Assert.All(
            inputs,
            input => Assert.NotNull(
                input.GetVisualDescendants().OfType<TextBox>().FirstOrDefault()));

        field!.X = 120m;
        await PumpAsync();

        var resetButton = window
            .GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(button => button.DataContext is OffsetSettingFieldViewModel
                && button.Content is string text
                && text == field.ResetText);
        Assert.NotNull(resetButton);
        Assert.True(resetButton!.IsVisible);
    }

    private static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.UIThread.InvokeAsync(() => { });
    }
}
