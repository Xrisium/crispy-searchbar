using CrispySearchbar.Core.Search;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class OpenUrlBuilderTests
{
    [Theory]
    [InlineData("apple", "https://www.bing.com/search?q=apple")]
    [InlineData("hello world", "https://www.bing.com/search?q=hello%20world")]
    [InlineData("a&b=c", "https://www.bing.com/search?q=a%26b%3Dc")]
    public void Build_ReplacesPlaceholderWithEncodedQuery(string query, string expected)
    {
        var url = OpenUrlBuilder.Build("https://www.bing.com/search?q={0}", query);
        Assert.Equal(expected, url);
    }

    [Fact]
    public void Build_AppendsQueryWhenTemplateHasNoPlaceholder()
    {
        var url = OpenUrlBuilder.Build("https://example.com/search", "test query");
        Assert.Equal("https://example.com/search?q=test%20query", url);
    }

    [Fact]
    public void Build_EncodesChineseCharacters()
    {
        var url = OpenUrlBuilder.Build("https://chat.deepseek.com/?q={0}", "你好世界");
        Assert.Equal("https://chat.deepseek.com/?q=%E4%BD%A0%E5%A5%BD%E4%B8%96%E7%95%8C", url);
    }
}
