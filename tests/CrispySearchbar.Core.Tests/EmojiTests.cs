using System.Diagnostics;
using System.IO.Compression;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Emoji;
using Xunit;
using Xunit.Abstractions;

namespace CrispySearchbar.Core.Tests;

public class EmojiTests(ITestOutputHelper output)
{
    private static readonly EmojiIndex Index = EmojiResources.LoadIndex();
    [Theory]
    [InlineData("smile", "🙂")]
    [InlineData("weixiao", "🙂")]
    [InlineData("wei xiao", "🙂")]
    [InlineData("wēixiào", "🙂")]
    [InlineData("wx", "🙂")]
    [InlineData("点赞", "👍")]
    [InlineData("dianzan", "👍")]
    [InlineData("dz", "👍")]
    [InlineData("thumbs up", "👍")]
    [InlineData("smilex", "🙂")]
    [InlineData("火", "🔥")]
    [InlineData("huo", "🔥")]
    [InlineData("fire", "🔥")]
    [InlineData("heart", "❤️")]
    [InlineData("爱心", "❤️")]
    [InlineData("aixin", "❤️")]
    [InlineData("笑哭", "😂")]
    [InlineData("xiaoku", "😂")]
    [InlineData("lol", "😂")]
    [InlineData("狗头", "🐶")]
    [InlineData("goutou", "🐶")]
    [InlineData("dog", "🐶")]
    [InlineData("cat", "🐱")]
    [InlineData("猫", "🐱")]
    [InlineData("panda", "🐼")]
    [InlineData("咖啡", "☕")]
    [InlineData("coffee", "☕")]
    [InlineData("庆祝", "🎉")]
    [InlineData("party", "🎉")]
    [InlineData("礼物", "🎁")]
    [InlineData("gift", "🎁")]
    [InlineData("rocket", "🚀")]
    [InlineData("huojian", "🚀")]
    [InlineData("谢谢", "🙏")]
    [InlineData("thanks", "🙏")]
    [InlineData("red 爱心", "❤️")]
    [InlineData("red爱心", "❤️")]
    [InlineData("rocket火箭", "🚀")]
    public void CommonQueriesPutExpectedEmojiInTopThree(string query, string expected)
        => Assert.Contains(Index.Search(query).Take(3), m => m.Entry.Sequence == expected);

    [Fact]
    public void CatalogAndGraphicsCoverEveryFullyQualifiedSequence()
    {
        Assert.Equal(3944, Index.Entries.Count);
        Assert.Equal(Index.Entries.Count, Index.Entries.Select(e => e.Sequence).Distinct().Count());
        using var stream = EmojiResources.OpenImages();
        using var archive = new ZipArchive(stream);
        foreach (var entry in Index.Entries)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.NameEn));
            Assert.False(string.IsNullOrWhiteSpace(entry.NameZh));
            Assert.NotNull(archive.GetEntry(entry.Image + ".png"));
        }
        Assert.Equal("👩🏽‍💻", Assert.Single(Index.Search("👩🏽‍💻")).Entry.Sequence);
    }
    [Fact]
    public void ToneSelectionUsesRealVariantsIncludingMixedHandshake()
    {
        var thumbs = Index.Search("dianzan", tone: EmojiSkinTone.Dark)[0].Entry;
        Assert.Equal("👍🏿", thumbs.Sequence);
        Assert.Contains(Index.GetVariants(Index.Search("🤝")[0].Entry), e => e.Sequence == "🫱🏻‍🫲🏿");
        Assert.Contains(Index.GetVariants(Index.Search("👫")[0].Entry), e => e.Sequence == "👩🏻‍🤝‍👨🏿");
        Assert.Contains(Index.GetVariants(Index.Search("👬")[0].Entry), e => e.Sequence == "👨🏻‍🤝‍👨🏿");
        Assert.Contains(Index.GetVariants(Index.Search("👭")[0].Entry), e => e.Sequence == "👩🏻‍🤝‍👩🏿");
    }
    [Fact]
    public void PersonalUsageDoesNotDisplaceExactMatches()
    {
        var usage = new[] { new EmojiUsage("😸", 1000000, DateTimeOffset.UtcNow) };
        Assert.Equal("🙂", Index.Search("smile", usage)[0].Entry.Sequence);
        Assert.Equal("😸", Index.Search("", usage)[0].Entry.Sequence);
        Assert.Empty(Index.Search("zzzzzzzzzzzzzz"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Assert.Throws<OperationCanceledException>(() => Index.Search("smile", cancellationToken: cancellation.Token));
    }
    [Fact]
    public void NewDefaultsKeepTextCopyReservedAndDoNotCrashOnLegacyShortcutConflicts()
    {
        Assert.Null(ShortcutValidation.ValidateCandidate(ShortcutAction.CopyEmoji, "Ctrl+Shift+C"));
        Assert.NotNull(ShortcutValidation.ValidateCandidate(ShortcutAction.CopyEmoji, "Ctrl+C"));
        var settings = new AppSettings { ExecuteShortcut = "Alt+Down" };
        Assert.True(ShortcutParser.TryParse("Alt+Down", out var binding));
        Assert.Equal(ShortcutAction.Execute, ShortcutCatalog.Create(settings).FindAction(binding));
        Assert.Contains(ShortcutValidation.Validate(settings), e => e.PropertyName == nameof(AppSettings.EmojiVariantsShortcut));
    }

    [Fact]
    public void SignedNumericAliasesRemainDistinct()
    {
        Assert.Equal("👍", Index.Search("+1")[0].Entry.Sequence);
        Assert.Equal("👎", Index.Search("-1")[0].Entry.Sequence);
        Assert.Equal("👎", Index.Search(":-1:")[0].Entry.Sequence);
    }
    [Fact]
    public void ReportsWarmSearchLatency()
    {
        string[] queries = ["smile", "weixiao", "wx", "点赞", "thumbs up", "smilex", "red 爱心"];
        foreach (var query in queries)
            Index.Search(query);
        var times = new List<double>();
        for (var i = 0; i < 70; i++)
        {
            var watch = Stopwatch.StartNew();
            Index.Search(queries[i % queries.Length]);
            times.Add(watch.Elapsed.TotalMilliseconds);
        }
        times.Sort();
        output.WriteLine($"Warm search P95: {times[(int)(times.Count * .95)]:F2} ms; max: {times[^1]:F2} ms");
    }
    [Fact]
    public async Task HistoryIsBoundedAndSurvivesCorruptionAndReload()
    {
        var directory = Path.Combine(Path.GetTempPath(), "crispy-history-" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, "emoji-history.json"), "broken");
            var history = new EmojiHistoryStore(directory);
            Assert.Empty(history.Snapshot());
            File.WriteAllText(Path.Combine(directory, "emoji-history.json"), "[null]");
            Assert.Empty(new EmojiHistoryStore(directory).Snapshot());
            await history.RecordAsync("❤️");
            await history.RecordAsync("❤️");
            Assert.Equal(2, new EmojiHistoryStore(directory).Snapshot()[0].Count);
            for (var i = 0; i < 130; i++)
                await history.RecordAsync(i.ToString());
            Assert.Equal(128, new EmojiHistoryStore(directory).Snapshot().Count);
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
