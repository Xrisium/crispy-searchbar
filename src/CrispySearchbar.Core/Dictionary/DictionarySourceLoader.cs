namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// 词典数据源统一入口：两个方向共用同一组格式，方向由调用方（配置槽位）决定。
/// 扩展名决定解析器（.ifo → StarDict；.csv → 按表头分派；其余按行文本），
/// .gz/.zip 先解压一层；同一份文件在任一方向都能解析。
/// </summary>
public static class DictionarySourceLoader
{
    public const string CcCedictSourceName = "CC-CEDICT";

    public const string EcdictSourceName = "ECDICT";

    public static CedictIndex LoadChineseIndex(string filePath)
        => CedictIndex.Build(LoadChineseEntries(filePath));

    public static EcdictIndex LoadEnglishIndex(string filePath)
        => EcdictIndex.Build(LoadEnglishEntries(filePath));

    public static IReadOnlyList<DictionaryEntry> LoadChineseEntries(string filePath)
        => LoadRecords(filePath).Select(DictionaryRecordMapper.ToDictionaryEntry).ToList();

    public static IReadOnlyList<EcdictEntry> LoadEnglishEntries(string filePath)
        => LoadRecords(filePath).Select(DictionaryRecordMapper.ToEcdictEntry).ToList();

    public static IReadOnlyList<DictionaryRecord> LoadRecords(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (DictionaryFileFormatDetector.Detect(filePath) == DictionaryFileFormat.StarDict)
        {
            if (DictionaryFileFormatDetector.IsArchive(filePath))
            {
                throw new NotSupportedException(
                    $"StarDict 词典不支持压缩包，请直接选择 .ifo 文件：{filePath}");
            }

            return StarDictDictionaryLoader.BuildRecords(filePath);
        }

        using var source = DictionaryTextSource.Open(filePath);
        return source.Format == DictionaryFileFormat.Csv
            ? DictionaryCsvParser.Parse(
                source.Reader,
                CcCedictSourceName,
                EcdictSourceName)
            : DictionaryLineParser.Parse(source.Reader, CcCedictSourceName, source.SourceName);
    }

    /// <summary>
    /// 从 gzip 流（内置词典的嵌入资源）加载：格式与来源名由调用方给出，
    /// 解析链路与文件入口完全一致。
    /// </summary>
    public static CedictIndex LoadChineseIndex(
        Stream gzipStream,
        DictionaryFileFormat format,
        string sourceName)
        => CedictIndex.Build(LoadChineseEntries(gzipStream, format, sourceName));

    /// <summary>从 gzip 流（内置词典的嵌入资源）加载英 → 汉索引。</summary>
    public static EcdictIndex LoadEnglishIndex(
        Stream gzipStream,
        DictionaryFileFormat format,
        string sourceName)
        => EcdictIndex.Build(LoadEnglishEntries(gzipStream, format, sourceName));

    /// <summary>从 gzip 流加载汉英词条。</summary>
    public static IReadOnlyList<DictionaryEntry> LoadChineseEntries(
        Stream gzipStream,
        DictionaryFileFormat format,
        string sourceName)
        => LoadRecords(gzipStream, format, sourceName)
            .Select(DictionaryRecordMapper.ToDictionaryEntry)
            .ToList();

    /// <summary>从 gzip 流加载英汉词条。</summary>
    public static IReadOnlyList<EcdictEntry> LoadEnglishEntries(
        Stream gzipStream,
        DictionaryFileFormat format,
        string sourceName)
        => LoadRecords(gzipStream, format, sourceName)
            .Select(DictionaryRecordMapper.ToEcdictEntry)
            .ToList();

    /// <summary>从 gzip 流加载方向无关的词条记录。</summary>
    public static IReadOnlyList<DictionaryRecord> LoadRecords(
        Stream gzipStream,
        DictionaryFileFormat format,
        string sourceName)
    {
        ArgumentNullException.ThrowIfNull(gzipStream);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

        using var source = DictionaryTextSource.OpenGzip(gzipStream, format, sourceName);
        return source.Format == DictionaryFileFormat.Csv
            ? DictionaryCsvParser.Parse(
                source.Reader,
                CcCedictSourceName,
                EcdictSourceName)
            : DictionaryLineParser.Parse(source.Reader, CcCedictSourceName, source.SourceName);
    }
}
