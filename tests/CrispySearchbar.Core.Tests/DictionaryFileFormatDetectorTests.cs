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
    }

    [Theory]
    [InlineData("cedict_ts.u8", DictionaryFileFormat.CcCedict)]
    [InlineData("CEDICT.TXT", DictionaryFileFormat.CcCedict)]
    [InlineData("ecdict.csv", DictionaryFileFormat.Ecdict)]
    [InlineData("ECDICT.CSV", DictionaryFileFormat.Ecdict)]
    [InlineData("something.bin", DictionaryFileFormat.Unknown)]
    [InlineData("", DictionaryFileFormat.Unknown)]
    [InlineData(null, DictionaryFileFormat.Unknown)]
    public void Detect_ClassifiesOtherExtensions(string? path, DictionaryFileFormat expected)
    {
        Assert.Equal(expected, DictionaryFileFormatDetector.Detect(path));
    }
}
