using System.IO.Compression;
using System.Text;
using CrispySearchbar.Core.Dictionary;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Dictionary;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// 内置词典改为随程序集内嵌后的解析优先级：
/// settings.json 指定路径 &gt; 用户数据目录 &gt; 程序目录（原始名或 .gz）&gt; 内嵌数据。
/// </summary>
public sealed class AppDictionaryResourcesTests : IDisposable
{
    private const string UserCedictLine = "苹果 苹果 [ping2 guo3] /apple from user data/";
    private const string ProgramCedictLine = "苹果 苹果 [ping2 guo3] /apple from program directory/";
    private const string UserEcdictDefinition = "from user data";
    private const string ProgramEcdictDefinition = "from program directory";

    private readonly AppStrings _strings =
        TranslationCatalog.Default.Resolve(AppLanguage.English);
    private readonly string _userDataDirectory;
    private readonly string _programDirectory;

    public AppDictionaryResourcesTests()
    {
        _userDataDirectory = CreateTemporaryDirectory("user");
        _programDirectory = CreateTemporaryDirectory("program");
    }

    public void Dispose()
    {
        Directory.Delete(_userDataDirectory, recursive: true);
        Directory.Delete(_programDirectory, recursive: true);
    }

    [Fact]
    public async Task UserDataDirectory_TakesPrecedenceOverProgramDirectory()
    {
        WriteCcCedict(_userDataDirectory, UserCedictLine);
        WriteEcdict(_userDataDirectory, UserEcdictDefinition, compress: false);
        WriteCcCedict(_programDirectory, ProgramCedictLine);
        WriteEcdict(_programDirectory, ProgramEcdictDefinition, compress: false);

        var result = await CreateResources().LoadAsync(CancellationToken.None);

        Assert.Equal(
            "apple from user data",
            result.CedictIndex!.Search("苹果")[0].Definitions[0]);
        Assert.Equal(
            UserEcdictDefinition,
            result.EcdictIndex!.Search("apple")[0].Senses[0]);
    }

    [Fact]
    public async Task ProgramDirectory_IsUsedWhenUserDataDirectoryHasNoFile()
    {
        WriteCcCedict(_programDirectory, ProgramCedictLine);
        WriteEcdict(_programDirectory, ProgramEcdictDefinition, compress: false);

        var result = await CreateResources().LoadAsync(CancellationToken.None);

        Assert.Equal(
            "apple from program directory",
            result.CedictIndex!.Search("苹果")[0].Definitions[0]);
        Assert.Equal(
            ProgramEcdictDefinition,
            result.EcdictIndex!.Search("apple")[0].Senses[0]);
    }

    [Fact]
    public async Task GzipFileName_IsAcceptedInOverrideDirectories()
    {
        WriteCcCedict(_userDataDirectory, UserCedictLine, compress: true);
        WriteEcdict(_userDataDirectory, UserEcdictDefinition, compress: true);

        var result = await CreateResources().LoadAsync(CancellationToken.None);

        Assert.Equal(
            "apple from user data",
            result.CedictIndex!.Search("苹果")[0].Definitions[0]);
        Assert.Equal(
            UserEcdictDefinition,
            result.EcdictIndex!.Search("apple")[0].Senses[0]);
    }

    [Fact]
    public async Task ConfiguredPathMissing_ReportsLocalizedMessage()
    {
        var missing = Path.Combine(_userDataDirectory, "does-not-exist.txt");
        var result = await new AppDictionaryResources(
                _strings,
                missing,
                null,
                _userDataDirectory,
                _programDirectory)
            .LoadAsync(CancellationToken.None);

        Assert.Null(result.CedictIndex);
        Assert.NotNull(result.CedictMessage);
        Assert.Contains(missing, result.CedictMessage!, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>没有任何外部文件时（单文件便携版的实际形态）必须直接使用内嵌数据。</summary>
    [Fact]
    public async Task WithoutAnyExternalFile_UsesEmbeddedData()
    {
        var result = await CreateResources().LoadAsync(CancellationToken.None);

        Assert.Null(result.CedictMessage);
        Assert.Null(result.EcdictMessage);
        Assert.True(result.CedictIndex!.Count >= 124_000);
        Assert.Equal("苹果", result.CedictIndex.Search("苹果")[0].Simplified);
        Assert.True(result.EcdictIndex!.Count >= 700_000);
        Assert.Equal("apple", result.EcdictIndex.Search("apple")[0].Word);
    }

    private AppDictionaryResources CreateResources()
        => new(
            _strings,
            cedictFilePath: null,
            ecdictFilePath: null,
            _userDataDirectory,
            _programDirectory);

    private static string CreateTemporaryDirectory(string name)
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            $"crispy-resources-{name}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteCcCedict(string directory, string line, bool compress = false)
    {
        var fileName = BundledDictionaryResources.CcCedictFileName;
        var content = $"""
            #! traditional simplified [pinyin] /definition/
            {line}
            """;
        WriteFile(
            Path.Combine(directory, compress ? fileName + ".gz" : fileName),
            content,
            compress);
    }

    private static void WriteEcdict(string directory, string translation, bool compress)
    {
        var fileName = BundledDictionaryResources.EcdictFileName;
        var content = $"""
            word,phonetic,translation,definition,exchange
            apple,ˈæpl,{translation},a fruit,
            """;
        WriteFile(
            Path.Combine(directory, compress ? fileName + ".gz" : fileName),
            content,
            compress);
    }

    private static void WriteFile(string path, string content, bool compress)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        if (!compress)
        {
            File.WriteAllBytes(path, bytes);
            return;
        }

        using var file = File.Create(path);
        using var gzip = new GZipStream(file, CompressionLevel.Optimal);
        gzip.Write(bytes);
    }
}
