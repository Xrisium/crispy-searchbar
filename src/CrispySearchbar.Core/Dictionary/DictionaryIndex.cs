using System.Text;

namespace CrispySearchbar.Core.Dictionary;

/// <summary>
/// 不可变的词典内存索引：简体/繁体头部前缀 + 英文释义分词索引。
/// 构建完成后可安全地被后台任务读取。
/// </summary>
public sealed class DictionaryIndex
{
    private const int MaxRawCandidatesPerQuery = 600;

    private readonly DictionaryEntry[] _entries;
    private readonly HeadwordRef[] _headwords;
    private readonly string[] _tokens;
    private readonly Dictionary<string, int[]> _tokenEntries;
    private readonly string[] _definitionTexts;

    private DictionaryIndex(
        DictionaryEntry[] entries,
        HeadwordRef[] headwords,
        string[] tokens,
        Dictionary<string, int[]> tokenEntries,
        string[] definitionTexts)
    {
        _entries = entries;
        _headwords = headwords;
        _tokens = tokens;
        _tokenEntries = tokenEntries;
        _definitionTexts = definitionTexts;
    }

    public int Count => _entries.Length;

    public IReadOnlyList<DictionaryEntry> Entries => _entries;

    public static DictionaryIndex Empty { get; } = new(
        [],
        [],
        [],
        new Dictionary<string, int[]>(),
        []);

    /// <summary>从词条构建索引。空词条、空简体/繁体会被忽略。</summary>
    public static DictionaryIndex Build(IEnumerable<DictionaryEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        var entryList = entries
            .Where(entry => entry is not null
                && !string.IsNullOrWhiteSpace(entry.Simplified)
                && !string.IsNullOrWhiteSpace(entry.Traditional))
            .ToArray();
        if (entryList.Length == 0)
        {
            return Empty;
        }

        var headwords = new List<HeadwordRef>(entryList.Length * 2);
        for (var entryIndex = 0; entryIndex < entryList.Length; entryIndex++)
        {
            var entry = entryList[entryIndex];
            headwords.Add(new HeadwordRef(entry.Simplified, entryIndex));
            if (!string.Equals(entry.Traditional, entry.Simplified, StringComparison.Ordinal))
            {
                headwords.Add(new HeadwordRef(entry.Traditional, entryIndex));
            }
        }

        headwords.Sort(static (left, right) =>
        {
            var byText = string.CompareOrdinal(left.Text, right.Text);
            return byText != 0 ? byText : left.Entry.CompareTo(right.Entry);
        });

        var tokenBuckets = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var definitionTexts = new string[entryList.Length];
        for (var entryIndex = 0; entryIndex < entryList.Length; entryIndex++)
        {
            var normalizedTokens = new List<string>();
            foreach (var definition in entryList[entryIndex].Definitions)
            {
                foreach (var token in ExtractLatinTokens(definition))
                {
                    normalizedTokens.Add(token);
                    if (!tokenBuckets.TryGetValue(token, out var bucket))
                    {
                        bucket = [];
                        tokenBuckets.Add(token, bucket);
                    }

                    if (bucket.Count == 0 || bucket[^1] != entryIndex)
                    {
                        bucket.Add(entryIndex);
                    }
                }
            }

            definitionTexts[entryIndex] = string.Join(' ', normalizedTokens);
        }

        var tokens = tokenBuckets.Keys.ToArray();
        Array.Sort(tokens, StringComparer.Ordinal);
        var tokenEntries = tokenBuckets.ToDictionary(
            pair => pair.Key,
            pair => pair.Value.ToArray(),
            StringComparer.Ordinal);

        return new DictionaryIndex(
            entryList,
            headwords.ToArray(),
            tokens,
            tokenEntries,
            definitionTexts);
    }

    /// <summary>
    /// 搜索候选。含 CJK 的查询按中文头部匹配；其余查询按释义英文词匹配，
    /// 排序为 释义开头完整词 &gt; 完整词 &gt; 前缀 &gt; 子串，同一档位保持原始词条顺序。
    /// </summary>
    public IReadOnlyList<DictionaryEntry> Search(string? rawQuery, int maxResults = 12)
    {
        if (string.IsNullOrWhiteSpace(rawQuery) || maxResults <= 0)
        {
            return [];
        }

        var query = rawQuery.Trim();
        if (query.Length == 0)
        {
            return [];
        }

        return ContainsCjk(query)
            ? SearchChinese(query, maxResults)
            : SearchLatin(query, maxResults);
    }

    private IReadOnlyList<DictionaryEntry> SearchChinese(string query, int maxResults)
    {
        var exact = new List<int>();
        var prefix = new List<int>();
        var seen = new HashSet<int>();

        var lower = LowerBound(_headwords, query);
        for (var i = lower; i < _headwords.Length; i++)
        {
            var headword = _headwords[i];
            if (!headword.Text.StartsWith(query, StringComparison.Ordinal))
            {
                break;
            }

            if (!seen.Add(headword.Entry))
            {
                continue;
            }

            if (string.Equals(headword.Text, query, StringComparison.Ordinal))
            {
                exact.Add(headword.Entry);
            }
            else
            {
                prefix.Add(headword.Entry);
            }

            if (exact.Count >= maxResults)
            {
                break;
            }
        }

        var results = new List<ScoredEntry>(exact.Count + prefix.Count);
        AddScored(results, exact, score: 0);
        if (results.Count < maxResults)
        {
            AddScored(results, prefix, score: 1);
        }

        if (results.Count < maxResults)
        {
            for (var entryIndex = 0; entryIndex < _entries.Length && results.Count < maxResults; entryIndex++)
            {
                if (seen.Contains(entryIndex))
                {
                    continue;
                }

                var entry = _entries[entryIndex];
                if (entry.Simplified.Contains(query, StringComparison.Ordinal)
                    || entry.Traditional.Contains(query, StringComparison.Ordinal))
                {
                    results.Add(new ScoredEntry(entryIndex, 2));
                    seen.Add(entryIndex);
                }
            }
        }

        return Resolve(results, maxResults);
    }

