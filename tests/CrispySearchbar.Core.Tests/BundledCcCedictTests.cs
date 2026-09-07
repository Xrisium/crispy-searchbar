using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class BundledCcCedictTests
{
    [Fact]
    public void BundledCcCedict_LoadsAndAnswersCommonBidirectionalQueries()
    {
        var dataPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "data", "cc-cedict", "cedict_ts.u8"));
        Assert.True(File.Exists(dataPath), $"Bundled CC-CEDICT not found at {dataPath}");

        var index = CedictIndexLoader.LoadFile(dataPath);

        Assert.True(index.Count >= 124_000, $"Expected full CC-CEDICT, got {index.Count} entries.");
        Assert.Equal("苹果", index.Search("apple")[0].Simplified);
        Assert.Contains(index.Search("你好"), entry => entry.Simplified == "你好");
        Assert.Contains(index.Search("學校"), entry => entry.Simplified == "学校");
        var searchResults = index.Search("search");
        Assert.NotEmpty(searchResults);
        Assert.StartsWith("搜", searchResults[0].Simplified);
    }
}
