using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class BundledEcdictTests
{
    [Fact]
    public void BundledEcdict_LoadsAndAnswersEnglishQueries()
    {
        var dataPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "data", "ecdict", "ecdict.csv"));
        Assert.True(File.Exists(dataPath), $"Bundled ECDICT not found at {dataPath}");

        var index = EcdictIndexLoader.LoadFile(dataPath);

        Assert.True(index.Count >= 700_000, $"Expected full ECDICT, got {index.Count} entries.");

        var apple = index.Search("apple");
        Assert.Equal("apple", apple[0].Word);
        Assert.Contains(apple[0].Senses, sense => sense.Contains("苹果"));

        var water = index.Search("water");
        Assert.Equal("water", water[0].Word);

        Assert.Equal("apple", index.Search("APPLE")[0].Word);
        Assert.Equal("search", index.Search("search")[0].Word);
    }
}
