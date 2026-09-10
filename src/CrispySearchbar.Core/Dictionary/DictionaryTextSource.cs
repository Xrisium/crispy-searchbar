using System.IO.Compression;
using System.Text;

namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// 词典文本源：把配置路径（含一层 .gz/.zip 压缩）统一成 <see cref="TextReader"/> 与解析格式。
/// 调用方用 using 释放。
/// </summary>
public sealed class DictionaryTextSource : IDisposable
{
    /// <summary>压缩包内允许作为词典数据的条目扩展名（.u8 是 MDBG 官方 zip 的内层名）。</summary>
    private static readonly string[] SelectableEntryExtensions =
    [
        DictionaryFileFormatDetector.TextExtension,
        DictionaryFileFormatDetector.CsvExtension,
        DictionaryFileFormatDetector.LegacyCedictExtension,
    ];

    private readonly IDisposable? _owner;

    private DictionaryTextSource(
        TextReader reader,
        DictionaryFileFormat format,
        string sourceName,
        IDisposable? owner)
    {
        Reader = reader;
        Format = format;
        SourceName = sourceName;
        _owner = owner;
    }

    /// <summary>已按压缩层解开的内容读取器。</summary>
    public TextReader Reader { get; }

    /// <summary>解析格式：<see cref="DictionaryFileFormat.Text"/>（含 Unknown 回退）或 Csv。</summary>
    public DictionaryFileFormat Format { get; }

    /// <summary>通用行文本的来源名：配置文件名去掉压缩后缀与扩展名。</summary>
    public string SourceName { get; }

    public static DictionaryTextSource Open(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"词典文件不存在：{filePath}", filePath);
        }

        if (DictionaryFileFormatDetector.IsZip(filePath))
        {
            return OpenZip(filePath);
        }

        if (DictionaryFileFormatDetector.IsGzip(filePath))
        {
            var gzip = new GZipStream(File.OpenRead(filePath), CompressionMode.Decompress);
            return new DictionaryTextSource(
                new StreamReader(gzip, Encoding.UTF8, detectEncodingFromByteOrderMarks: true),
                DictionaryFileFormatDetector.Detect(filePath),
                GetSourceName(filePath),
                owner: null);
        }

        var stream = File.OpenRead(filePath);
        return new DictionaryTextSource(
            new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true),
            DictionaryFileFormatDetector.DetectFromName(filePath),
            GetSourceName(filePath),
            owner: null);
    }

    /// <summary>
    /// 从已打开的 gzip 流读取（例如内置词典的嵌入资源）；格式与来源名由调用方给出。
    /// 释放本实例会连带释放传入的流。
    /// </summary>
    public static DictionaryTextSource OpenGzip(
        Stream gzipStream,
        DictionaryFileFormat format,
        string sourceName)
    {
        ArgumentNullException.ThrowIfNull(gzipStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        var gzip = new GZipStream(gzipStream, CompressionMode.Decompress);
        return new DictionaryTextSource(
            new StreamReader(gzip, Encoding.UTF8, detectEncodingFromByteOrderMarks: true),
            format,
            sourceName,
            owner: null);
    }

    public void Dispose()
    {
        // 先释放读取器（连带其底层流），再释放压缩包句柄。
        Reader.Dispose();
        _owner?.Dispose();
    }

    private static DictionaryTextSource OpenZip(string filePath)
    {
        var archive = new ZipArchive(File.OpenRead(filePath), ZipArchiveMode.Read);
        var entry = SelectEntry(archive, filePath);
        var stream = entry.Open();
        return new DictionaryTextSource(
            new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true),
            DictionaryFileFormatDetector.DetectFromName(entry.FullName),
            GetSourceName(filePath),
            archive);
    }

    /// <summary>在压缩包内取未压缩体积最大的 .txt/.csv 条目（同名按序数排序兜底）。</summary>
    private static ZipArchiveEntry SelectEntry(ZipArchive archive, string filePath)
    {
        var candidates = archive.Entries
            .Where(entry => !string.IsNullOrEmpty(entry.Name) && IsSupportedEntry(entry.Name))
            .OrderByDescending(entry => entry.Length)
            .ThenBy(entry => entry.FullName, StringComparer.Ordinal)
            .ToArray();
        if (candidates.Length == 0)
        {
            throw new InvalidDataException($"压缩包内没有受支持的词典文件：{filePath}");
        }

        return candidates[0];
    }

    private static bool IsSupportedEntry(string entryName)
        => SelectableEntryExtensions.Contains(
            Path.GetExtension(entryName),
            StringComparer.OrdinalIgnoreCase);

    private static string GetSourceName(string filePath)
    {
        var inner = DictionaryFileFormatDetector.StripArchiveSuffix(filePath);
        var name = Path.GetFileNameWithoutExtension(inner);
        return string.IsNullOrWhiteSpace(name)
            ? DictionaryFileFormatDetector.StripArchiveSuffix(Path.GetFileName(filePath))
            : name;
    }
}
