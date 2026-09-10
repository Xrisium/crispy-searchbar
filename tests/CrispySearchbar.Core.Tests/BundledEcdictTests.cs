using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class BundledEcdictTests
{
    [Fact]
    public void BundledEcdict_LoadsAndAnswersEnglishQueries()
    {
        var index = LoadBundledIndex();

        Assert.True(index.Count >= 700_000, $"Expected full ECDICT, got {index.Count} entries.");

        var apple = index.Search("apple");
        Assert.Equal("apple", apple[0].Word);
        Assert.Contains(apple[0].Senses, sense => sense.Contains("苹果"));

        var water = index.Search("water");
        Assert.Equal("water", water[0].Word);

        Assert.Equal("apple", index.Search("APPLE")[0].Word);
        Assert.Equal("search", index.Search("search")[0].Word);
        Assert.Equal("ECDICT", index.Search("apple")[0].Source);
    }

    /// <summary>内置 ECDICT 随程序集内嵌（gzip 资源），测试不得依赖任何外部数据文件。</summary>
    internal static EcdictIndex LoadBundledIndex()
    {
        using var stream = BundledDictionaryResources.TryOpenEcdict()
            ?? throw new InvalidOperationException("Bundled ECDICT resource is missing.");
        return DictionarySourceLoader.LoadEnglishIndex(
            stream,
            BundledDictionaryResources.Ecdict.Format,
            BundledDictionaryResources.Ecdict.SourceName);
    }
}
