using System.Text;

namespace CrispySearchbar.Core.Dictionary;

/// <summary>StarDict 词库元数据与推导出的配套文件路径。</summary>
public sealed record StarDictInfo(
    string BookName,
    string? Description,
    string? SametypeSequence,
    int IdxOffsetBits,
    int WordCount,
    string IfoFilePath,
    string IdxFilePath,
    string DataFilePath,
    string? SynFilePath)
{
    /// <summary>展示用来源名：优先 .ifo 的 bookname，缺失时退回 .ifo 文件名。</summary>
    public string SourceName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(BookName))
            {
                return BookName.Trim();
            }

            var fileName = Path.GetFileNameWithoutExtension(IfoFilePath);
            return string.IsNullOrWhiteSpace(fileName) ? StarDictIfoParser.SourceName : fileName;
        }
    }
}

/// <summary>
/// StarDict .ifo 读取器：解析键值元数据，并按同目录同基名推导 .idx、.dict/.dict.dz 与可选 .syn。
/// </summary>
public static class StarDictIfoParser
{
    public const string SourceName = "StarDict";

    private const int DefaultIdxOffsetBits = 32;

    public static StarDictInfo Parse(string ifoFilePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ifoFilePath);
        if (!File.Exists(ifoFilePath))
        {
            throw new FileNotFoundException($"StarDict 词典文件不存在：{ifoFilePath}", ifoFilePath);
        }

        var values = ReadValues(ifoFilePath);
        var bookName = values.TryGetValue("bookname", out var name) ? name.Trim() : string.Empty;
        var description = values.TryGetValue("description", out var descriptionValue)
            ? descriptionValue.Trim()
            : null;
        var sametypeSequence = values.TryGetValue("sametypesequence", out var sequence)
            && !string.IsNullOrWhiteSpace(sequence)
                ? sequence.Trim()
                : null;
        var offsetBits = values.TryGetValue("idxoffsetbits", out var bitsValue)
            && int.TryParse(bitsValue.Trim(), out var bits) && bits == 64
                ? 64
                : DefaultIdxOffsetBits;
        var wordCount = values.TryGetValue("wordcount", out var countValue)
            && int.TryParse(countValue.Trim(), out var count) && count > 0
                ? count
                : 0;

        var fullIfoPath = Path.GetFullPath(ifoFilePath);
        var directory = Path.GetDirectoryName(fullIfoPath) ?? string.Empty;
        var basePath = Path.Combine(directory, Path.GetFileNameWithoutExtension(fullIfoPath));

        var idxPath = basePath + ".idx";
        var dataPath = File.Exists(basePath + ".dict.dz")
            ? basePath + ".dict.dz"
            : File.Exists(basePath + ".dict")
                ? basePath + ".dict"
                : null;
        var synPath = File.Exists(basePath + ".syn") ? basePath + ".syn" : null;

        if (!File.Exists(idxPath))
        {
            throw new FileNotFoundException($"StarDict 词典缺少索引文件：{idxPath}", idxPath);
        }

        if (dataPath is null)
        {
            var expected = basePath + ".dict(.dz)";
            throw new FileNotFoundException($"StarDict 词典缺少正文文件：{expected}", expected);
        }

        return new StarDictInfo(
            bookName,
            description,
            sametypeSequence,
            offsetBits,
            wordCount,
            fullIfoPath,
            idxPath,
            dataPath,
            synPath);
    }

    private static Dictionary<string, string> ReadValues(string path)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        using var reader = new StreamReader(
            path,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#')
            {
                continue;
            }

            var separator = trimmed.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }

            var key = trimmed[..separator].Trim();
            if (key.Length == 0 || values.ContainsKey(key))
            {
                continue;
            }

            values[key] = trimmed[(separator + 1)..];
        }

        return values;
    }
}
