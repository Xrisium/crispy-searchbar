namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// .txt 行解析器：两个方向共用。逐行识别两种写法——
/// CC-CEDICT 行（<c>繁 简 [拼音] /释义/…/</c>）与通用行（<c>词头&lt;TAB&gt;释义</c>）。
/// 空行与 <c>#</c> 注释跳过；两种写法都不匹配的行忽略。
/// </summary>
public static class DictionaryLineParser
{
    /// <summary>通用行按词头合并义项时的上限，避免无界增长。</summary>
    public const int MaxSensesPerEntry = 64;

    public static IReadOnlyList<DictionaryRecord> Parse(
        TextReader reader,
        string cedictSourceName,
        string genericSourceName)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var records = new List<DictionaryRecord>();
        // 通用行按首次出现顺序合并到同一词条；CC-CEDICT 行按行成条，不参与合并。
        var genericIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        while (reader.ReadLine() is { } line)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] == '#')
            {
                continue;
            }

            if (CedictParser.TryParse(trimmed) is { } entry)
            {
                records.Add(new DictionaryRecord(
                    entry.Simplified,
                    entry.Traditional,
                    entry.Pinyin,
                    Phonetic: null,
                    entry.Definitions,
                    Exchange: null,
                    cedictSourceName));
                continue;
            }

            var separator = trimmed.IndexOf('\t');
            if (separator <= 0 || separator == trimmed.Length - 1)
            {
                continue;
            }

            var headword = trimmed[..separator].Trim();
            var definition = trimmed[(separator + 1)..].Trim();
            if (headword.Length == 0 || definition.Length == 0)
            {
                continue;
            }

            if (genericIndex.TryGetValue(headword, out var existingIndex))
            {
                var existing = records[existingIndex];
                if (existing.Definitions.Count >= MaxSensesPerEntry)
                {
                    continue;
                }

                var definitions = new List<string>(existing.Definitions) { definition };
                records[existingIndex] = existing with { Definitions = definitions };
                continue;
            }

            genericIndex[headword] = records.Count;
            records.Add(new DictionaryRecord(
                headword,
                Traditional: null,
                Pinyin: null,
                Phonetic: null,
                [definition],
                Exchange: null,
                genericSourceName));
        }

        return records;
    }
}
