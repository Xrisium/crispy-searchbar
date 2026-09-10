using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// 回归测试：设置面板中的开关（通用开关行与模式列表行）右侧标签必须本地化，
/// 不能停留在 Avalonia ToggleSwitch 默认的英文 On/Off。
/// </summary>
public sealed class SettingsToggleTextTests
{
    [AvaloniaFact]
    public async Task ToggleSwitches_ShowLocalizedOnOffText()
    {
        TestStyles.Ensure();
        var window = new SettingsWindow();
        window.Show();
        await PumpAsync();

        window.ReloadFromConfiguration(
            new AppSettings { Language = AppLanguage.SimplifiedChinese },
            TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese));
        await PumpAsync();

        var toggles = window.GetVisualDescendants().OfType<ToggleSwitch>().ToList();
        Assert.NotEmpty(toggles);
        Assert.All(toggles, toggle =>
        {
            Assert.Equal("开", toggle.OnContent);
            Assert.Equal("关", toggle.OffContent);

            // 模板必须把当前状态的标签真正渲染成可见文本，而不只是停留在开关属性上。
            var labels = toggle
                .GetVisualDescendants()
                .OfType<TextBlock>()
                .Select(text => text.Text)
                .ToList();
            Assert.Contains(toggle.IsChecked == true ? "开" : "关", labels);
        });
    }

    [AvaloniaFact]
    public async Task ToggleSwitch_LabelsAreNotClippedByTheModeRowColumn()
    {
        TestStyles.Ensure();
        var window = new SettingsWindow();
        window.Show();
        await PumpAsync();

        window.ReloadFromConfiguration(
            new AppSettings { Language = AppLanguage.SimplifiedChinese },
            TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese));
        await PumpAsync();

        var toggles = window.GetVisualDescendants().OfType<ToggleSwitch>().ToList();
        Assert.NotEmpty(toggles);

        // 用同一套控件的自然宽度作为基准：列宽不足时开关会被压到列宽，标签随之被裁掉。
        var probe = new ToggleSwitch
        {
            OnContent = toggles[0].OnContent,
            OffContent = toggles[0].OffContent,
            FontFamily = new FontFamily("Segoe UI, Microsoft YaHei UI"),
        };
        var host = new Window { Content = probe, Width = 320, Height = 80 };
        host.Show();
        await PumpAsync();
        var naturalWidth = probe.DesiredSize.Width;

        Assert.All(
            toggles,
            toggle => Assert.True(
                toggle.Bounds.Width >= naturalWidth - 0.5,
                $"开关标签被列宽裁掉：可用 {toggle.Bounds.Width}，需要 {naturalWidth}"));
    }

    private static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.UIThread.InvokeAsync(() => { });
    }
}
