using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class EcdictIndexLoaderTests
{
    [Fact]
    public void LoadFile_ParsesAndIndexesWholeFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "crispy-searchbar-ecdict-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "ecdict.csv");
        try
        {
            File.WriteAllText(path, "word,phonetic,definition,translation,pos,collins,oxford,tag,bnc,frq,exchange,detail,audio\n"
                + "apple,/ˈæpl/,\"a fruit\",\"n. 苹果\",n,,,,0,0,,,\n"
                + "water,/ˈwɔːtə/,\"liquid\",\"n. 水\",n,,,,0,0,,,\n");

            var index = EcdictIndexLoader.LoadFile(path);

            Assert.Equal(2, index.Count);
            Assert.Equal("apple", index.Search("apple")[0].Word);
            Assert.Contains(index.Search("water")[0].Senses, sense => sense.Contains("水"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadFile_MissingFile_ThrowsFileNotFound()
    {
        var missing = Path.Combine(Path.GetTempPath(), "definitely-missing-" + Guid.NewGuid().ToString("N") + ".csv");

        Assert.Throws<FileNotFoundException>(() => EcdictIndexLoader.LoadFile(missing));
    }
}