    private IReadOnlyList<DictionaryEntry> SearchLatin(string rawQuery, int maxResults)
    {
        var query = NormalizeLatinQuery(rawQuery);
        if (query.Length == 0)
        {
            return [];
        }

        var seen = new HashSet<int>();
        var results = new List<ScoredEntry>(MaxRawCandidatesPerQuery);

        if (_tokenEntries.TryGetValue(query, out var exactEntries))
        {
            foreach (var entryIndex in exactEntries)
            {
                if (results.Count >= MaxRawCandidatesPerQuery)
                {
                    break;
                }

                if (!seen.Add(entryIndex))
                {
                    continue;
                }

                // 释义以查询词开头（如 apple / apple (computer)）比“某处包含 apple”
                // 的习语条目更贴近用户意图，排在最前。
                var score = HasDefinitionStartingWithQuery(_entries[entryIndex], query) ? 0 : 1;
                results.Add(new ScoredEntry(entryIndex, score));
            }
        }

        if (results.Count < MaxRawCandidatesPerQuery)
        {
            var lower = LowerBound(_tokens, query);
            for (var i = lower;
                 i < _tokens.Length && results.Count < MaxRawCandidatesPerQuery;
                 i++)
            {
                var token = _tokens[i];
                if (!token.StartsWith(query, StringComparison.Ordinal))
                {
                    break;
                }

                foreach (var entryIndex in _tokenEntries[token])
                {
                    if (results.Count >= MaxRawCandidatesPerQuery)
                    {
                        break;
                    }

                    if (seen.Add(entryIndex))
                    {
                        results.Add(new ScoredEntry(entryIndex, 2));
                    }
                }
            }
        }

        if (results.Count < maxResults)
        {
            for (var entryIndex = 0; entryIndex < _entries.Length && results.Count < maxResults; entryIndex++)
            {
                if (seen.Contains(entryIndex))
                {
                    continue;
                }

                if (_definitionTexts[entryIndex].Contains(query, StringComparison.Ordinal))
                {
                    results.Add(new ScoredEntry(entryIndex, 3));
                    seen.Add(entryIndex);
                }
            }
        }

        return Resolve(results, maxResults);
    }

    private static bool HasDefinitionStartingWithQuery(DictionaryEntry entry, string query)
    {
        foreach (var definition in entry.Definitions)
        {
            var normalized = NormalizeLatinQuery(definition);
            if (normalized.Equals(query, StringComparison.Ordinal))
            {
                return true;
            }

            if (normalized.StartsWith(query, StringComparison.Ordinal)
                && normalized.Length > query.Length
                && char.IsWhiteSpace(normalized[query.Length]))
            {
                return true;
            }
        }

        return false;
    }
    private IReadOnlyList<DictionaryEntry> Resolve(List<ScoredEntry> scored, int maxResults)
    {
        scored.Sort(static (left, right) =>
        {
            var byScore = left.Score.CompareTo(right.Score);
            return byScore != 0 ? byScore : left.Entry.CompareTo(right.Entry);
        });

        var count = Math.Min(scored.Count, maxResults);
        var entries = new List<DictionaryEntry>(count);
        for (var i = 0; i < count; i++)
        {
            entries.Add(_entries[scored[i].Entry]);
        }

        return entries;
    }

    private static void AddScored(
        List<ScoredEntry> target,
        IReadOnlyList<int> entryIndexes,
        int score)
    {
        foreach (var entryIndex in entryIndexes)
        {
            target.Add(new ScoredEntry(entryIndex, score));
        }
    }

    private static bool ContainsCjk(string text)
    {
        foreach (var character in text)
        {
            if (character is >= '\u4E00' and <= '\u9FFF')
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeLatinQuery(string text)
    {
        var builder = new StringBuilder(text.Length);
        var pendingSpace = false;
        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
            }
            else if (char.IsLetterOrDigit(character) || character is '\'' or '-' or '\u2019')
            {
                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(char.ToLowerInvariant(character is '\u2019' ? '\'' : character));
            }
            else
            {
                pendingSpace = builder.Length > 0;
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static IEnumerable<string> ExtractLatinTokens(string text)
    {
        var token = new StringBuilder();
        foreach (var rawCharacter in text)
        {
            var character = rawCharacter is '\u2019' ? '\'' : rawCharacter;
            if (char.IsLetterOrDigit(character) || character is '\'' or '-')
            {
                token.Append(char.ToLowerInvariant(character));
            }
            else if (token.Length > 0)
            {
                yield return token.ToString();
                token.Clear();
            }
        }

        if (token.Length > 0)
        {
            yield return token.ToString();
        }
    }

    private static int LowerBound(string[] items, string value)
    {
        var low = 0;
        var high = items.Length;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (string.CompareOrdinal(items[middle], value) < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private static int LowerBound(HeadwordRef[] items, string value)
    {
        var low = 0;
        var high = items.Length;
        while (low < high)
        {
            var middle = low + ((high - low) / 2);
            if (string.CompareOrdinal(items[middle].Text, value) < 0)
            {
                low = middle + 1;
            }
            else
            {
                high = middle;
            }
        }

        return low;
    }

    private readonly record struct HeadwordRef(string Text, int Entry);

    private readonly record struct ScoredEntry(int Entry, int Score);
}
