using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class DictionarySearchHitTests
{
    [Fact]
    public void EnglishQuery_ReturnsHitsWithEnglishForm_AndPrioritizesDirectSense()
    {
        var index = DictionaryIndex.Build(new[]
        {
            Entry("一手包辦", "一手包办", "yi1 shou3 bao1 ban4", "to run the whole show"),
            Entry("跑", "跑", "pao3", "to run"),
            Entry("蘋果公司", "苹果公司", "ping2 guo3 gong1 si1", "Apple Inc."),
            Entry("蘋果", "苹果", "ping2 guo3", "apple", "apple (computer)"),
        });

        var runHits = index.SearchHits("run");
        Assert.Equal("run", runHits[0].EnglishForm);
        Assert.Equal("跑", runHits[0].Entry.Simplified);

        var appleHits = index.SearchHits("apple");
        Assert.Equal("apple", appleHits[0].EnglishForm);
        Assert.Equal("苹果", appleHits[0].Entry.Simplified);
    }

    [Fact]
    public void EnglishPrefix_ReturnsFullMatchedTokenAsEnglishForm()
    {
        var index = DictionaryIndex.Build(new[]
        {
            Entry("跑步", "跑步", "pao3 bu4", "running"),
            Entry("跑鞋", "跑鞋", "pao3 xie2", "running shoes"),
        });

        var hits = index.SearchHits("runn");

        Assert.All(hits.Take(2), hit => Assert.Equal("running", hit.EnglishForm));
    }

    [Fact]
    public void ChineseQuery_HitsDoNotCarryEnglishForm()
    {
        var index = DictionaryIndex.Build(new[]
        {
            Entry("你好", "你好", "ni3 hao3", "hello; hi"),
        });

        var hits = index.SearchHits("你好");

        var hit = Assert.Single(hits);
        Assert.False(hit.IsEnglishMatch);
        Assert.Null(hit.EnglishForm);
    }

    private static DictionaryEntry Entry(
        string traditional,
        string simplified,
        string pinyin,
        params string[] definitions)
        => new(traditional, simplified, pinyin, definitions);
}
