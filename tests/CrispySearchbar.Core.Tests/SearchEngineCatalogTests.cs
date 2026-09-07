using CrispySearchbar.Core.Configuration;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class SearchEngineCatalogTests
{
    [Theory]
    [InlineData(SearchEngineKind.Baidu, "https://www.baidu.com/s?wd={0}", "百度")]
    [InlineData(SearchEngineKind.Google, "https://www.google.com/search?q={0}", "Google")]
    [InlineData(SearchEngineKind.Bing, "https://www.bing.com/search?q={0}", "必应")]
    public void BuiltInEngines_HaveUrlTemplateAndDisplayName(
        SearchEngineKind engine, string expectedUrl, string expectedName)
    {
        Assert.Equal(expectedUrl, SearchEngineCatalog.GetUrlTemplate(engine));
        Assert.Equal(expectedName, SearchEngineCatalog.GetDisplayName(engine));
    }

    [Fact]
    public void BuiltIn_ContainsOnlyTheThreePresetEngines()
    {
        Assert.Equal(
            new[] { SearchEngineKind.Baidu, SearchEngineKind.Google, SearchEngineKind.Bing },
            SearchEngineCatalog.BuiltIn);
    }
}
