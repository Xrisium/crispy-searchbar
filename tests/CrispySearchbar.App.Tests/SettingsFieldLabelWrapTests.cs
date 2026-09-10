using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// 回归测试：设置面板中每个可编辑项的标题必须在剩余宽度内换行，
/// 不能在行尾被网格裁掉（标题列为 * 列，右侧控件列宽固定）。
/// </summary>
public sealed class SettingsFieldLabelWrapTests
{
    [AvaloniaFact]
    public async Task SettingFieldLabelsWrapInsteadOfBeingClipped()
    {
        TestStyles.Ensure();
        var window = new SettingsWindow();
        window.Show();
        await PumpAsync();

        var labels = window
            .GetVisualDescendants()
            .OfType<TextBlock>()
            .Where(text => text.DataContext is SettingFieldViewModel field
                && text.Text == field.Label)
            .ToList();

        Assert.NotEmpty(labels);
        Assert.All(labels, label => Assert.Equal(TextWrapping.Wrap, label.TextWrapping));
    }

    private static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.UIThread.InvokeAsync(() => { });
    }
}
