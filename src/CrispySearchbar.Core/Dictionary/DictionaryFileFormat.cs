namespace CrispySearchbar.Core.Dictionary;

/// <summary>词典文件格式：按扩展名侦测；未知扩展名按行文本回退。</summary>
public enum DictionaryFileFormat
{
    /// <summary>扩展名不在已知列表中，按行文本回退解析。</summary>
    Unknown,

    /// <summary>行文本（.txt）：CC-CEDICT 行或“词头&lt;TAB&gt;释义”通用行。</summary>
    Text,

    /// <summary>CSV（.csv）：按表头分派 CC-CEDICT / ECDICT 方言。</summary>
    Csv,

    /// <summary>StarDict 词库（.ifo + .idx + .dict/.dict.dz）。</summary>
    StarDict,
}

/// <summary>
/// 按文件扩展名侦测词典格式，大小写不敏感；会先剥掉一层 .gz/.zip 压缩后缀。
/// 两个方向共用同一组格式，方向由配置槽位决定。
/// </summary>
public static class DictionaryFileFormatDetector
{
    public const string StarDictExtension = ".ifo";

    public const string CsvExtension = ".csv";

    public const string TextExtension = ".txt";

    public const string GzipExtension = ".gz";

    public const string ZipExtension = ".zip";

    /// <summary>
    /// MDBG 官方 zip 的内层文件名（cedict_ts.u8）使用的旧扩展名。
    /// 仅用于压缩包内层条目识别与旧配置兼容解析，不再作为对外宣传的可选格式。
    /// </summary>
    public const string LegacyCedictExtension = ".u8";

    public static DictionaryFileFormat Detect(string? filePath)
        => DetectFromName(StripArchiveSuffix(filePath));

    /// <summary>按文件名侦测格式，不做压缩后缀剥离（用于压缩包内层条目名）。</summary>
    public static DictionaryFileFormat DetectFromName(string? fileName)
    {
        var extension = Path.GetExtension(fileName ?? string.Empty);
        if (extension.Equals(StarDictExtension, StringComparison.OrdinalIgnoreCase))
        {
            return DictionaryFileFormat.StarDict;
        }

        if (extension.Equals(CsvExtension, StringComparison.OrdinalIgnoreCase))
        {
            return DictionaryFileFormat.Csv;
        }

        return extension.Equals(TextExtension, StringComparison.OrdinalIgnoreCase)
            ? DictionaryFileFormat.Text
            : DictionaryFileFormat.Unknown;
    }

    /// <summary>是否压缩容器（.gz/.zip，大小写不敏感）。</summary>
    public static bool IsArchive(string? filePath)
        => EndsWith(filePath, GzipExtension) || EndsWith(filePath, ZipExtension);

    public static bool IsGzip(string? filePath) => EndsWith(filePath, GzipExtension);

    public static bool IsZip(string? filePath) => EndsWith(filePath, ZipExtension);

    /// <summary>如果是配置路径本身（非压缩包）指向 .ifo，则为 StarDict 词库。</summary>
    public static bool IsStarDictFile(string? filePath)
        => !IsArchive(filePath) && DetectFromName(filePath) == DictionaryFileFormat.StarDict;

    /// <summary>剥掉一层 .gz/.zip 后缀，便于按内层文件名判格式与来源名。</summary>
    public static string StripArchiveSuffix(string? filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return filePath ?? string.Empty;
        }

        if (EndsWith(filePath, GzipExtension))
        {
            return filePath[..^GzipExtension.Length];
        }

        return EndsWith(filePath, ZipExtension)
            ? filePath[..^ZipExtension.Length]
            : filePath;
    }

    private static bool EndsWith(string? value, string suffix)
        => value is not null && value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase);
}
