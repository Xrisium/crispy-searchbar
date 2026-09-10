using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class DictionaryFileFormatDetectorTests
{
    [Theory]
    [InlineData("dict.ifo")]
    [InlineData("DICT.IFO")]
    [InlineData(@"C:\dicts\My StarDict.ifo")]
    public void Detect_RecognizesStarDict(string path)
    {
        Assert.Equal(DictionaryFileFormat.StarDict, DictionaryFileFormatDetector.Detect(path));
        Assert.True(DictionaryFileFormatDetector.IsStarDictFile(path));
    }

    [Theory]
    [InlineData("cedict.txt", DictionaryFileFormat.Text)]
    [InlineData("CEDICT.TXT", DictionaryFileFormat.Text)]
    [InlineData("ecdict.csv", DictionaryFileFormat.Csv)]
    [InlineData("ECDICT.CSV", DictionaryFileFormat.Csv)]
    public void Detect_ClassifiesPlainFiles(string path, DictionaryFileFormat expected)
    {
        Assert.Equal(expected, DictionaryFileFormatDetector.Detect(path));
    }

    [Theory]
    [InlineData("cedict_1_0_ts_utf-8_mdbg.txt.gz", DictionaryFileFormat.Text)]
    [InlineData("ecdict.csv.GZ", DictionaryFileFormat.Csv)]
    [InlineData("data.txt.zip", DictionaryFileFormat.Text)]
    [InlineData("data.csv.ZIP", DictionaryFileFormat.Csv)]
    [InlineData("dict.ifo.gz", DictionaryFileFormat.StarDict)]
    public void Detect_StripsOneArchiveLayer(string path, DictionaryFileFormat expected)
    {
        Assert.Equal(expected, DictionaryFileFormatDetector.Detect(path));
    }

    [Theory]
    [InlineData("cedict_ts.u8")]
    [InlineData("something.bin")]
    [InlineData("cedict_1_0_ts_utf-8_mdbg.zip")]
    [InlineData("")]
    [InlineData(null)]
    public void Detect_UnknownOrExtensionlessArchives(string? path)
    {
        Assert.Equal(DictionaryFileFormat.Unknown, DictionaryFileFormatDetector.Detect(path));
    }

    [Fact]
    public void Detect_ArchivePathIsNotStarDictFile()
    {
        Assert.True(DictionaryFileFormatDetector.IsArchive("dict.ifo.gz"));
        Assert.False(DictionaryFileFormatDetector.IsStarDictFile("dict.ifo.gz"));
        Assert.True(DictionaryFileFormatDetector.IsStarDictFile("dict.ifo"));
    }

    [Fact]
    public void StripArchiveSuffix_RemovesSingleLayer()
    {
        Assert.Equal("data.txt", DictionaryFileFormatDetector.StripArchiveSuffix("data.txt.gz"));
        Assert.Equal("data.CSV", DictionaryFileFormatDetector.StripArchiveSuffix("data.CSV.zip"));
        Assert.Equal("data", DictionaryFileFormatDetector.StripArchiveSuffix("data"));
    }
}
