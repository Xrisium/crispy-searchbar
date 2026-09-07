using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Modes;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class SearchModeTests
{
    [Fact]
    public void AskAi_UsesBigFishDisplayNameAndDeepSeekWatermark()
    {
        var mode = SearchMode.AskAi("https://chat.deepseek.com/?q={0}");

        Assert.Equal("ask-ai", mode.Key);
        Assert.Equal("问问大肥鱼", mode.Title);
        Assert.Equal("输入问题，按 Enter 跳转到 DeepSeek 网页端", mode.Watermark);
        Assert.Equal("按 Enter 跳转到 DeepSeek 网页端", mode.ActionHint);
    }

    [Fact]
    public void DefaultCatalog_KeepsDocumentedModeOrderAndDisplayNames()
    {
        var settings = new AppSettings();
        var modes = SearchModeCatalog.CreateDefault(settings);

        Assert.Equal(
            new[] { "web-search", "ask-ai", "dictionary" },
            modes.Select(mode => mode.Key));
        Assert.Equal("网页搜索", modes[0].Title);
        Assert.Equal("问问大肥鱼", modes[1].Title);
        Assert.Equal("词典", modes[2].Title);
    }
}