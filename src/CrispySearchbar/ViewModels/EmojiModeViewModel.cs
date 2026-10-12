using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Media.Imaging;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Emoji;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Emoji;

namespace CrispySearchbar.ViewModels;

public sealed record EmojiCandidateViewModel(EmojiEntry Entry, string Title, string Subtitle,
    Func<Bitmap> LoadImage, bool HasVariants)
{
    private Bitmap? _image;
    public Bitmap Image => _image ??= LoadImage();
    public string UnicodeCode { get; } = string.Join(" ", Entry.Sequence.EnumerateRunes().Select(r => $"U+{r.Value:X4}"));
    public string DisplaySubtitle => Subtitle.Length > 0 ? $"{Subtitle} · {UnicodeCode}" : UnicodeCode;
    public string Tooltip => $"{Title} · {UnicodeCode}";
}

public sealed class EmojiCategoryViewModel(string key, string title) : INotifyPropertyChanged
{
    private string _title = title;
    public string Key { get; } = key;
    public string Title
    {
        get => _title;
        set
        {
            if (_title == value)
                return;
            _title = value;
            PropertyChanged?.Invoke(this, new(nameof(Title)));
        }
    }
    public event PropertyChangedEventHandler? PropertyChanged;
}

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
    private EmojiIndex? _index;
    private IReadOnlyList<EmojiMatch> _matches = [];
    private int _selectedCategoryIndex;
    private bool _selectInitialCategory = true;
    private bool HasQuery => !string.IsNullOrWhiteSpace(_query);
    public const int GridColumns = 12;

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
    public IReadOnlyList<EmojiCategoryViewModel> Categories { get; private set; } = [];
    public bool IsGrid => _settings.EmojiCandidateStyle == EmojiCandidateStyle.Grid;
    public bool ShowCategories => IsGrid && !_variants;
    public EmojiCategoryViewModel? SelectedCategory
    {
        get => Categories.ElementAtOrDefault(_selectedCategoryIndex);
        set
        {
            if (value is not null)
                SelectedCategoryIndex = Categories.ToList().IndexOf(value);
        }
    }
    public int SelectedCategoryIndex
    {
        get => _selectedCategoryIndex;
        set
        {
            if (value < 0 || value >= Categories.Count || value == _selectedCategoryIndex)
                return;
            _selectedCategoryIndex = value;
            _selectInitialCategory = false;
            Changed();
            Changed(nameof(SelectedCategory));
            if (_search is null)
                FilterCandidates();
        }
    }
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
    public string Hint => ShortcutTextFormatter.Format(IsGrid ? _strings.Emoji.GridHint : _strings.Emoji.Hint,
        ShortcutCatalog.Create(_settings), _strings.SettingsTexts.ShortcutUnsetText);
    public string VariantsText => _strings.Emoji.Variants;
    public string CopyText => _strings.Emoji.Copy;
    public bool HasCandidates => Candidates.Count > 0;
    public void ApplyConfiguration(AppStrings strings, AppSettings settings)
    {
        if (IsGrid != (settings.EmojiCandidateStyle == EmojiCandidateStyle.Grid))
        {
            _selectedCategoryIndex = 0;
            _selectInitialCategory = true;
        }
        _strings = strings;
        _settings = settings;
        Changed(nameof(Hint));
        Changed(nameof(VariantsText));
        Changed(nameof(CopyText));
        Changed(nameof(IsGrid));
        Changed(nameof(ShowCategories));
        if (_active)
            _ = SearchAsync();
    }

    public void Update(string query, bool active)
    {
        if (_query == query && _active == active)
            return;
        _query = query;
        _active = active;
        _selectedCategoryIndex = 0;
        _selectInitialCategory = true;
        Changed(nameof(SelectedCategoryIndex));
        Changed(nameof(SelectedCategory));
        _search?.Cancel();
        _version++;
        _variants = false;
        _normalCandidates = [];
        _variantParent = null;
        Changed(nameof(IsChoosingVariant));
        Changed(nameof(CanShowVariants));
        Changed(nameof(ShowCategories));
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
            var grid = IsGrid;
            var enablePinyin = _strings.Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
            var matches = await Task.Run(() => grid && string.IsNullOrWhiteSpace(query)
                ? index.Entries.DistinctBy(e => e.VariantGroup).Select(e => new EmojiMatch(index.ApplySkinTone(e, tone), "", 0, 0)).ToArray()
                : index.Search(query, usage, tone, maxResults: grid ? int.MaxValue : 12, cancellationToken: cts.Token,
                    enablePinyin: enablePinyin), cts.Token);
            if (version != _version || !_active)
                return;
            _index = index;
            _matches = matches;
            _variants = false;
            Changed(nameof(IsChoosingVariant));
            Changed(nameof(ShowCategories));
            if (Categories.Count == 0)
            {
                Categories = new[] { new EmojiCategoryViewModel("", "") }
                    .Concat(index.Entries.Select(e => e.Group).Distinct().Select(key => new EmojiCategoryViewModel(key, ""))).ToArray();
                Changed(nameof(Categories));
            }
            foreach (var category in Categories)
                category.Title = category.Key.Length == 0 ? HasQuery ? _strings.Emoji.SearchResultsTab : _strings.Emoji.RecentTab
                    : _strings.Emoji.Categories.GetValueOrDefault(category.Key, category.Key);
            if (_selectInitialCategory)
                _selectedCategoryIndex = grid && !HasQuery && usage.Count == 0 ? 1 : 0;
            _selectInitialCategory = false;
            Changed(nameof(SelectedCategoryIndex));
            Changed(nameof(SelectedCategory));
            FilterCandidates();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (version == _version) Status = string.Format(_strings.Emoji.LoadFailed, ex.Message); }
        finally { if (ReferenceEquals(_search, cts)) _search = null; cts.Dispose(); }
    }

    private EmojiCandidateViewModel CreateCandidate(EmojiIndex index, EmojiEntry entry, string matchedTerm = "")
    {
        var chinese = _strings.Language.StartsWith("zh", StringComparison.OrdinalIgnoreCase);
        var title = chinese ? entry.NameZh : entry.NameEn;
        var subtitle = chinese ? entry.NameEn : "";
        if (chinese && !string.IsNullOrWhiteSpace(matchedTerm) && matchedTerm.Trim() != entry.Sequence
            && !title.Contains(matchedTerm, StringComparison.OrdinalIgnoreCase)
            && !subtitle.Contains(matchedTerm, StringComparison.OrdinalIgnoreCase))
            subtitle += " · " + matchedTerm;
        return new(entry, title, subtitle, () => _images.Get(entry.Image), index.GetVariants(entry).Count > 1);
    }

    private void FilterCandidates()
    {
        if (_index is null || !_active)
            return;
        IEnumerable<EmojiMatch> matches = _matches;
        if (IsGrid)
        {
            if (_selectedCategoryIndex > 0)
                matches = matches.Where(m => m.Entry.Group == Categories[_selectedCategoryIndex].Key);
            else if (!HasQuery)
            {
                var bySequence = _index.Entries.ToDictionary(e => e.Sequence);
                matches = _history.Snapshot().OrderByDescending(u => u.LastUsed)
                    .Where(u => bySequence.ContainsKey(u.Sequence))
                    .Select(u => new EmojiMatch(bySequence[u.Sequence], "", 0, 0));
            }
        }
        Replace(matches.Select(m => CreateCandidate(_index, m.Entry, m.MatchedTerm)));
        Status = Candidates.Count == 0
            ? IsGrid && !HasQuery && _selectedCategoryIndex == 0 ? _strings.Emoji.NoRecent : _strings.Emoji.NoResults
            : !IsGrid && !HasQuery ? _strings.Emoji.Recent : "";
    }

    private void Replace(IEnumerable<EmojiCandidateViewModel> candidates)
    {
        Candidates.Clear();
        foreach (var candidate in candidates)
            Candidates.Add(candidate);
        SelectedIndex = Candidates.Count > 0 ? 0 : -1;
        Changed(nameof(HasCandidates));
    }

    public void MoveSelection(int delta) => SelectedIndex = Candidates.Count == 0 ? -1
        : Math.Clamp(SelectedIndex + delta, 0, Candidates.Count - 1);
    public void MoveVertical(int direction)
    {
        if (!IsGrid)
        {
            MoveSelection(direction);
            return;
        }
        if (SelectedIndex < 0 || Candidates.Count == 0)
            return;
        var row = SelectedIndex / GridColumns + direction;
        if (row >= 0 && row <= (Candidates.Count - 1) / GridColumns)
            SelectedIndex = Math.Min(SelectedIndex + direction * GridColumns, Candidates.Count - 1);
    }
    public void MoveCategory(int direction)
    {
        if (ShowCategories && Categories.Count > 0)
            SelectedCategoryIndex = (SelectedCategoryIndex + direction + Categories.Count) % Categories.Count;
    }
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
        Changed(nameof(ShowCategories));
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
        Changed(nameof(ShowCategories));
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
        Changed(nameof(ShowCategories));
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
