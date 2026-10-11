using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia.Media.Imaging;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Emoji;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Emoji;

namespace CrispySearchbar.ViewModels;

public sealed record EmojiCandidateViewModel(EmojiEntry Entry, string Title, string Subtitle,
    Bitmap Image, bool HasVariants);

public sealed class EmojiModeViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly Task<EmojiIndex> _indexTask;
    private readonly EmojiHistoryStore _history;
    private readonly EmojiImageCache _images;
    private AppStrings _strings;
    private AppSettings _settings;
    private CancellationTokenSource? _search;
    private int _version;
    private bool _active;
    private bool _variants;
    private string _query = "";
    private string _status = "";
    private int _selectedIndex;
    private EmojiCandidateViewModel? _variantParent;
    private EmojiCandidateViewModel[] _normalCandidates = [];

    public EmojiModeViewModel(AppStrings strings, AppSettings settings, Task<EmojiIndex> indexTask,
        EmojiHistoryStore history, EmojiImageCache images)
    {
        _strings = strings;
        _settings = settings;
        _indexTask = indexTask;
        _history = history;
        _images = images;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<EmojiCandidateViewModel> Candidates { get; } = [];
    public int SelectedIndex
    {
        get => _selectedIndex; set
        {
            _selectedIndex = value;
            Changed();
            Changed(nameof(CanShowVariants));
        }
    }
    public EmojiCandidateViewModel? Selected => Candidates.ElementAtOrDefault(SelectedIndex);
    public bool IsChoosingVariant => _variants;
    public bool CanShowVariants => !_variants && Selected?.HasVariants == true;
    public bool KeepOpenAfterInsert => _settings.EmojiKeepOpenAfterInsert;
    public string Status
    {
        get => _status; private set
        {
            _status = value;
            Changed();
            Changed(nameof(HasStatus));
        }
    }
    public bool HasStatus => !string.IsNullOrWhiteSpace(Status);
    public string Hint => ShortcutTextFormatter.Format(_strings.Emoji.Hint, ShortcutCatalog.Create(_settings), _strings.SettingsTexts.ShortcutUnsetText);
    public string VariantsText => _strings.Emoji.Variants;
    public string CopyText => _strings.Emoji.Copy;
    public bool HasCandidates => Candidates.Count > 0;
    public void ApplyConfiguration(AppStrings strings, AppSettings settings)
    {
        _strings = strings;
        _settings = settings;
        Changed(nameof(Hint));
        Changed(nameof(VariantsText));
        Changed(nameof(CopyText));
        if (_active)
            _ = SearchAsync();
    }

    public void Update(string query, bool active)
    {
        if (_query == query && _active == active)
            return;
        _query = query;
        _active = active;
        _search?.Cancel();
        _version++;
        _variants = false;
        Changed(nameof(IsChoosingVariant));
        Changed(nameof(CanShowVariants));
        if (active)
            _ = SearchAsync();
        else
            Replace([]);
    }

    private async Task SearchAsync()
    {
        _search?.Cancel();
        var cts = _search = new CancellationTokenSource();
        var version = ++_version;
        Replace([]);
        Status = _strings.Emoji.Loading;
        try
        {
            var index = await _indexTask;
            cts.Token.ThrowIfCancellationRequested();
            var query = _query;
            var usage = _history.Snapshot();
            var tone = _settings.EmojiDefaultSkinTone;
            var matches = await Task.Run(() => index.Search(query, usage, tone, cancellationToken: cts.Token), cts.Token);
            if (version != _version || !_active)
                return;
            Replace(matches.Select(m => CreateCandidate(index, m.Entry, m.MatchedTerm)));
            _variants = false;
            Changed(nameof(IsChoosingVariant));
            Status = Candidates.Count == 0 ? _strings.Emoji.NoResults : string.IsNullOrWhiteSpace(query) ? _strings.Emoji.Recent : "";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (version == _version) Status = string.Format(_strings.Emoji.LoadFailed, ex.Message); }
        finally { if (ReferenceEquals(_search, cts)) _search = null; cts.Dispose(); }
    }

    private EmojiCandidateViewModel CreateCandidate(EmojiIndex index, EmojiEntry entry, string matchedTerm = "")
    {
        var chinese = _strings.Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        var title = chinese ? entry.NameZh : entry.NameEn;
        var subtitle = chinese ? entry.NameEn : entry.NameZh;
        if (!string.IsNullOrWhiteSpace(matchedTerm) && !title.Contains(matchedTerm, StringComparison.OrdinalIgnoreCase)
            && !subtitle.Contains(matchedTerm, StringComparison.OrdinalIgnoreCase))
            subtitle += " · " + matchedTerm;
        return new(entry, title, subtitle, _images.Get(entry.Image), index.GetVariants(entry).Count > 1);
    }

    private void Replace(IEnumerable<EmojiCandidateViewModel> candidates)
    {
        Candidates.Clear();
        foreach (var candidate in candidates)
            Candidates.Add(candidate);
        SelectedIndex = Candidates.Count > 0 ? 0 : -1;
        Changed(nameof(HasCandidates));
    }

    public void MoveSelection(int delta) => SelectedIndex = Math.Clamp(SelectedIndex + delta, 0, Math.Max(0, Candidates.Count - 1));
    public void OpenVariants()
    {
        if (!CanShowVariants || !_indexTask.IsCompletedSuccessfully || Selected is not { } selected)
            return;
        _normalCandidates = Candidates.ToArray();
        _variantParent = selected;
        _variants = true;
        Replace(_indexTask.Result.GetVariants(selected.Entry).Select(e => CreateCandidate(_indexTask.Result, e)));
        SelectedIndex = Math.Max(0, Candidates.ToList().FindIndex(c => c.Entry.Sequence == selected.Entry.Sequence));
        Status = _strings.Emoji.Variants;
        Changed(nameof(IsChoosingVariant));
        Changed(nameof(CanShowVariants));
    }
    public bool CommitVariant()
    {
        if (!_variants || Selected is not { } chosen)
            return false;
        var parent = _variantParent;
        var restored = _normalCandidates.Select(c => c == parent ? chosen : c).ToArray();
        _variants = false;
        Replace(restored);
        SelectedIndex = Array.IndexOf(restored, chosen);
        Status = "";
        Changed(nameof(IsChoosingVariant));
        Changed(nameof(CanShowVariants));
        return true;
    }
    public bool CancelVariants()
    {
        if (!_variants)
            return false;
        var parent = _variantParent;
        _variants = false;
        Replace(_normalCandidates);
        SelectedIndex = Math.Max(0, Array.IndexOf(_normalCandidates, parent));
        Status = "";
        Changed(nameof(IsChoosingVariant));
        Changed(nameof(CanShowVariants));
        return true;
    }
    public Task RecordAsync(string sequence) => _history.RecordAsync(sequence);
    public void ReportInputFailed() => Status = _strings.Emoji.InputFailed;
    public void ReportCopied() => Status = _strings.Emoji.Copied;
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
    public void Dispose()
    {
        _active = false;
        _version++;
        _search?.Cancel();
        _images.Dispose();
    }
}
