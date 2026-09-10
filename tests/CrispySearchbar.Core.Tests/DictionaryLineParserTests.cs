using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class DictionaryLineParserTests
{
    private static IReadOnlyList<DictionaryRecord> Parse(string text)
    {
        using var reader = new StringReader(text);
        return DictionaryLineParser.Parse(reader, "CC-CEDICT", "my-list");
    }

    [Fact]
    public void Parse_ReadsCcCedictLines()
    {
        var records = Parse("# header\n蘋果 苹果 [píng guǒ] /apple/\n");

        var record = Assert.Single(records);
        Assert.Equal("苹果", record.Headword);
        Assert.Equal("蘋果", record.Traditional);
        Assert.Equal("píng guǒ", record.Pinyin);
        Assert.Equal(new[] { "apple" }, record.Definitions);
        Assert.Equal("CC-CEDICT", record.Source);
    }

    [Fact]
    public void Parse_ReadsGenericTabLinesAndMergesByHeadword()
    {
        var records = Parse("apple\tn. 苹果\nwater\tn. 水\napple\tn. 苹果树\n");

        Assert.Equal(2, records.Count);
        Assert.Equal("apple", records[0].Headword);
        Assert.Equal(new[] { "n. 苹果", "n. 苹果树" }, records[0].Definitions);
        Assert.Equal("my-list", records[0].Source);
        Assert.Null(records[0].Phonetic);
        Assert.Equal("water", records[1].Headword);
        Assert.Equal(new[] { "n. 水" }, records[1].Definitions);
    }

    [Fact]
    public void Parse_SkipsCommentsBlankAndMalformedLines()
    {
        var records = Parse(
            "# comment\n"
            + "\n"
            + "   \n"
            + "no separator line\n"
            + "headword only\t\n"
            + "\tdefinition only\n"
            + "apple\tn. 苹果\n");

        var record = Assert.Single(records);
        Assert.Equal("apple", record.Headword);
    }

    [Fact]
    public void Parse_KeepsCcCedictAndGenericLinesInOrder()
    {
        var records = Parse("苹果 苹果 [píng guǒ] /apple/\napple\tn. 苹果\n");

        Assert.Equal(2, records.Count);
        Assert.Equal("CC-CEDICT", records[0].Source);
        Assert.Equal("my-list", records[1].Source);
    }

    [Fact]
    public void Parse_CapsMergedSenses()
    {
        var lines = string.Concat(
            Enumerable.Range(0, DictionaryLineParser.MaxSensesPerEntry + 10)
                .Select(index => $"apple\tsense {index}\n"));

        var records = Parse(lines);

        var record = Assert.Single(records);
        Assert.Equal(DictionaryLineParser.MaxSensesPerEntry, record.Definitions.Count);
    }
}
