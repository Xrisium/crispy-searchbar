using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CrispySearchbar.Core.Dictionary;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.Core.Search;
using CrispySearchbar.Dictionary;

namespace CrispySearchbar.ViewModels;

/// <summary>主窗口视图模型：负责当前模式、查询文本与执行动作。</summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    public const int DictionaryMaxResults = 12;

    private const string DictionaryModeKey = "dictionary";

    private string EmptyDictionaryHint => _dictionaryEmptyHint;

    private string LoadingDictionaryHint => _strings.DictionaryLoadingHint;

    private AppStrings _strings;

    private IReadOnlyList<SearchMode> _modes;
    private readonly Action<string> _openUrl;
    private Func<Task<DictionaryLoadResult>>? _loadDictionary;
    private bool _clearQueryOnHide;
    private string _dictionaryEmptyHint;

    private int _modeIndex;
    private bool _isSearchBarVisible;
    private bool _isModeWheelOpen;
    private int _modeWheelSelectedIndex;
    private string _query = string.Empty;

    private Task<DictionaryLoadResult>? _dictionaryLoadTask;
    private CancellationTokenSource? _dictionaryQueryCts;
    private int _dictionarySearchVersion;
    private IReadOnlyList<DictionaryCandidateViewModel> _dictionaryMatches = Array.Empty<DictionaryCandidateViewModel>();
    private int _dictionarySelectedIndex = -1;
    private DictionaryCandidateViewModel? _dictionaryDetailHit;
    private string _dictionaryHint = string.Empty;

    public MainWindowViewModel(
        AppStrings strings,
        IReadOnlyList<SearchMode> modes,
        Action<string> openUrl,
        bool clearQueryOnHide = true,
        string? dictionaryEmptyHint = null,
        Func<Task<DictionaryLoadResult>>? loadDictionary = null)
    {
        ArgumentNullException.ThrowIfNull(strings);
        _strings = strings;
        _modes = modes;
        _openUrl = openUrl;
        _clearQueryOnHide = clearQueryOnHide;
        _dictionaryEmptyHint = dictionaryEmptyHint ?? strings.DictionaryEmptyHint;
        _loadDictionary = loadDictionary;
        _dictionaryHint = _dictionaryEmptyHint;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>设置保存后由 App 调用，运行期切换语言/模式/隐藏行为，不重建窗口。</summary>
    public void ApplyConfiguration(
        AppStrings strings,
        IReadOnlyList<SearchMode> modes,
        bool clearQueryOnHide,
        string? dictionaryEmptyHint = null)
    {
        ArgumentNullException.ThrowIfNull(strings);
        ArgumentNullException.ThrowIfNull(modes);

        var previousModeKey = _modes.Count > 0 ? _modes[_modeIndex].Key : null;
        _strings = strings;
        _modes = modes;
        _clearQueryOnHide = clearQueryOnHide;
        _dictionaryEmptyHint = dictionaryEmptyHint ?? strings.DictionaryEmptyHint;
        if (_modes.Count > 0)
        {
            var preservedIndex = previousModeKey is null
                ? -1
                : FindModeIndexByKey(previousModeKey);
            _modeIndex = preservedIndex >= 0 ? preservedIndex : 0;
        }

        OnPropertyChanged(nameof(CurrentMode));
        OnPropertyChanged(nameof(Modes));

        if (IsDictionaryMode)
        {
            if (string.IsNullOrWhiteSpace(Query))
            {
                ClearDictionaryState();
                DictionaryHint = EmptyDictionaryHint;
                NotifyDictionaryState();
            }
            else
            {
                _ = SearchDictionaryAsync();
            }
        }
    }

    /// <summary>
    /// 词典路径配置变化后由 App 调用，替换数据源加载任务；
    /// 正在查询/展示的旧数据会被取消，词典模式下新查询再等待新任务。
    /// </summary>
    public void ReloadDictionarySource(Func<Task<DictionaryLoadResult>> loadDictionary)
    {
        ArgumentNullException.ThrowIfNull(loadDictionary);

        _loadDictionary = loadDictionary;
        _dictionaryLoadTask = null;
        if (!IsDictionaryMode)
        {
            return;
        }

        _dictionaryQueryCts?.Cancel();
        _dictionarySearchVersion++;
        ClearDictionarySearchResults();

        if (string.IsNullOrWhiteSpace(Query))
        {
            DictionaryHint = EmptyDictionaryHint;
            NotifyDictionaryState();
        }
        else
        {
            _ = SearchDictionaryAsync();
        }
    }

    public SearchMode CurrentMode => _modes[_modeIndex];

    public IReadOnlyList<SearchMode> Modes => _modes;

    public bool IsModeWheelOpen
    {
        get => _isModeWheelOpen;
        private set
        {
            if (_isModeWheelOpen == value)
            {
                return;
            }

            _isModeWheelOpen = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDictionaryPopupOpen));
        }
    }

    public int ModeWheelSelectedIndex
    {
        get => _modeWheelSelectedIndex;
        set
        {
            if (_modeWheelSelectedIndex == value)
            {
                return;
            }

            _modeWheelSelectedIndex = value;
            OnPropertyChanged();
        }
    }

    public bool IsDictionaryMode => CurrentMode.Key == DictionaryModeKey;

    public bool IsDictionaryPanelOpen => IsDictionaryMode && !IsDictionaryDetailOpen;

    /// <summary>
    /// 候选浮层属于搜索框层级：搜索框本身不可见时，任何候选/详情浮层都不允许打开。
    /// </summary>
    public bool IsDictionaryPopupOpen => IsSearchBarVisible && IsDictionaryMode && !IsModeWheelOpen;

    /// <summary>搜索框窗口当前是否显示；由主窗口在显示/隐藏时同步。</summary>
    public bool IsSearchBarVisible
    {
        get => _isSearchBarVisible;
        private set
        {
            if (_isSearchBarVisible == value)
            {
                return;
            }

            _isSearchBarVisible = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDictionaryPopupOpen));
        }
    }

    public bool IsDictionaryDetailOpen => DictionaryDetailHit is not null;

    public string DictionaryHint
    {
        get => _dictionaryHint;
        private set
        {
            if (_dictionaryHint == value)
            {
                return;
            }

            _dictionaryHint = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<DictionaryCandidateViewModel> DictionaryCandidates { get; } = [];

    public bool HasDictionaryCandidates => DictionaryCandidates.Count > 0;

    public bool ShowDictionaryHint => IsDictionaryPanelOpen && !HasDictionaryCandidates;

    public int DictionarySelectedIndex
    {
        get => _dictionarySelectedIndex;
        set
        {
            if (_dictionarySelectedIndex == value)
            {
                return;
            }

            _dictionarySelectedIndex = value;
            OnPropertyChanged();
        }
    }

    public DictionaryCandidateViewModel? DictionaryDetailHit
    {
        get => _dictionaryDetailHit;
        private set
        {
            if (ReferenceEquals(_dictionaryDetailHit, value))
            {
                return;
            }

            _dictionaryDetailHit = value;
            OnPropertyChanged();
        }
    }

    public string DictionaryDetailTitle => DictionaryDetailHit?.DetailTitle ?? string.Empty;

    public bool DictionaryDetailHasSubtitle => !string.IsNullOrWhiteSpace(DictionaryDetailSubtitle);

    public string DictionaryDetailSubtitle => DictionaryDetailHit?.DetailSubtitle ?? string.Empty;

    /// <summary>释义以“• ”分行预览，避免详情视图引入额外数据模板。</summary>
    public string DictionaryDetailDefinitionText
    {
        get
        {
            var definitions = DictionaryDetailHit?.Definitions;
            if (definitions is null || definitions.Count == 0)
            {
                return string.Empty;
            }

            return string.Join("\n", definitions.Select(definition => "• " + definition));
        }
    }

    public string DictionaryDetailSource => DictionaryDetailHit?.Source ?? string.Empty;

    public string Query
    {
        get => _query;
        set
        {
            if (_query == value)
            {
                return;
            }

            _query = value;
            OnPropertyChanged();

            if (IsDictionaryMode)
            {
                _ = SearchDictionaryAsync();
            }
        }
    }

    /// <summary>快速按 Tab：切换到下一个模式。</summary>
    public void CycleMode() => SetMode((_modeIndex + 1) % _modes.Count);

    /// <summary>长按 Tab 打开模式轮盘，高亮当前模式。</summary>
    public void OpenModeWheel()
    {
        if (IsModeWheelOpen || _modes.Count == 0)
        {
            return;
        }

        IsModeWheelOpen = true;
        ModeWheelSelectedIndex = _modeIndex;
    }

    /// <summary>在轮盘内移动高亮，支持首尾循环。</summary>
    public void MoveModeWheelSelection(int delta)
    {
        var count = _modes.Count;
        if (!IsModeWheelOpen || count == 0)
        {
            return;
        }

        var current = ModeWheelSelectedIndex >= 0 ? ModeWheelSelectedIndex : _modeIndex;
        ModeWheelSelectedIndex = ((current + delta) % count + count) % count;
    }

    /// <summary>松开 Tab / Enter / 点击：切换到指定（或高亮）模式并关闭轮盘。</summary>
    public void CommitModeWheel(SearchMode? mode = null)
    {
        if (!IsModeWheelOpen)
        {
            return;
        }

        IsModeWheelOpen = false;
        var index = mode is null ? ModeWheelSelectedIndex : FindModeIndex(mode);
        if (index >= 0 && index < _modes.Count)
        {
            SetMode(index);
        }
    }

    /// <summary>Esc：关闭轮盘，保持当前模式。</summary>
    public void CancelModeWheel() => IsModeWheelOpen = false;

    private int FindModeIndex(SearchMode mode)
    {
        for (var i = 0; i < _modes.Count; i++)
        {
            if (_modes[i] == mode)
            {
                return i;
            }
        }

        return -1;
    }

    private int FindModeIndexByKey(string key)
    {
        for (var i = 0; i < _modes.Count; i++)
        {
            if (string.Equals(_modes[i].Key, key, StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private void SetMode(int index)
    {
        if (index < 0 || index >= _modes.Count || index == _modeIndex)
        {
            return;
        }

        _modeIndex = index;
        OnPropertyChanged(nameof(CurrentMode));
        OnModeChanged();
    }

    /// <summary>↑/↓ 在词典候选中移动选择。</summary>
    public void MoveDictionarySelection(int delta)
    {
        if (!IsDictionaryMode || IsDictionaryDetailOpen || _dictionaryMatches.Count == 0)
        {
            return;
        }

        var count = _dictionaryMatches.Count;
        var current = DictionarySelectedIndex;
        var next = current < 0
            ? (delta > 0 ? 0 : count - 1)
            : Math.Clamp(current + delta, 0, count - 1);
        DictionarySelectedIndex = next;
    }

    /// <summary>打开当前选择（或第一项）的词典详情。</summary>
    public bool OpenSelectedDictionaryEntry()
    {
        if (!IsDictionaryMode || IsDictionaryDetailOpen || _dictionaryMatches.Count == 0)
        {
            return false;
        }

        var index = DictionarySelectedIndex;
        if (index < 0 || index >= _dictionaryMatches.Count)
        {
            index = 0;
        }

        ShowDictionaryDetail(_dictionaryMatches[index]);
        return true;
    }

    /// <summary>鼠标单击候选行时直接打开对应词条详情。</summary>
    public bool OpenDictionaryCandidate(DictionaryCandidateViewModel candidate)
    {
        var index = candidate is null ? -1 : DictionaryCandidates.IndexOf(candidate);
        if (index < 0)
        {
            return false;
        }

        DictionarySelectedIndex = index;
        return OpenSelectedDictionaryEntry();
    }

    /// <summary>
    /// Enter 键执行当前动作。网页/询问 DeepSeek 模式在浏览器打开结果；
    /// 词典模式打开详情。返回是否应隐藏搜索框。
    /// </summary>
    public bool ExecuteCurrent()
    {
        var mode = CurrentMode;
        if (mode.Key == DictionaryModeKey)
        {
            OpenSelectedDictionaryEntry();
            return false;
        }

        if (mode.UrlTemplate is null)
        {
            return false;
        }

        var query = Query.Trim();
        if (query.Length == 0)
        {
            return false;
        }

        _openUrl(OpenUrlBuilder.Build(mode.UrlTemplate, query));
        return true;
    }

    /// <summary>窗口隐藏时按配置决定是否清空已输入内容。</summary>
    public void OnWindowHidden()
    {
        IsSearchBarVisible = false;
        if (_clearQueryOnHide)
        {
            Query = string.Empty;
            return;
        }

        ClearDictionaryState();
    }

    /// <summary>窗口重新显示时刷新词典状态，避免残留详情或过期候选。</summary>
    public void OnWindowShown()
    {
        IsSearchBarVisible = true;
        if (!IsDictionaryMode)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(Query))
        {
            _ = SearchDictionaryAsync();
        }
        else
        {
            ClearDictionaryState();
            DictionaryHint = EmptyDictionaryHint;
            NotifyDictionaryState();
        }
    }

    private void OnModeChanged()
    {
        if (!IsDictionaryMode)
        {
            ClearDictionaryState();
            return;
        }

        if (!string.IsNullOrWhiteSpace(Query))
        {
            _ = SearchDictionaryAsync();
        }
        else
        {
            ClearDictionaryState();
            DictionaryHint = EmptyDictionaryHint;
            NotifyDictionaryState();
        }
    }

    private async Task SearchDictionaryAsync()
    {
        _dictionaryQueryCts?.Cancel();
        _dictionaryQueryCts = new CancellationTokenSource();
        var cancellation = _dictionaryQueryCts;
        var version = ++_dictionarySearchVersion;

        ClearDictionarySearchResults();
        var query = Query;
        if (string.IsNullOrWhiteSpace(query))
        {
            DictionaryHint = EmptyDictionaryHint;
            NotifyDictionaryState();
            return;
        }

        DictionaryHint = LoadingDictionaryHint;
        NotifyDictionaryState();

        try
        {
            var result = await GetDictionaryLoadResultAsync();
            if (version != _dictionarySearchVersion || cancellation.IsCancellationRequested)
            {
                return;
            }

            // 中文输入特征 → 汉英（CC-CEDICT）；其余按英汉（ECDICT）查询。
            var direction = DictionaryQueryClassifier.Detect(query);
            if (direction == DictionaryQueryDirection.ChineseToEnglish)
            {
                if (result.CedictIndex is null)
                {
                    DictionaryHint = result.CedictMessage ?? EmptyDictionaryHint;
                    NotifyDictionaryState();
                    return;
                }

                var matches = await Task.Run(
                    () => result.CedictIndex.Search(query, DictionaryMaxResults),
                    cancellation.Token);
                if (version != _dictionarySearchVersion || cancellation.IsCancellationRequested)
                {
                    return;
                }

                ApplyChineseMatches(query, matches);
            }
            else
            {
                if (result.EcdictIndex is null)
                {
                    DictionaryHint = result.EcdictMessage ?? EmptyDictionaryHint;
                    NotifyDictionaryState();
                    return;
                }

                var matches = await Task.Run(
                    () => result.EcdictIndex.Search(query, DictionaryMaxResults),
                    cancellation.Token);
                if (version != _dictionarySearchVersion || cancellation.IsCancellationRequested)
                {
                    return;
                }

                ApplyEnglishMatches(query, matches);
            }
        }
        catch (OperationCanceledException)
        {
            // 过期查询/快速连续输入：静默丢弃。
        }
        catch (Exception ex)
        {
            if (version != _dictionarySearchVersion)
            {
                return;
            }

            DictionaryHint = _strings.FormatDictionaryLoadFailed(ex.Message);
            NotifyDictionaryState();
        }
    }

    private Task<DictionaryLoadResult> GetDictionaryLoadResultAsync()
    {
        if (_dictionaryLoadTask is not null)
        {
            return _dictionaryLoadTask;
        }

        if (_loadDictionary is null)
        {
            return Task.FromResult(DictionaryLoadResult.MissingAll(_strings.DictionaryDataSourceNotConfigured));
        }

        // 加载任务由应用启动时创建一次；后续查询只等待同一个已完成任务，不重新加载。
        _dictionaryLoadTask = _loadDictionary();
        return _dictionaryLoadTask;
    }

    private void ApplyChineseMatches(string query, IReadOnlyList<DictionaryEntry> matches)
    {
        var viewModels = matches.Select(entry => (DictionaryCandidateViewModel)new ChineseDictionaryCandidateViewModel(entry)).ToArray();
        ApplyDictionaryMatches(query, viewModels);
    }

    private void ApplyEnglishMatches(string query, IReadOnlyList<EcdictEntry> matches)
    {
        var viewModels = matches.Select(entry => (DictionaryCandidateViewModel)new EnglishDictionaryCandidateViewModel(entry)).ToArray();
        ApplyDictionaryMatches(query, viewModels);
    }

    private void ApplyDictionaryMatches(string query, IReadOnlyList<DictionaryCandidateViewModel> matches)
    {
        DictionaryCandidates.Clear();
        _dictionaryMatches = matches;
        foreach (var match in matches)
        {
            DictionaryCandidates.Add(match);
        }

        DictionarySelectedIndex = matches.Count > 0 ? 0 : -1;
        DictionaryHint = matches.Count == 0
            ? _strings.FormatDictionaryNoResults(query)
            : string.Empty;
        NotifyDictionaryState();
    }

    private void ShowDictionaryDetail(DictionaryCandidateViewModel hit)
    {
        _dictionaryQueryCts?.Cancel();
        _dictionarySearchVersion++;
        ClearDictionarySearchResults();
        DictionaryDetailHit = hit;
        DictionaryHint = string.Empty;
        NotifyDictionaryState();
    }

    private void ClearDictionaryState()
    {
        _dictionaryQueryCts?.Cancel();
        _dictionarySearchVersion++;
        ClearDictionarySearchResults();
        DictionaryHint = EmptyDictionaryHint;
        NotifyDictionaryState();
    }

    private void ClearDictionarySearchResults()
    {
        DictionaryCandidates.Clear();
        _dictionaryMatches = Array.Empty<DictionaryCandidateViewModel>();
        _dictionarySelectedIndex = -1;
        _dictionaryDetailHit = null;
    }

    private void NotifyDictionaryState()
    {
        OnPropertyChanged(nameof(IsDictionaryMode));
        OnPropertyChanged(nameof(IsDictionaryPopupOpen));
        OnPropertyChanged(nameof(IsDictionaryPanelOpen));
        OnPropertyChanged(nameof(IsDictionaryDetailOpen));
        OnPropertyChanged(nameof(HasDictionaryCandidates));
        OnPropertyChanged(nameof(ShowDictionaryHint));
        OnPropertyChanged(nameof(DictionaryHint));
        OnPropertyChanged(nameof(DictionarySelectedIndex));
        OnPropertyChanged(nameof(DictionaryDetailHit));
        OnPropertyChanged(nameof(DictionaryDetailTitle));
        OnPropertyChanged(nameof(DictionaryDetailSubtitle));
        OnPropertyChanged(nameof(DictionaryDetailHasSubtitle));
        OnPropertyChanged(nameof(DictionaryDetailDefinitionText));
        OnPropertyChanged(nameof(DictionaryDetailSource));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
