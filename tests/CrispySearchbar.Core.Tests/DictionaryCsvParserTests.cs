using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class DictionaryCsvParserTests
{
    private static IReadOnlyList<DictionaryRecord> Parse(string csv)
    {
        using var reader = new StringReader(csv);
        return DictionaryCsvParser.Parse(reader, "CC-CEDICT", "ECDICT");
    }

    [Fact]
    public void Parse_DispatchesEcdictDialectByHeader()
    {
        var records = Parse(
            "word,phonetic,definition,translation,pos,exchange\n"
            + "apple,/ˈæpl/,\"a fruit\",\"n. 苹果\",n,\"apples/apple\"\n");

        var record = Assert.Single(records);
        Assert.Equal("apple", record.Headword);
        Assert.Equal("/ˈæpl/", record.Phonetic);
        Assert.Equal(new[] { "n. 苹果" }, record.Definitions);
        Assert.Equal("apples/apple", record.Exchange);
        Assert.Equal("ECDICT", record.Source);
    }

    [Fact]
    public void Parse_DispatchesCcCedictDialectByHeader()
    {
        var records = Parse(
            "pinyin,definitions,simplified,traditional,extra\n"
            + "píng guǒ,/apple/apple tree/,苹果,蘋果,ignored\n");

        var record = Assert.Single(records);
        Assert.Equal("苹果", record.Headword);
        Assert.Equal("蘋果", record.Traditional);
        Assert.Equal("píng guǒ", record.Pinyin);
        Assert.Equal(new[] { "apple", "apple tree" }, record.Definitions);
        Assert.Equal("CC-CEDICT", record.Source);
    }

    [Fact]
    public void Parse_HandlesQuotedNewlinesInEcdictDialect()
    {
        var records = Parse(
            "word,translation\n"
            + "run,\"n. 奔跑\nv. 跑\"\n");

        var record = Assert.Single(records);
        Assert.Equal(new[] { "n. 奔跑", "v. 跑" }, record.Definitions);
    }

    [Fact]
    public void Parse_SkipsRowsWithoutDefinitions()
    {
        var records = Parse(
            "word,translation\n"
            + "apple,n. 苹果\n"
            + "empty,\n");

        var record = Assert.Single(records);
        Assert.Equal("apple", record.Headword);
    }

    [Fact]
    public void Parse_ThrowsForUnknownHeader()
    {
        var exception = Assert.Throws<InvalidDataException>(() => Parse("alpha,beta\n1,2\n"));

        Assert.Contains("CSV 表头", exception.Message);
    }
}
