using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class BundledCcCedictTests
{
    [Fact]
    public void BundledCcCedict_LoadsAndAnswersChineseQueries()
    {
        var dataPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "data", "cc-cedict", "cedict_ts.u8"));
        Assert.True(File.Exists(dataPath), $"Bundled CC-CEDICT not found at {dataPath}");

        var index = CedictIndexLoader.LoadFile(dataPath);

        Assert.True(index.Count >= 124_000, $"Expected full CC-CEDICT, got {index.Count} entries.");
        Assert.Equal("苹果", index.Search("苹果")[0].Simplified);
        Assert.Contains(index.Search("你好"), entry => entry.Simplified == "你好");
        Assert.Contains(index.Search("學校"), entry => entry.Simplified == "学校");
        Assert.Contains(index.Search("跑"), entry => entry.Simplified == "跑");

        // 汉英方向索引不负责英文查询：英文输入应交给 ECDICT（英汉）数据源。
        Assert.Empty(index.Search("apple"));
    }
}
