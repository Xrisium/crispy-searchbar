using System.Text.Json;
using CrispySearchbar.Core.Configuration;

namespace CrispySearchbar.Core.Emoji;

public sealed class EmojiHistoryStore
{
    private readonly string _path;
    private readonly object _gate = new();
    private readonly SemaphoreSlim _writer = new(1, 1);
    private List<EmojiUsage> _items = [];
    public EmojiHistoryStore(string? directory = null)
    {
        _path = Path.Combine(directory ?? Path.Combine(Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData), "CrispySearchbar"), "emoji-history.json");
        try
        {
            if (File.Exists(_path))
                _items = (JsonSerializer.Deserialize<List<EmojiUsage?>>(File.ReadAllBytes(_path)) ?? [])
                .Where(u => u is not null && !string.IsNullOrEmpty(u.Sequence) && u.Count > 0).Cast<EmojiUsage>().DistinctBy(u => u.Sequence)
                .OrderByDescending(u => u.LastUsed).Take(128).ToList();
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
    }
    public IReadOnlyList<EmojiUsage> Snapshot()
    {
        lock (_gate)
            return _items.ToArray();
    }
    public async Task RecordAsync(string sequence)
    {
        lock (_gate)
        {
            var old = _items.FirstOrDefault(u => u.Sequence == sequence);
            _items.RemoveAll(u => u.Sequence == sequence);
            _items.Insert(0, new(sequence, Math.Min(old?.Count ?? 0, long.MaxValue - 1) + 1, DateTimeOffset.UtcNow));
            if (_items.Count > 128)
                _items.RemoveRange(128, _items.Count - 128);
        }
        await _writer.WaitAsync().ConfigureAwait(false);
        try
        {
            var bytes = JsonSerializer.SerializeToUtf8Bytes(Snapshot());
            await Task.Run(() => AppSettingsStore.WriteAtomic(_path, bytes)).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
        finally { _writer.Release(); }
    }
}
