using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Styling;
using Avalonia.Threading;
using CrispySearchbar.Core.Dictionary;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.Dictionary;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

public sealed class WebDictionaryLinkTests
{
    [AvaloniaTheory]
    [InlineData("hell", "hello", false, false, "hello")]
    [InlineData("ice", "ice cream", false, true, "ice%20cream")]
    [InlineData("rock", "rock'n'roll", false, false, "rock%27n%27roll")]
    [InlineData("test", "test/a?b#c&d", false, true, "test%2Fa%3Fb%23c%26d")]
    [InlineData("蘋", "苹果", true, false, "%E8%8B%B9%E6%9E%9C")]
    [InlineData("苹", "苹果", true, true, "%E8%8B%B9%E6%9E%9C")]
    public async Task LinkActivationOpensCurrentHeadwordInCorrectDictionary(
        string query, string headword, bool chinese, bool dark, string encodedHeadword)
    {
        TestStyles.Ensure();
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese);
        var openedUrls = new List<string>();
        // 同一自定义来源名用于两个槽位，确保方向不依赖来源标记。
        var data = new DictionaryLoadResult(
            CedictIndex.Build([new DictionaryEntry("蘋果", "苹果", "píng guǒ", ["apple"])
                { Source = "Custom dictionary" }]), null,
            EcdictIndex.Build([new EcdictEntry(headword, null, ["测试释义"], null)
                { Source = "Custom dictionary" }]), null);
        var viewModel = new MainWindowViewModel(strings,
            [SearchMode.Dictionary(strings.DictionaryMode)], openedUrls.Add,
            loadDictionary: () => Task.FromResult(data));
        var window = new MainWindow
        {
            DataContext = viewModel,
            RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light,
            // Headless 平台把 Popup 嵌入主窗口，需预留详情底部的点击区域。
            Height = 600,
        };

        try
        {
            window.Show();
            viewModel.OnWindowShown();
            viewModel.OpenWebDictionary();
            Assert.Empty(openedUrls);

            viewModel.Query = query;
            var deadline = DateTime.UtcNow.AddSeconds(5);
            while (viewModel.DictionaryCandidates.Count == 0 && DateTime.UtcNow < deadline)
            {
                await Dispatcher.UIThread.InvokeAsync(() => { });
                await Task.Delay(10);
            }

            Assert.True(viewModel.OpenSelectedDictionaryEntry());
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            await Dispatcher.UIThread.InvokeAsync(() => { });

            var link = window.FindControl<HyperlinkButton>("WebDictionaryLink")!;
            Assert.True(link.IsEffectivelyVisible);
            Assert.Equal("网页词典释义", link.Content);
            var stack = Assert.IsType<StackPanel>(link.Parent);
            Assert.Same(link, stack.Children[^1]);
            Assert.Equal("Custom dictionary", Assert.IsType<TextBlock>(stack.Children[^2]).Text);

            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            var point = link.TranslatePoint(new Point(link.Bounds.Width / 2, link.Bounds.Height / 2), window);
            Assert.NotNull(point);
            window.MouseDown(point.Value, MouseButton.Left);
            window.MouseUp(point.Value, MouseButton.Left);

            var dictionary = chinese ? "chinese-simplified-english" : "english-chinese-simplified";
            Assert.Equal($"https://dictionary.cambridge.org/dictionary/{dictionary}/{encodedHeadword}",
                Assert.Single(openedUrls));
        }
        finally
        {
            window.AllowClose();
            window.Close();
        }
    }
}
