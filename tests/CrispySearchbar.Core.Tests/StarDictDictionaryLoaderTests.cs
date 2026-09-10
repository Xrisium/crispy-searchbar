using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class StarDictDictionaryLoaderTests
{
    private const string TaggedIfo = """
        StarDict's dict ifo file
        version=2.4.2
        bookname=Test Dict
        wordcount=2
        idxfilesize=0
        """;

    [Fact]
    public void LoadChineseIndex_MapsHeadwordPinyinAndDefinitions()
    {
        using var fixture = StarDictFixture.Create();
        fixture.WriteIfo(TaggedIfo);
        fixture.WriteEntries(
        [
            ("苹果", StarDictFixture.TaggedPayload(
                ('y', "píng guǒ"),
                ('m', "apple\napple tree"))),
            ("水", StarDictFixture.TaggedPayload(('m', "water"))),
        ]);

        var index = StarDictDictionaryLoader.LoadChineseIndex(fixture.IfoPath);

        var entry = index.Search("苹果")[0];
        Assert.Equal("苹果", entry.Simplified);
        Assert.Equal("苹果", entry.Traditional);
        Assert.Equal("píng guǒ", entry.Pinyin);
        Assert.Equal(new[] { "apple", "apple tree" }, entry.Definitions);
        Assert.Equal("Test Dict", entry.Source);
    }

    [Fact]
    public void LoadEnglishIndex_MapsWordPhoneticAndSenses()
    {
        using var fixture = StarDictFixture.Create();
        fixture.WriteIfo(TaggedIfo);
        fixture.WriteEntries(
        [
            ("apple", StarDictFixture.TaggedPayload(
                ('t', "/ˈæpl/"),
                ('m', "n. 苹果\na fruit"))),
            ("water", StarDictFixture.TaggedPayload(('m', "n. 水"))),
        ]);

        var index = StarDictDictionaryLoader.LoadEnglishIndex(fixture.IfoPath);

        var entry = index.Search("APPLE")[0];
        Assert.Equal("apple", entry.Word);
        Assert.Equal("/ˈæpl/", entry.Phonetic);
        Assert.Equal(new[] { "n. 苹果", "a fruit" }, entry.Senses);
        Assert.Equal("Test Dict", entry.Source);
    }

    [Fact]
    public void LoadEnglishIndex_StripsHtmlMarkupAndEntities()
    {
        using var fixture = StarDictFixture.Create();
        fixture.WriteIfo(TaggedIfo);
        fixture.WriteEntries(
        [
            ("apple", StarDictFixture.TaggedPayload(
                ('m', "<b>apple</b><br/>a fruit &amp; more"))),
        ]);

        var index = StarDictDictionaryLoader.LoadEnglishIndex(fixture.IfoPath);

        var entry = index.Search("apple")[0];
        Assert.Equal(new[] { "apple", "a fruit & more" }, entry.Senses);
    }

    [Fact]
    public void LoadChineseIndex_IncludesSynonymHeadwords()
    {
        using var fixture = StarDictFixture.Create();
        fixture.WriteIfo(TaggedIfo);
        fixture.WriteEntries(
            [("苹果", StarDictFixture.TaggedPayload(('m', "apple")))],
            synonyms: [("pingguo", 0)]);

        var index = StarDictDictionaryLoader.LoadChineseIndex(fixture.IfoPath);

        Assert.Equal(2, index.Count);
        var synonym = index.Search("pingguo")[0];
        Assert.Equal("pingguo", synonym.Simplified);
        Assert.Equal(new[] { "apple" }, synonym.Definitions);
    }

    [Fact]
    public void LoadEnglishIndex_SupportsSametypeSequence()
    {
        using var fixture = StarDictFixture.Create();
        fixture.WriteIfo("""
            StarDict's dict ifo file
            version=2.4.2
            bookname=Sequenced Dict
            sametypesequence=tm
            """);
        fixture.WriteEntries(
        [
            ("apple", StarDictFixture.SequencedPayload("tm", "/ˈæpl/", "n. 苹果")),
        ]);

        var index = StarDictDictionaryLoader.LoadEnglishIndex(fixture.IfoPath);

        var entry = index.Search("apple")[0];
        Assert.Equal("/ˈæpl/", entry.Phonetic);
        Assert.Equal(new[] { "n. 苹果" }, entry.Senses);
        Assert.Equal("Sequenced Dict", entry.Source);
    }

    [Fact]
    public void LoadEnglishIndex_Supports64BitIndexOffsets()
    {
        using var fixture = StarDictFixture.Create();
        fixture.WriteIfo("""
            StarDict's dict ifo file
            version=3.0.0
            bookname=64bit Dict
            idxoffsetbits=64
            """);
        fixture.WriteEntries(
        [
            ("apple", StarDictFixture.TaggedPayload(('m', "n. 苹果"))),
            ("water", StarDictFixture.TaggedPayload(('m', "n. 水"))),
        ], offsetBits: 64);

        var index = StarDictDictionaryLoader.LoadEnglishIndex(fixture.IfoPath);

        Assert.Equal(2, index.Count);
        Assert.Equal("n. 水", index.Search("water")[0].Senses[0]);
    }

    [Fact]
    public void LoadEnglishIndex_SupportsCompressedDataFile()
    {
        using var fixture = StarDictFixture.Create();
        fixture.WriteIfo(TaggedIfo);
        fixture.WriteEntries(
        [
            ("apple", StarDictFixture.TaggedPayload(('m', "n. 苹果"))),
        ], compressed: true);
        Assert.True(File.Exists(fixture.DictDzPath));

        var index = StarDictDictionaryLoader.LoadEnglishIndex(fixture.IfoPath);

        Assert.Equal("apple", index.Search("apple")[0].Word);
    }

    [Fact]
    public void LoadEnglishIndex_CompressedDataWinsOverPlainDataFile()
    {
        using var fixture = StarDictFixture.Create();
        fixture.WriteIfo(TaggedIfo);
        fixture.WriteEntries(
        [
            ("stale", StarDictFixture.TaggedPayload(('m', "stale entry"))),
        ]);
        fixture.WriteEntries(
        [
            ("fresh", StarDictFixture.TaggedPayload(('m', "fresh entry"))),
        ], compressed: true);

        var index = StarDictDictionaryLoader.LoadEnglishIndex(fixture.IfoPath);

        Assert.Equal("fresh", index.Search("fresh")[0].Word);
        Assert.Empty(index.Search("stale"));
    }

    [Fact]
    public void LoadEnglishIndex_SkipsEntriesWithoutDefinitions()
    {
        using var fixture = StarDictFixture.Create();
        fixture.WriteIfo(TaggedIfo);
        fixture.WriteEntries(
        [
            ("empty", StarDictFixture.TaggedPayload(('t', "/ˈɛmpti/"))),
            ("apple", StarDictFixture.TaggedPayload(('m', "n. 苹果"))),
        ]);

        var index = StarDictDictionaryLoader.LoadEnglishIndex(fixture.IfoPath);

        Assert.Equal(1, index.Count);
        Assert.Empty(index.Search("empty"));
    }

    [Fact]
    public void Parse_MissingIndexFile_ThrowsFileNotFound()
    {
        using var fixture = StarDictFixture.Create();
        fixture.WriteIfo(TaggedIfo);

        var exception = Assert.Throws<FileNotFoundException>(
            () => StarDictDictionaryLoader.LoadEnglishIndex(fixture.IfoPath));

        Assert.Equal(fixture.IdxPath, exception.FileName);
    }

    [Fact]
    public void Parse_MissingDataFile_ThrowsFileNotFound()
    {
        using var fixture = StarDictFixture.Create();
        fixture.WriteIfo(TaggedIfo);
        File.WriteAllBytes(fixture.IdxPath, []);

        var exception = Assert.Throws<FileNotFoundException>(
            () => StarDictDictionaryLoader.LoadEnglishIndex(fixture.IfoPath));

        Assert.Contains(".dict", exception.FileName);
    }

    [Fact]
    public void Parse_MissingIfoFile_ThrowsFileNotFound()
    {
        var missing = Path.Combine(
            Path.GetTempPath(),
            "definitely-missing-" + Guid.NewGuid().ToString("N") + ".ifo");

        Assert.Throws<FileNotFoundException>(
            () => StarDictDictionaryLoader.LoadEnglishIndex(missing));
    }
}
