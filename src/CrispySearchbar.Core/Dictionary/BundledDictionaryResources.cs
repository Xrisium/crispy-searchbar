using System.Reflection;

namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// 内置词典数据：两份 gzip 文本以嵌入资源随 <c>CrispySearchbar.Core</c> 程序集分发，
/// 单文件发布时不需要任何外附数据文件。
/// 解析格式与来源名由资源文件名推导（保持 <c>.txt.gz</c> / <c>.csv.gz</c> 后缀）。
/// </summary>
public static class BundledDictionaryResources
{
    /// <summary>CC-CEDICT（汉 → 英）嵌入资源逻辑名。</summary>
    public const string CcCedictResourceName =
        "CrispySearchbar.Core.Data.cedict_1_0_ts_utf-8_mdbg.txt.gz";

    /// <summary>ECDICT（英 → 汉）嵌入资源逻辑名。</summary>
    public const string EcdictResourceName =
        "CrispySearchbar.Core.Data.ecdict.csv.gz";

    /// <summary>内置 CC-CEDICT 的原始文件名（用户数据目录/程序目录覆盖层用它查找）。</summary>
    public const string CcCedictFileName = "cedict_1_0_ts_utf-8_mdbg.txt";

    /// <summary>内置 ECDICT 的原始文件名（用户数据目录/程序目录覆盖层用它查找）。</summary>
    public const string EcdictFileName = "ecdict.csv";

    /// <summary>内置数据的 gzip 文件名（用户数据目录/程序目录覆盖层也接受该形式）。</summary>
    public const string GzipExtension = ".gz";

    private static readonly Assembly ResourceAssembly = typeof(BundledDictionaryResources).Assembly;

    /// <summary>CC-CEDICT（汉 → 英）资源元数据。</summary>
    public static BundledDictionaryResource CcCedict { get; } = new(
        CcCedictResourceName,
        CcCedictFileName);

    /// <summary>ECDICT（英 → 汉）资源元数据。</summary>
    public static BundledDictionaryResource Ecdict { get; } = new(
        EcdictResourceName,
        EcdictFileName);

    /// <summary>内嵌资源是否存在。</summary>
    public static bool Exists(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        return Array.IndexOf(ResourceAssembly.GetManifestResourceNames(), resourceName) >= 0;
    }

    /// <summary>打开内嵌 CC-CEDICT 的 gzip 流；资源缺失时返回 null。调用方负责释放。</summary>
    public static Stream? TryOpenCcCedict() => CcCedict.TryOpen();

    /// <summary>打开内嵌 ECDICT 的 gzip 流；资源缺失时返回 null。调用方负责释放。</summary>
    public static Stream? TryOpenEcdict() => Ecdict.TryOpen();

    /// <summary>打开指定逻辑名的内嵌资源流；资源缺失时返回 null。调用方负责释放。</summary>
    public static Stream? TryOpen(string resourceName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceName);
        return ResourceAssembly.GetManifestResourceStream(resourceName);
    }
}

/// <summary>一份内置词典资源：逻辑名、外部覆盖层使用的文件名，以及由文件名推导的解析元数据。</summary>
public sealed record BundledDictionaryResource(string ResourceName, string FileName)
{
    /// <summary>解析格式（先剥掉 .gz 后缀）。</summary>
    public DictionaryFileFormat Format => DictionaryFileFormatDetector.Detect(FileName);

    /// <summary>通用行文本解析时的来源标识：文件名去掉压缩后缀与扩展名。</summary>
    public string SourceName => Path.GetFileNameWithoutExtension(
        DictionaryFileFormatDetector.StripArchiveSuffix(FileName));

    /// <summary>用户数据目录/程序目录覆盖层接受的文件名：原始名优先，其次同名 .gz。</summary>
    public IReadOnlyList<string> ExternalFileNames { get; } =
        [FileName, FileName + BundledDictionaryResources.GzipExtension];

    /// <summary>内嵌资源是否存在。</summary>
    public bool Exists => BundledDictionaryResources.Exists(ResourceName);

    /// <summary>打开内嵌 gzip 流；资源缺失时返回 null。调用方负责释放。</summary>
    public Stream? TryOpen() => BundledDictionaryResources.TryOpen(ResourceName);
}
