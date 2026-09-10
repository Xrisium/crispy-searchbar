using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CrispySearchbar.Core.Dictionary;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.Dictionary;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// 回归测试：鼠标左键点击候选框词条必须打开对应词条详情。
/// 该行为曾失效：候选行模板上订阅指针事件不会触发，因为浮层里的指针事件到达行模板前
/// 已被上层标记为已处理，且行模板不在路由事件路径上；现在统一在候选列表上处理 PointerReleased。
/// </summary>
public sealed class DictionaryCandidateClickTests
{
    [AvaloniaFact]
    public async Task LeftClickOnSecondCandidateOpensThatEntry()
    {
        var (window, viewModel) = await CreateDictionaryWindowAsync("hello");
        var list = FindVisibleListBox(window);
        Assert.True(viewModel.DictionaryCandidates.Count >= 2, "需要至少两个候选才能验证点击第二条。");

        var expected = viewModel.DictionaryCandidates[1].DetailTitle;
        ClickContainer(window, list, 1);

        Assert.True(viewModel.IsDictionaryDetailOpen, "点击候选词条后详情卡片应打开。");
        Assert.Equal(expected, viewModel.DictionaryDetailTitle);
        // 详情打开时清理候选列表，选中下标随之复位（与 Enter 打开详情的行为一致）。
        Assert.Equal(-1, viewModel.DictionarySelectedIndex);
        Assert.False(viewModel.IsDictionaryPanelOpen, "详情打开后候选列表应让位给详情卡片。");
    }

    private static async Task<(Window Window, MainWindowViewModel ViewModel)> CreateDictionaryWindowAsync(
        string query)
    {
        TestStyles.Ensure();
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.English);
        var dataPath = Path.Combine(AppContext.BaseDirectory, AppDictionaryResources.EcdictFileName);
        Assert.True(File.Exists(dataPath), $"测试需要 ECDICT 数据文件：{dataPath}");

        var index = await Task.Run(() => EcdictIndexLoader.LoadFile(dataPath));
        var viewModel = new MainWindowViewModel(
            strings,
            [SearchMode.Dictionary(strings.DictionaryMode)],
            _ => { },
            dictionaryEmptyHint: strings.DictionaryEmptyHint,
            loadDictionary: () => Task.FromResult(
                new DictionaryLoadResult(
                    CedictIndex: null,
                    CedictMessage: null,
                    EcdictIndex: index,
                    EcdictMessage: null)));

        var window = new MainWindow { DataContext = viewModel, Width = 660, Height = 88 };
        window.Show();
        viewModel.OnWindowShown();
        await PumpAsync();
        viewModel.Query = query;

        await WaitForAsync(() => viewModel.DictionaryCandidates.Count > 0);
        Assert.True(viewModel.DictionaryCandidates.Count > 0, $"查询“{query}”应产生候选。");
        await PumpAsync();
        return (window, viewModel);
    }

    private static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.UIThread.InvokeAsync(() => { });
    }

    private static void ClickContainer(Window window, ListBox list, int index)
    {
        var container = list.ContainerFromIndex(index) as Control;
        Assert.NotNull(container);

        var point = container!.TranslatePoint(
            new Point(20, Math.Max(1, container.Bounds.Height / 2)),
            window);
        Assert.NotNull(point);

        window.MouseDown(point!.Value, MouseButton.Left);
        window.MouseUp(point.Value, MouseButton.Left);
    }

    private static ListBox FindVisibleListBox(Window window)
    {
        var list = window
            .GetVisualDescendants()
            .OfType<ListBox>()
            .FirstOrDefault(item => item.IsVisible);
        Assert.NotNull(list);
        return list!;
    }

    private static async Task WaitForAsync(Func<bool> condition, int timeoutMs = 20000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            await Dispatcher.UIThread.InvokeAsync(() => { });
            if (condition())
            {
                return;
            }

            await Task.Delay(25);
        }
    }
}
