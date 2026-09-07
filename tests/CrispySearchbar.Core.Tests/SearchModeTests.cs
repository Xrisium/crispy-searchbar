using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Core.Modes;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class SearchModeTests
{
    [Fact]
    public void AskAi_UsesLocalizedAskAiTexts()
    {
        var strings = AppStrings.SimplifiedChinese;
        var mode = SearchMode.AskAi(
            strings.AskAiMode,
            "https://chat.deepseek.com/?q={0}");

        Assert.Equal("ask-ai", mode.Key);
        Assert.Equal("问问大肥鱼", mode.Title);
        Assert.Equal("输入问题，按 Enter 跳转到 DeepSeek 网页端", mode.Watermark);
        Assert.Equal("按 Enter 跳转到 DeepSeek 网页端", mode.ActionHint);
    }

    [Fact]
    public void DefaultCatalog_KeepsDocumentedModeOrderAndChineseDisplayNames()
    {
        var settings = new AppSettings();
        var modes = SearchModeCatalog.CreateDefault(settings, AppStrings.SimplifiedChinese);

        Assert.Equal(
            new[] { "web-search", "ask-ai", "dictionary" },
            modes.Select(mode => mode.Key));
        Assert.Equal("网页搜索", modes[0].Title);
        Assert.Equal("问问大肥鱼", modes[1].Title);
        Assert.Equal("词典", modes[2].Title);
    }

    [Fact]
    public void DefaultCatalog_UsesEnglishTextsWhenEnglishIsConfigured()
    {
        var settings = new AppSettings();
        var modes = SearchModeCatalog.CreateDefault(settings, AppStrings.English);

        Assert.Equal("Web Search", modes[0].Title);
        Assert.Equal("Ask DeepSeek", modes[1].Title);
        Assert.Equal("Dictionary", modes[2].Title);
        Assert.Equal(
            "Type an English word or Chinese term",
            modes[2].Watermark);
    }
}
