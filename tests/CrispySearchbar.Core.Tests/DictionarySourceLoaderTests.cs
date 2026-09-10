using System.IO.Compression;
using System.Text;
using CrispySearchbar.Core.Dictionary;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class DictionarySourceLoaderTests
{
    private const string CedictText = "蘋果 苹果 [píng guǒ] /apple/\n";

    private const string EcdictCsv =
        "word,phonetic,definition,translation,exchange\n"
        + "apple,/ˈæpl/,\"a fruit\",\"n. 苹果\",\"apples/apple\"\n";

    [Fact]
    public void LoadEnglishEntries_AcceptsEcdictCsv()
    {
        using var file = TempFile.Create(".csv");
        File.WriteAllText(file.Path, EcdictCsv);

        var entries = DictionarySourceLoader.LoadEnglishEntries(file.Path);

        var entry = Assert.Single(entries);
        Assert.Equal("apple", entry.Word);
        Assert.Equal("/ˈæpl/", entry.Phonetic);
        Assert.Equal(new[] { "n. 苹果" }, entry.Senses);
        Assert.Equal("ECDICT", entry.Source);
    }

    [Fact]
    public void LoadChineseEntries_AcceptsTheSameEcdictCsv()
    {
        // 格式支持对称：同一份 CSV 放进汉英槽也能解析，只是映射成汉英词条。
        using var file = TempFile.Create(".csv");
        File.WriteAllText(file.Path, EcdictCsv);

        var entries = DictionarySourceLoader.LoadChineseEntries(file.Path);

        var entry = Assert.Single(entries);
        Assert.Equal("apple", entry.Simplified);
        Assert.Equal("apple", entry.Traditional);
        Assert.Equal(string.Empty, entry.Pinyin);
        Assert.Equal(new[] { "n. 苹果" }, entry.Definitions);
    }

    [Fact]
    public void LoadEnglishEntries_AcceptsGenericLineText()
    {
        using var file = TempFile.Create(".txt");
        File.WriteAllText(file.Path, "apple\tn. 苹果\n");

        var entries = DictionarySourceLoader.LoadEnglishEntries(file.Path);

        var entry = Assert.Single(entries);
        Assert.Equal("apple", entry.Word);
        Assert.Equal(new[] { "n. 苹果" }, entry.Senses);
        Assert.Null(entry.Phonetic);
    }

    [Fact]
    public void LoadChineseIndex_AcceptsCcCedictText()
    {
        using var file = TempFile.Create(".txt");
        File.WriteAllText(file.Path, CedictText);

        var index = DictionarySourceLoader.LoadChineseIndex(file.Path);

        var entry = index.Search("苹果")[0];
        Assert.Equal("píng guǒ", entry.Pinyin);
        Assert.Equal("CC-CEDICT", entry.Source);
    }

    [Fact]
    public void LoadChineseEntries_StillAcceptsLegacyU8Files()
    {
        // .u8 软移除：不再内置/不再列进选择器，但旧配置仍按行文本解析。
        using var file = TempFile.Create(".u8");
        File.WriteAllText(file.Path, CedictText);

        var entries = DictionarySourceLoader.LoadChineseEntries(file.Path);

        var entry = Assert.Single(entries);
        Assert.Equal("苹果", entry.Simplified);
    }

    [Fact]
    public void LoadEnglishEntries_ReadsGzipArchive()
    {
        using var file = TempFile.Create(".csv.gz");
        WriteGzip(file.Path, EcdictCsv);

        var entries = DictionarySourceLoader.LoadEnglishEntries(file.Path);

        Assert.Equal("apple", Assert.Single(entries).Word);
    }

    [Fact]
    public void LoadChineseEntries_ReadsGzipArchive()
    {
        using var file = TempFile.Create(".txt.gz");
        WriteGzip(file.Path, CedictText);

        var entries = DictionarySourceLoader.LoadChineseEntries(file.Path);

        Assert.Equal("苹果", Assert.Single(entries).Simplified);
    }

    [Fact]
    public void LoadEnglishEntries_ReadsZipArchiveWithNamedEntry()
    {
        using var file = TempFile.Create(".zip");
        WriteZip(file.Path, ("readme.md", "not a dictionary"), ("data.csv", EcdictCsv));

        var entries = DictionarySourceLoader.LoadEnglishEntries(file.Path);

        Assert.Equal("apple", Assert.Single(entries).Word);
    }

    [Fact]
    public void LoadChineseEntries_ReadsOfficialZipLayout()
    {
        // MDBG 官方 cedict_1_0_ts_utf-8_mdbg.zip 内层条目就叫 cedict_ts.u8。
        using var file = TempFile.Create(".zip");
        WriteZip(file.Path, ("cedict_ts.u8", CedictText));

        var entries = DictionarySourceLoader.LoadChineseEntries(file.Path);

        Assert.Equal("苹果", Assert.Single(entries).Simplified);
    }

    [Fact]
    public void LoadEnglishEntries_ZipWithoutSupportedEntry_Throws()
    {
        using var file = TempFile.Create(".zip");
        WriteZip(file.Path, ("readme.md", "not a dictionary"));

        var exception = Assert.Throws<InvalidDataException>(
            () => DictionarySourceLoader.LoadEnglishEntries(file.Path));

        Assert.Contains("压缩包", exception.Message);
    }

    [Fact]
    public void LoadEnglishEntries_CompressedStarDict_Throws()
    {
        using var file = TempFile.Create(".ifo.gz");
        WriteGzip(file.Path, "StarDict's dict ifo file\n");

        Assert.Throws<NotSupportedException>(
            () => DictionarySourceLoader.LoadEnglishEntries(file.Path));
    }

    [Fact]
    public void LoadEnglishEntries_MissingFile_ThrowsFileNotFound()
    {
        var missing = Path.Combine(
            Path.GetTempPath(),
            "definitely-missing-" + Guid.NewGuid().ToString("N") + ".csv");

        Assert.Throws<FileNotFoundException>(
            () => DictionarySourceLoader.LoadEnglishEntries(missing));
    }

    private static void WriteGzip(string path, string content)
    {
        using var file = File.Create(path);
        using var gzip = new GZipStream(file, CompressionLevel.Optimal);
        gzip.Write(Encoding.UTF8.GetBytes(content));
    }

    private static void WriteZip(string path, params (string Name, string Content)[] entries)
    {
        using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
        foreach (var (name, content) in entries)
        {
            var entry = archive.CreateEntry(name);
            using var stream = entry.Open();
            stream.Write(Encoding.UTF8.GetBytes(content));
        }
    }

    /// <summary>临时文件夹具：用给定扩展名建文件，Dispose 时删除。</summary>
    private sealed class TempFile : IDisposable
    {
        private readonly string _directory;

        private TempFile(string directory, string path)
        {
            _directory = directory;
            Path = path;
        }

        public string Path { get; }

        public static TempFile Create(string extension)
        {
            var directory = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "crispy-searchbar-source-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return new TempFile(
                directory,
                System.IO.Path.Combine(directory, "dictionary" + extension));
        }

        public void Dispose()
        {
            try
            {
                Directory.Delete(_directory, recursive: true);
            }
            catch (IOException)
            {
                // 临时目录清理失败不影响测试结果。
            }
        }
    }
}
