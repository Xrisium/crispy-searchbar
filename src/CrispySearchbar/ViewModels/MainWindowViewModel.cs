using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CrispySearchbar.Core.Dictionary;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.Core.Search;
using CrispySearchbar.Dictionary;

namespace CrispySearchbar.ViewModels;

/// <summary>主窗口视图模型：负责当前模式、查询文本与执行动作。</summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    public const int DictionaryMaxResults = 12;

    private const string DictionaryModeKey = "dictionary";
    private const string EmptyDictionaryHint = "输入英文单词或中文词语，↑/↓ 选择，Enter 查看释义";
    private const string LoadingDictionaryHint = "正在加载词典数据…";

    private readonly IReadOnlyList<SearchMode> _modes;
    private readonly Action<string> _openUrl;
    private readonly Func<Task<DictionaryLoadResult>>? _loadDictionary;
    private readonly bool _clearQueryOnHide;

    private int _modeIndex;
    private string _query = string.Empty;

    private Task<DictionaryLoadResult>? _dictionaryLoadTask;
    private CancellationTokenSource? _dictionaryQueryCts;
    private int _dictionarySearchVersion;
    private IReadOnlyList<DictionaryCandidateViewModel> _dictionaryMatches = Array.Empty<DictionaryCandidateViewModel>();
    private int _dictionarySelectedIndex = -1;
    private DictionaryCandidateViewModel? _dictionaryDetailHit;
    private string _dictionaryHint = EmptyDictionaryHint;

    public MainWindowViewModel(
        IReadOnlyList<SearchMode> modes,
        Action<string> openUrl,
        bool clearQueryOnHide = true,
        Func<Task<DictionaryLoadResult>>? loadDictionary = null)
    {
        _modes = modes;
        _openUrl = openUrl;
        _clearQueryOnHide = clearQueryOnHide;
        _loadDictionary = loadDictionary;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SearchMode CurrentMode => _modes[_modeIndex];

    public bool IsDictionaryMode => CurrentMode.Key == DictionaryModeKey;

    public bool IsDictionaryPanelOpen => IsDictionaryMode && !IsDictionaryDetailOpen;

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

    /// <summary>Tab 键按模式顺序循环切换。</summary>
    public void CycleMode()
    {
        _modeIndex = (_modeIndex + 1) % _modes.Count;
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
    /// Enter 键执行当前动作。网页/询问 AI 模式在浏览器打开结果；
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

            DictionaryHint = $"词典数据加载失败：{ex.Message}";
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
            return Task.FromResult(DictionaryLoadResult.MissingAll("词典数据源未配置。"));
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
            ? $"没有找到“{query}”的条目"
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
