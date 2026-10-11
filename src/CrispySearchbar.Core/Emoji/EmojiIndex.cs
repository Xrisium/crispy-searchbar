using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using CrispySearchbar.Core.Configuration;

namespace CrispySearchbar.Core.Emoji;

/// <summary>Immutable offline index. Search relevance always precedes personal usage.</summary>
public sealed class EmojiIndex
{
    private sealed record Term(string Key, string Label, bool Preferred, EmojiEntry Entry, string[] Words);
    private sealed record Indexed(Term[] Terms, string[] Initials, EmojiEntry Entry);
    private readonly Indexed[] _entries;
    private readonly Dictionary<string, EmojiEntry[]> _variants;
    private readonly Dictionary<string, EmojiEntry> _bySequence;
    private static readonly string[] Featured =
        ["🙂", "😂", "❤️", "👍", "🙏", "🎉", "🔥", "✨", "🥰", "😭", "👋", "✅"];
    private static readonly Regex TokenSeparator = new(
        @"[\s\-_:]+|(?<=[A-Za-z0-9])(?=[\u3400-\u9fff])|(?<=[\u3400-\u9fff])(?=[A-Za-z0-9])",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    public EmojiIndex(IReadOnlyList<EmojiEntry> entries)
    {
        Entries = entries;
        _bySequence = entries.ToDictionary(e => e.Sequence, StringComparer.Ordinal);
        _variants = entries.GroupBy(e => e.VariantGroup).ToDictionary(g => g.Key, g => g.ToArray());
        // Skin variants share most keywords. Score each normalized term once per group rather
        // than repeating the same fuzzy comparison for every skin combination.
        _entries = _variants.Values.Select(group => new Indexed(
            group.SelectMany(e => e.Terms.Concat(e.Pinyin).Select(t => new Term(Normalize(t), t,
                e.PreferredTerms.Contains(t), e, t.Split([' ', '-', ':', '/'], StringSplitOptions.RemoveEmptyEntries)
                    .Select(Normalize).Where(w => w.Length > 0).ToArray())))
                .Where(t => t.Key.Length > 0).OrderByDescending(t => t.Preferred).ThenBy(t => t.Entry.Order)
                .DistinctBy(t => t.Key).ToArray(),
            group.SelectMany(e => e.Initials).Select(Normalize).Distinct().ToArray(), group[0])).ToArray();
    }

    public IReadOnlyList<EmojiEntry> Entries
    {
        get;
    }
    public IReadOnlyList<EmojiEntry> GetVariants(EmojiEntry entry) => _variants[entry.VariantGroup];

    public EmojiEntry ApplySkinTone(EmojiEntry entry, EmojiSkinTone tone)
    {
        var variants = _variants[entry.VariantGroup];
        if (tone == EmojiSkinTone.Neutral)
            return variants.FirstOrDefault(e => !HasSkinTone(e.Sequence)) ?? entry;
        var desired = 0x1f3fa + (int)tone;
        return variants.FirstOrDefault(e =>
        {
            var modifiers = e.Sequence.EnumerateRunes().Where(r => r.Value is >= 0x1f3fb and <= 0x1f3ff).ToArray();
            return modifiers.Length > 0 && modifiers.All(r => r.Value == desired);
        }) ?? variants.FirstOrDefault(e => !HasSkinTone(e.Sequence)) ?? entry;
    }

    public IReadOnlyList<EmojiMatch> Search(string query, IReadOnlyList<EmojiUsage>? usage = null,
        EmojiSkinTone tone = EmojiSkinTone.Neutral, int maxResults = 12, CancellationToken cancellationToken = default)
    {
        if (maxResults <= 0)
            return [];
        var normalized = Normalize(query);
        var history = (usage ?? []).ToDictionary(u => u.Sequence, StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(query))
        {
            var recent = (usage ?? []).OrderByDescending(u => u.LastUsed)
                .Where(u => _bySequence.ContainsKey(u.Sequence)).Select(u => _bySequence[u.Sequence]);
            return recent.Concat(Featured.Where(_bySequence.ContainsKey).Select(s => ApplySkinTone(_bySequence[s], tone)))
                .DistinctBy(e => e.Sequence).Take(maxResults).Select(e => new EmojiMatch(e, "", 0, 0)).ToArray();
        }
        if (_bySequence.TryGetValue(query.Trim(), out var literal))
            return [new(literal, query, 0, 0)];
        if (normalized.Length == 0)
            return [];
        var tokens = TokenSeparator.Split(query)
            .Select(Normalize).Where(t => t.Length > 0).ToArray();
        var matches = new List<EmojiMatch>();
        foreach (var indexed in _entries)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bestTier = int.MaxValue;
            var bestQuality = int.MaxValue;
            var label = "";
            var matchedEntry = indexed.Entry;
            foreach (var term in indexed.Terms)
            {
                var key = term.Key;
                var tier = key == normalized ? 0 : IsPrefix(term, normalized) ? 1
                    : key.Contains(normalized, StringComparison.Ordinal) ? 2 : int.MaxValue;
                var quality = Math.Abs(key.Length - normalized.Length) + (term.Preferred ? 0 : 1000);
                if (tier != int.MaxValue && (tier < bestTier || tier == bestTier && quality < bestQuality))
                {
                    bestTier = tier;
                    bestQuality = quality;
                    label = term.Label;
                    matchedEntry = term.Entry;
                }
            }
            if (bestTier > 2 && tokens.Length > 1 && tokens.All(t => indexed.Terms.Any(k => k.Key.Contains(t, StringComparison.Ordinal))))
            {
                bestTier = 2;
                bestQuality = tokens.Sum(t => indexed.Terms.Where(k => k.Key.Contains(t, StringComparison.Ordinal))
                    .Min(k => k.Key.Length - t.Length + (k.Preferred ? 0 : 1000)));
                label = query;
            }
            if (bestTier > 3 && normalized.Length >= 2 && indexed.Initials.Any(i => i == normalized || i.StartsWith(normalized, StringComparison.Ordinal)))
            {
                bestTier = 3;
                bestQuality = indexed.Initials.Where(i => i.StartsWith(normalized, StringComparison.Ordinal)).Min(i => i.Length - normalized.Length);
                label = query;
            }
            if (bestTier == int.MaxValue && normalized.Length >= 4)
            {
                var limit = normalized.Length >= 8 ? 2 : 1;
                foreach (var term in indexed.Terms)
                {
                    var key = term.Key;
                    if (Math.Abs(key.Length - normalized.Length) > limit)
                        continue;
                    var distance = EditDistance(normalized, key, limit);
                    if (distance <= limit && distance + (term.Preferred ? 0 : 1000) < bestQuality)
                    {
                        bestTier = 4;
                        bestQuality = distance + (term.Preferred ? 0 : 1000);
                        label = term.Label;
                        matchedEntry = term.Entry;
                    }
                }
            }
            if (bestTier != int.MaxValue)
                matches.Add(new(matchedEntry, label, bestTier, bestQuality));
        }
        return matches.GroupBy(m => m.Entry.VariantGroup)
            .Select(g =>
            {
                var best = g.OrderBy(m => m.Tier).ThenBy(m => m.Quality).ThenBy(m => m.Entry.Order).First();
                // Explicit skin-tone searches retain their exact matching variant.
                return bestTierIsSkinSpecific(best) ? best : best with
                {
                    Entry = ApplySkinTone(best.Entry, tone)
                };
            })
            .OrderBy(m => m.Tier).ThenBy(m => m.Quality)
            .ThenByDescending(m => history.GetValueOrDefault(m.Entry.Sequence)?.Count ?? 0)
            .ThenByDescending(m => history.GetValueOrDefault(m.Entry.Sequence)?.LastUsed ?? DateTimeOffset.MinValue)
            .ThenBy(m => m.Entry.Order).Take(maxResults).ToArray();

        bool bestTierIsSkinSpecific(EmojiMatch match) => HasSkinTone(match.Entry.Sequence)
            && (normalized.Contains("肤色", StringComparison.Ordinal) || normalized.Contains("skin", StringComparison.Ordinal));
    }

