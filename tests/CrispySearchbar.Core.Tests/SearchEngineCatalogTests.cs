using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class SearchEngineCatalogTests
{
    [Theory]
    [InlineData(SearchEngineKind.Baidu, "https://www.baidu.com/s?wd={0}")]
    [InlineData(SearchEngineKind.Google, "https://www.google.com/search?q={0}")]
    [InlineData(SearchEngineKind.Bing, "https://www.bing.com/search?q={0}")]
    public void BuiltInEngines_HaveUrlTemplate(
        SearchEngineKind engine,
        string expectedUrl)
    {
        Assert.Equal(expectedUrl, SearchEngineCatalog.GetUrlTemplate(engine));
    }

    [Fact]
    public void DisplayNames_FollowConfiguredLanguage()
    {
        Assert.Equal(
            "百度",
            SearchEngineCatalog.GetDisplayName(SearchEngineKind.Baidu, AppStrings.SimplifiedChinese));
        Assert.Equal(
            "必应",
            SearchEngineCatalog.GetDisplayName(SearchEngineKind.Bing, AppStrings.SimplifiedChinese));

        Assert.Equal(
            "Baidu",
            SearchEngineCatalog.GetDisplayName(SearchEngineKind.Baidu, AppStrings.English));
        Assert.Equal(
            "Bing",
            SearchEngineCatalog.GetDisplayName(SearchEngineKind.Bing, AppStrings.English));
        Assert.Equal(
            "Google",
            SearchEngineCatalog.GetDisplayName(SearchEngineKind.Google, AppStrings.English));
    }

    [Fact]
    public void BuiltIn_ContainsOnlyTheThreePresetEngines()
    {
        Assert.Equal(
            new[] { SearchEngineKind.Baidu, SearchEngineKind.Google, SearchEngineKind.Bing },
            SearchEngineCatalog.BuiltIn);
    }
}
