namespace CrispySearchbar.Core.Dictionary;

/// <summary>词典文件格式：按扩展名侦测；未知扩展名交给调用方按方向回退默认解析器。</summary>
public enum DictionaryFileFormat
{
    /// <summary>扩展名不在已知列表中，由调用方决定回退行为。</summary>
    Unknown,

    /// <summary>CC-CEDICT 文本（.u8/.txt 等）。</summary>
    CcCedict,

    /// <summary>ECDICT CSV。</summary>
    Ecdict,

    /// <summary>StarDict 词库（.ifo + .idx + .dict/.dict.dz）。</summary>
    StarDict,
}

/// <summary>按文件扩展名侦测词典格式，大小写不敏感。</summary>
public static class DictionaryFileFormatDetector
{
    public const string StarDictExtension = ".ifo";

    public static DictionaryFileFormat Detect(string? filePath)
    {
        var extension = Path.GetExtension(filePath ?? string.Empty);
        if (extension.Equals(StarDictExtension, StringComparison.OrdinalIgnoreCase))
        {
            return DictionaryFileFormat.StarDict;
        }

        return extension.ToLowerInvariant() switch
        {
            ".csv" => DictionaryFileFormat.Ecdict,
            ".u8" or ".txt" or ".utf8" => DictionaryFileFormat.CcCedict,
            _ => DictionaryFileFormat.Unknown,
        };
    }
}
