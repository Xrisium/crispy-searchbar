using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class CedictIndexTests
{
    [Fact]
    public void Search_ChineseSimplifiedAndTraditional_ReturnSameEntry()
    {
        var index = BuildSampleIndex();

        var simplifiedResult = index.Search("苹果");
        var traditionalResult = index.Search("蘋果");

        Assert.Equal("苹果", simplifiedResult[0].Simplified);
        Assert.Equal("苹果", traditionalResult[0].Simplified);
        Assert.Equal("蘋果", traditionalResult[0].Traditional);
    }

    [Fact]
    public void Search_ChineseOrdersExactBeforePrefixAndPreservesSourceOrder()
    {
        var index = BuildSampleIndex();

        var results = index.Search("苹果");

        Assert.Equal("苹果", results[0].Simplified);
        Assert.Equal("苹果树", results[1].Simplified);
    }

    [Fact]
    public void Search_ChineseContainsMatch_IsAvailableWhenNoHeadPrefixMatches()
    {
        var index = BuildSampleIndex();

        var results = index.Search("果酱");

        Assert.Contains(results, result => result.Simplified == "果酱");
        Assert.Contains(results, result => result.Simplified == "苹果酱");
    }

    [Fact]
    public void Search_EnglishQuery_IsNotAnsweredByChineseIndex()
    {
        var index = BuildSampleIndex();

        Assert.Empty(index.Search("apple"));
    }

    [Fact]
    public void Search_ResultLimit_IsHonored()
    {
        var index = BuildSampleIndex();

        var results = index.Search("苹", maxResults: 2);

        Assert.Equal(2, results.Count);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Search_EmptyQuery_ReturnsNoResults(string? query)
    {
        var index = BuildSampleIndex();

        Assert.Empty(index.Search(query));
    }

    [Fact]
    public void Search_UnknownQuery_ReturnsNoResults()
    {
        var index = BuildSampleIndex();

        Assert.Empty(index.Search("zzzzzz"));
        Assert.Empty(index.Search("没有这个词条"));
    }

    [Fact]
    public void Build_EmptyInput_ReturnsEmptyIndex()
    {
        var index = CedictIndex.Build([]);

        Assert.Equal(0, index.Count);
        Assert.Empty(index.Search("苹果"));
    }

    private static CedictIndex BuildSampleIndex() => CedictIndex.Build(new[]
    {
        Entry("蘋果", "苹果", "píng guǒ", "apple", "apple (computer)"),
        Entry("蘋果樹", "苹果树", "píng guǒ shù", "apple tree"),
        Entry("你好", "你好", "nǐ hǎo", "hello", "how are you"),
        Entry("再見", "再见", "zài jiàn", "goodbye"),
        Entry("跑", "跑", "pǎo", "to run"),
        Entry("跑鞋", "跑鞋", "pǎo xié", "running shoes"),
        Entry("果", "果", "guǒ", "fruit"),
        Entry("果醬", "果酱", "guǒ jiàng", "jam"),
        Entry("蘋果醬", "苹果酱", "píng guǒ jiàng", "apple jam"),
        Entry("學習", "学习", "xué xí", "to learn", "to study"),
    });

    private static DictionaryEntry Entry(
        string traditional,
        string simplified,
        string pinyin,
        params string[] definitions)
        => new(traditional, simplified, pinyin, definitions);
}
