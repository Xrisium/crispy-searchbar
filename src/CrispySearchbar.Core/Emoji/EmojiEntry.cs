namespace CrispySearchbar.Core.Emoji;

public sealed record EmojiEntry
{
    public string Group { get; init; } = "";
    public required string Sequence
    {
        get; init;
    }
    public required string NameEn
    {
        get; init;
    }
    public required string NameZh
    {
        get; init;
    }
    public string[] PreferredTerms { get; init; } = [];
    public required string[] Terms
    {
        get; init;
    }
    public required string[] Pinyin
    {
        get; init;
    }
    public required string[] Initials
    {
        get; init;
    }
    public required string VariantGroup
    {
        get; init;
    }
    public required string Image
    {
        get; init;
    }
    public required int Order
    {
        get; init;
    }
}

public sealed record EmojiMatch(EmojiEntry Entry, string MatchedTerm, int Tier, int Quality);
public sealed record EmojiUsage(string Sequence, long Count, DateTimeOffset LastUsed);
