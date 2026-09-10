namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// ECDICT CSV（UTF-8）解析器。
/// 首行为列头，字段允许被双引号包裹并在其中出现逗号与换行。
/// </summary>
public static class EcdictParser
{
    public const string SourceName = "ECDICT";

    private const int MaxSensesPerEntry = 64;

    public static IReadOnlyList<EcdictEntry> Parse(TextReader reader)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var entries = new List<EcdictEntry>();
        var columnIndex = new Dictionary<string, int>(StringComparer.Ordinal);
        var isHeader = true;
        foreach (var fields in CsvRecordReader.Read(reader))
        {
            if (isHeader)
            {
                isHeader = false;
                for (var i = 0; i < fields.Length; i++)
                {
                    columnIndex[fields[i].Trim()] = i;
                }

                continue;
            }

            if (TryParse(fields, columnIndex) is { } entry)
            {
                entries.Add(entry);
            }
        }

        return entries;
    }

    /// <summary>供测试使用：直接解析字符串形式的 CSV。</summary>
    public static IReadOnlyList<EcdictEntry> ParseText(string csv)
    {
        ArgumentNullException.ThrowIfNull(csv);
        using var reader = new StringReader(csv);
        return Parse(reader);
    }

    private static EcdictEntry? TryParse(
        string[] fields,
        IReadOnlyDictionary<string, int> columnIndex)
    {
        var word = Clean(Field(fields, columnIndex, "word"));
        if (word is null)
        {
            return null;
        }

        var phonetic = Clean(Field(fields, columnIndex, "phonetic"));
        var translation = Field(fields, columnIndex, "translation");
        var definition = Field(fields, columnIndex, "definition");
        var exchange = Clean(Field(fields, columnIndex, "exchange"));

        var senses = BuildSenses(translation, definition);
        if (senses.Count == 0)
        {
            return null;
        }

        return new EcdictEntry(word, phonetic, senses, exchange);
    }

    private static string? Field(
        string[] fields,
        IReadOnlyDictionary<string, int> columnIndex,
        string column)
    {
        return columnIndex.TryGetValue(column, out var index) && index < fields.Length
            ? fields[index]
            : null;
    }

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static IReadOnlyList<string> BuildSenses(string? translation, string? definition)
    {
        var text = !string.IsNullOrWhiteSpace(translation) ? translation : definition;
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var senses = text.Split(
            ['\r', '\n'],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return senses.Length <= MaxSensesPerEntry
            ? senses
            : senses.Take(MaxSensesPerEntry).ToArray();
    }

}
