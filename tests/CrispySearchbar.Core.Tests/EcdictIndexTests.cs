using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class EcdictIndexTests
{
    [Fact]
    public void Search_ExactWord_ComesFirst_AndIsCaseInsensitive()
    {
        var index = BuildSampleIndex();

        Assert.Equal("apple", index.Search("apple")[0].Word);
        Assert.Equal("apple", index.Search("APPLE")[0].Word);
        Assert.Equal("apple", index.Search("apple!")[0].Word);
    }

    [Fact]
    public void Search_Prefix_ReturnsWordsInStableLexicalOrder()
    {
        var index = BuildSampleIndex();

        var results = index.Search("app");

        Assert.Equal("app", results[0].Word);
        Assert.Equal("apple", results[1].Word);
        Assert.Equal("apple tree", results[2].Word);
        Assert.Equal("application", results[3].Word);
    }

    [Fact]
    public void Search_ExactSense_WinsOverPrefixMatch()
    {
        var index = BuildSampleIndex();

        var results = index.Search("run");

        Assert.Equal("run", results[0].Word);
        Assert.Contains(results, result => result.Word == "running");
    }

    [Fact]
    public void Search_ResultLimit_IsHonored()
    {
        var index = BuildSampleIndex();

        var results = index.Search("app", maxResults: 2);

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

        Assert.Empty(index.Search("zzzzzzzzzz"));
    }

    [Fact]
    public void Build_EmptyInput_ReturnsEmptyIndex()
    {
        var index = EcdictIndex.Build([]);

        Assert.Equal(0, index.Count);
        Assert.Empty(index.Search("apple"));
    }

    private static EcdictIndex BuildSampleIndex() => EcdictIndex.Build(new[]
    {
        Ecdict("apple", "n. 苹果"),
        Ecdict("apple tree", "n. 苹果树"),
        Ecdict("application", "n. 申请；应用"),
        Ecdict("app", "n. 应用"),
        Ecdict("run", "v. 跑；经营"),
        Ecdict("running", "n. 跑步"),
        Ecdict("water", "n. 水"),
    });

    private static EcdictEntry Ecdict(string word, string sense)
        => new(word, null, new[] { sense }, null);
}
