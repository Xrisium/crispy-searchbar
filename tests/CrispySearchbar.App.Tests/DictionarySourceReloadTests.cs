using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using CrispySearchbar.Core.Dictionary;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.Dictionary;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// 词典路径配置变化后 ReloadDictionarySource 的行为：
/// 词典模式下有输入立即用新数据源重查，无输入只显示空提示，非词典模式只替换加载任务。
/// </summary>
public sealed class DictionarySourceReloadTests
{
    [AvaloniaFact]
    public async Task ReloadDictionarySource_ReQueriesWithNewSource()
    {
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.English);
        var firstIndex = CedictIndex.Build(
            [new DictionaryEntry("苹果", "苹果", "píng guǒ", ["apple"])]);
        var secondIndex = CedictIndex.Build(
            [new DictionaryEntry("苹果", "苹果", "píng guǒ", ["apple (new)"])]);

        var viewModel = new MainWindowViewModel(
            strings,
            [SearchMode.Dictionary(strings.DictionaryMode)],
            _ => { },
            dictionaryEmptyHint: strings.DictionaryEmptyHint,
            loadDictionary: () => Task.FromResult(
                new DictionaryLoadResult(firstIndex, null, null, null)));

        viewModel.OnWindowShown();
        viewModel.Query = "苹果";
        await WaitForAsync(() => viewModel.DictionaryCandidates.Count > 0);
        Assert.Equal("apple", viewModel.DictionaryCandidates[0].Definitions[0]);

        viewModel.ReloadDictionarySource(() => Task.FromResult(
            new DictionaryLoadResult(secondIndex, null, null, null)));

        await WaitForAsync(() => viewModel.DictionaryCandidates.Count > 0
            && viewModel.DictionaryCandidates[0].Definitions[0] == "apple (new)");
        Assert.Equal("apple (new)", viewModel.DictionaryCandidates[0].Definitions[0]);
    }

    [AvaloniaFact]
    public async Task ReloadDictionarySource_WithoutQuery_ShowsEmptyHintAndDefersLoad()
    {
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.English);
        var index = CedictIndex.Build(
            [new DictionaryEntry("苹果", "苹果", "píng guǒ", ["apple"])]);
        var loadCount = 0;
        Func<Task<DictionaryLoadResult>> load = () =>
        {
            loadCount++;
            return Task.FromResult(new DictionaryLoadResult(index, null, null, null));
        };

        var viewModel = new MainWindowViewModel(
            strings,
            [SearchMode.Dictionary(strings.DictionaryMode)],
            _ => { },
            dictionaryEmptyHint: "empty hint",
            loadDictionary: load);

        viewModel.OnWindowShown();
        viewModel.ReloadDictionarySource(load);
        await PumpAsync();

        Assert.Equal(0, loadCount);
        Assert.Empty(viewModel.DictionaryCandidates);
        Assert.Equal("empty hint", viewModel.DictionaryHint);

        viewModel.Query = "苹果";
        await WaitForAsync(() => viewModel.DictionaryCandidates.Count > 0);
        Assert.Equal(1, loadCount);
    }

    [AvaloniaFact]
    public async Task ReloadDictionarySource_OutsideDictionaryMode_DoesNotQuery()
    {
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.English);
        var index = CedictIndex.Build(
            [new DictionaryEntry("苹果", "苹果", "píng guǒ", ["apple"])]);
        var loadCount = 0;

        var viewModel = new MainWindowViewModel(
            strings,
            [SearchMode.WebSearch(strings.WebSearchMode, "https://example.com/?q={0}")],
            _ => { },
            dictionaryEmptyHint: strings.DictionaryEmptyHint,
            loadDictionary: () =>
            {
                loadCount++;
                return Task.FromResult(new DictionaryLoadResult(index, null, null, null));
            });

        viewModel.OnWindowShown();
        viewModel.Query = "苹果";
        viewModel.ReloadDictionarySource(() => Task.FromResult(
            new DictionaryLoadResult(index, null, null, null)));
        await PumpAsync();

        Assert.Equal(0, loadCount);
        Assert.Empty(viewModel.DictionaryCandidates);
    }

    private static async Task PumpAsync()
    {
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
        await Dispatcher.UIThread.InvokeAsync(() => { });
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
