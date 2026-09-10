using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class BundledCcCedictTests
{
    [Fact]
    public void BundledCcCedict_LoadsAndAnswersChineseQueries()
    {
        var index = LoadBundledIndex();

        Assert.True(index.Count >= 124_000, $"Expected full CC-CEDICT, got {index.Count} entries.");
        Assert.Equal("苹果", index.Search("苹果")[0].Simplified);
        Assert.Contains(index.Search("你好"), entry => entry.Simplified == "你好");
        Assert.Contains(index.Search("學校"), entry => entry.Simplified == "学校");
        Assert.Contains(index.Search("跑"), entry => entry.Simplified == "跑");

        // 汉英方向索引不负责英文查询：英文输入应交给 ECDICT（英汉）数据源。
        Assert.Empty(index.Search("apple"));
    }

    /// <summary>内置数据也要能被生产路径上的统一加载入口读取（含来源名）。</summary>
    [Fact]
    public void BundledCcCedict_LoadsThroughUnifiedSourceLoader()
    {
        var index = LoadBundledIndex();

        Assert.True(index.Count >= 124_000, $"Expected full CC-CEDICT, got {index.Count} entries.");
        var entry = index.Search("苹果")[0];
        Assert.Equal("苹果", entry.Simplified);
        Assert.Equal("CC-CEDICT", entry.Source);
    }

    /// <summary>内置 CC-CEDICT 随程序集内嵌（gzip 资源），测试不得依赖任何外部数据文件。</summary>
    private static CedictIndex LoadBundledIndex()
    {
        using var stream = BundledDictionaryResources.TryOpenCcCedict()
            ?? throw new InvalidOperationException("Bundled CC-CEDICT resource is missing.");
        return DictionarySourceLoader.LoadChineseIndex(
            stream,
            BundledDictionaryResources.CcCedict.Format,
            BundledDictionaryResources.CcCedict.SourceName);
    }
}
