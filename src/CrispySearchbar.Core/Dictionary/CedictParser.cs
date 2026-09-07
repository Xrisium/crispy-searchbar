namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// CC-CEDICT 文本行解析器。
/// 标准行格式：<c>繁体 简体 [拼音1 拼音2] /释义1/释义2/</c>。
/// </summary>
public static class CedictParser
{
    public const string SourceName = "CC-CEDICT";

    /// <summary>逐行解析；注释、空行与格式异常的行会被跳过。</summary>
    public static IReadOnlyList<DictionaryEntry> ParseLines(IEnumerable<string> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var entries = new List<DictionaryEntry>();
        foreach (var line in lines)
        {
            if (TryParse(line) is { } entry)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    /// <summary>解析单行；不是有效 CC-CEDICT 词条时返回 null。</summary>
    public static DictionaryEntry? TryParse(string? line)
    {
        if (line is null)
        {
            return null;
        }

        var text = line.Trim();
        if (text.Length == 0 || text[0] == '#' || text[0] == '%')
        {
            return null;
        }

        var bracketStart = text.IndexOf('[');
        var bracketEnd = text.IndexOf(']', bracketStart + 1);
        if (bracketStart < 0 || bracketEnd < bracketStart)
        {
            return null;
        }

        var head = text[..bracketStart].Trim();
        var spaceIndex = head.IndexOf(' ');
        if (spaceIndex <= 0 || spaceIndex == head.Length - 1)
        {
            return null;
        }

        var traditional = head[..spaceIndex].Trim();
        var simplified = head[(spaceIndex + 1)..].Trim();
        if (traditional.Length == 0 || simplified.Length == 0)
        {
            return null;
        }

        var pinyin = text[(bracketStart + 1)..bracketEnd].Trim();
        var definitionPart = text[(bracketEnd + 1)..].Trim();
        if (definitionPart.Length < 3 || definitionPart[0] != '/')
        {
            return null;
        }

        var definitions = definitionPart
            .Trim('/')
            .Split('/')
            .Where(definition => definition.Length > 0)
            .ToArray();
        if (definitions.Length == 0)
        {
            return null;
        }

        return new DictionaryEntry(traditional, simplified, pinyin, definitions);
    }
}
