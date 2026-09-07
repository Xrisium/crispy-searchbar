using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class CedictParserTests
{
    [Fact]
    public void TryParse_ReadsCcCedictLine()
    {
        var entry = CedictParser.TryParse("蘋果 苹果 [píng guǒ] /apple/CL:個[ge4],颗[ke1]/apple (computer)/");

        Assert.NotNull(entry);
        Assert.Equal("蘋果", entry.Traditional);
        Assert.Equal("苹果", entry.Simplified);
        Assert.Equal("píng guǒ", entry.Pinyin);
        Assert.Equal(
            new[] { "apple", "CL:個[ge4],颗[ke1]", "apple (computer)" },
            entry.Definitions);
        Assert.Equal("CC-CEDICT", entry.Source);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("# 注释行")]
    [InlineData("% 另一种注释")]
    [InlineData("没有方括号的行")]
    [InlineData("只有中文 [没有释义]")]
    public void TryParse_ReturnsNullForCommentsAndMalformedLines(string line)
    {
        Assert.Null(CedictParser.TryParse(line));
    }

    [Fact]
    public void ParseLines_SkipsCommentsAndKeepsValidEntries()
    {
        var lines = new[]
        {
            "# CC-CEDICT 文件头",
            "你好 你好 [nǐ hǎo] /hello/",
            "再見 再见 [zài jiàn] /goodbye/",
            "这不是有效行",
        };

        var entries = CedictParser.ParseLines(lines);

        Assert.Equal(2, entries.Count);
        Assert.Equal("你好", entries[0].Simplified);
        Assert.Equal("再见", entries[1].Simplified);
    }
}
