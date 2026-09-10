namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// .csv 解析器：两个方向共用，按表头分派方言。
/// 含 <c>word</c> 列 → ECDICT 方言；含 <c>traditional</c> 与 <c>simplified</c> 列 → CC-CEDICT 方言。
/// </summary>
public static class DictionaryCsvParser
{
    public const string WordColumn = "word";

    public const string TraditionalColumn = "traditional";

    public const string SimplifiedColumn = "simplified";

    private const int MaxSensesPerEntry = 64;

    public static IReadOnlyList<DictionaryRecord> Parse(
        TextReader reader,
        string cedictSourceName,
        string ecdictSourceName)
    {
        ArgumentNullException.ThrowIfNull(reader);

        var records = new List<DictionaryRecord>();
        var columns = new Dictionary<string, int>(StringComparer.Ordinal);
        CsvDialect? dialect = null;
        var isHeader = true;
        foreach (var fields in CsvRecordReader.Read(reader))
        {
            if (isHeader)
            {
                isHeader = false;
                for (var i = 0; i < fields.Length; i++)
                {
                    var name = fields[i].Trim().ToLowerInvariant();
                    if (name.Length > 0)
                    {
                        columns[name] = i;
                    }
                }

                dialect = ResolveDialect(columns);
                continue;
            }

            var record = dialect switch
            {
                CsvDialect.Ecdict => ParseEcdictRow(fields, columns, ecdictSourceName),
                _ => ParseCedictRow(fields, columns, cedictSourceName),
            };
            if (record is not null)
            {
                records.Add(record);
            }
        }

        return records;
    }

    private static CsvDialect ResolveDialect(IReadOnlyDictionary<string, int> columns)
    {
        if (columns.ContainsKey(WordColumn))
        {
            return CsvDialect.Ecdict;
        }

        if (columns.ContainsKey(TraditionalColumn) && columns.ContainsKey(SimplifiedColumn))
        {
            return CsvDialect.CcCedict;
        }

        var header = string.Join(", ", columns.Keys);
        throw new InvalidDataException(
            header.Length == 0
                ? "无法识别的 CSV 表头：文件首行没有列名。"
                : $"无法识别的 CSV 表头：{header}。需要包含 word 列，或同时包含 traditional 与 simplified 列。");
    }

    private static DictionaryRecord? ParseEcdictRow(
        string[] fields,
        IReadOnlyDictionary<string, int> columns,
        string sourceName)
    {
        var word = Clean(Field(fields, columns, WordColumn));
        if (word is null)
        {
            return null;
        }

        var phonetic = Clean(Field(fields, columns, "phonetic"));
        var translation = Field(fields, columns, "translation");
        var definition = Field(fields, columns, "definition");
        var exchange = Clean(Field(fields, columns, "exchange"));

        var senses = BuildSenses(
            !string.IsNullOrWhiteSpace(translation) ? translation : definition);
        return senses.Count == 0
            ? null
            : new DictionaryRecord(
                word,
                Traditional: null,
                Pinyin: null,
                phonetic,
                senses,
                exchange,
                sourceName);
    }

    private static DictionaryRecord? ParseCedictRow(
        string[] fields,
        IReadOnlyDictionary<string, int> columns,
        string sourceName)
    {
        var traditional = Clean(Field(fields, columns, TraditionalColumn));
        var simplified = Clean(Field(fields, columns, SimplifiedColumn));
        if (simplified is null || traditional is null)
        {
            return null;
        }

        var pinyin = Clean(Field(fields, columns, "pinyin"));
        var definitions = (Field(fields, columns, "definitions") ?? string.Empty)
            .Trim()
            .Trim('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (definitions.Length == 0)
        {
            return null;
        }

        if (definitions.Length > MaxSensesPerEntry)
        {
            definitions = definitions[..MaxSensesPerEntry];
        }

        return new DictionaryRecord(
            simplified,
            traditional,
            pinyin,
            Phonetic: null,
            definitions,
            Exchange: null,
            sourceName);
    }

    private static string? Field(
        string[] fields,
        IReadOnlyDictionary<string, int> columns,
        string column)
        => columns.TryGetValue(column, out var index) && index < fields.Length
            ? fields[index]
            : null;

    private static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();
        return trimmed.Length == 0 ? null : trimmed;
    }

    private static IReadOnlyList<string> BuildSenses(string? text)
    {
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

    private enum CsvDialect
    {
        Ecdict,
        CcCedict,
    }
}