    public static string Normalize(string text)
    {
        var numericAlias = text.Trim().Trim(':');
        if (numericAlias is "+1" or "-1")
            return numericAlias;
        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Replace('ü', 'v').Replace('Ü', 'v').Normalize(NormalizationForm.FormD))
            if (char.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark && char.IsLetterOrDigit(c))
                builder.Append(char.ToLowerInvariant(c));
        return builder.ToString();
    }

    private static bool HasSkinTone(string text) => text.EnumerateRunes().Any(r => r.Value is >= 0x1f3fb and <= 0x1f3ff);

    private static bool IsPrefix(Term term, string query)
    {
        if (term.Key.StartsWith(query, StringComparison.Ordinal))
            return true;
        foreach (var word in term.Words)
            if (word.StartsWith(query, StringComparison.Ordinal))
                return true;
        return false;
    }

    private static int EditDistance(string left, string right, int limit)
    {
        Span<int> previous = stackalloc int[right.Length + 1];
        Span<int> current = stackalloc int[right.Length + 1];
        for (var j = 0; j <= right.Length; j++)
            previous[j] = j;
        for (var i = 1; i <= left.Length; i++)
        {
            current[0] = i;
            var minimum = i;
            for (var j = 1; j <= right.Length; j++)
            {
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (left[i - 1] == right[j - 1] ? 0 : 1));
                minimum = Math.Min(minimum, current[j]);
            }
            if (minimum > limit)
                return limit + 1;
            current.CopyTo(previous);
        }
        return previous[right.Length];
    }
}
