using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class EcdictParserTests
{
    private const string Header = "word,phonetic,definition,translation,pos,collins,oxford,tag,bnc,frq,exchange,detail,audio";

    [Fact]
    public void Parse_ReadsBasicRow()
    {
        var csv = Header + "\n"
            + "apple,/ˈæpl/,\"a round fruit\",\"n. 苹果\",n,,,,0,0,\"apples/apple/apples/apple\",,\n";

        var entries = EcdictParser.ParseText(csv);

        var entry = Assert.Single(entries);
        Assert.Equal("apple", entry.Word);
        Assert.Equal("/ˈæpl/", entry.Phonetic);
        Assert.Equal(new[] { "n. 苹果" }, entry.Senses);
        Assert.Equal("apples/apple/apples/apple", entry.Exchange);
        Assert.Equal("ECDICT", entry.Source);
    }

    [Fact]
    public void Parse_HandlesQuotedNewlinesInsideField()
    {
        var csv = Header + "\n"
            + "run,/rʌn/,\"to move quickly\",\"n. 奔跑\nv. 跑；经营\",v,,,,0,0,\"runs/running/ran/run\",,\n";

        var entries = EcdictParser.ParseText(csv);

        var entry = Assert.Single(entries);
        Assert.Equal("run", entry.Word);
        Assert.Equal(new[] { "n. 奔跑", "v. 跑；经营" }, entry.Senses);
    }

    [Fact]
    public void Parse_UsesEnglishDefinitionWhenTranslationIsEmpty()
    {
        var csv = Header + "\n"
            + "esolang,/ˈiːsoʊlæŋ/,\"esoteric programming language\",\"\",n,,,,0,0,,,\n";

        var entries = EcdictParser.ParseText(csv);

        var entry = Assert.Single(entries);
        Assert.Equal(new[] { "esoteric programming language" }, entry.Senses);
    }

    [Fact]
    public void Parse_SkipsBlankAndWordlessRows()
    {
        var csv = Header + "\n"
            + "apple,/ˈæpl/,,n. 苹果,,,,,,0,0,,,\n"
            + "\n"
            + ",,/x/,,translation with no word,,,,,,0,0,,,\n"
            + "water,/ˈwɔːtə/,,n. 水,,,,,,0,0,,,\n";

        var entries = EcdictParser.ParseText(csv);

        Assert.Equal(2, entries.Count);
        Assert.Equal("apple", entries[0].Word);
        Assert.Equal("water", entries[1].Word);
    }
}
