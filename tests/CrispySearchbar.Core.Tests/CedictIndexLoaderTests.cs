using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class CedictIndexLoaderTests
{
    [Fact]
    public void LoadFile_ParsesAndIndexesWholeFile()
    {
        var directory = Path.Combine(Path.GetTempPath(), "crispy-searchbar-dict-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "cedict_ts.u8");
        try
        {
            File.WriteAllLines(path, new[]
            {
                "# comment",
                "蘋果 苹果 [píng guǒ] /apple/",
                "你好 你好 [nǐ hǎo] /hello/",
            });

            var index = CedictIndexLoader.LoadFile(path);

            Assert.Equal(2, index.Count);
            Assert.Equal("苹果", index.Search("苹果")[0].Simplified);
            Assert.Empty(index.Search("apple"));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadFile_MissingFile_ThrowsFileNotFound()
    {
        var missing = Path.Combine(Path.GetTempPath(), "definitely-missing-" + Guid.NewGuid().ToString("N") + ".u8");

        Assert.Throws<FileNotFoundException>(() => CedictIndexLoader.LoadFile(missing));
    }
}
