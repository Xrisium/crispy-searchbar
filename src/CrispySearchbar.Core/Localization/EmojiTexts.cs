namespace CrispySearchbar.Core.Localization;

public sealed class EmojiTexts
{
    public required string RecentTab { get; init; }
    public required string SearchResultsTab { get; init; }
    public required string NoRecent { get; init; }
    public required string GridHint { get; init; }
    public required IReadOnlyDictionary<string, string> Categories { get; init; }
    public required string Loading
    {
        get; init;
    }
    public required string NoResults
    {
        get; init;
    }
    public required string Recent
    {
        get; init;
    }
    public required string Hint
    {
        get; init;
    }
    public required string Variants
    {
        get; init;
    }
    public required string Copy
    {
        get; init;
    }
    public required string Copied
    {
        get; init;
    }
    public required string InputFailed
    {
        get; init;
    }
    public required string LoadFailed
    {
        get; init;
    }
    public required string HotkeyFailed
    {
        get; init;
    }
}
