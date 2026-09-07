using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CrispySearchbar.Core.Modes;
using CrispySearchbar.Core.Search;

namespace CrispySearchbar.ViewModels;

/// <summary>主窗口视图模型：负责当前模式、查询文本与执行动作。</summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    private readonly IReadOnlyList<SearchMode> _modes;
    private readonly Action<string> _openUrl;
    private readonly bool _clearQueryOnHide;
    private int _modeIndex;
    private string _query = string.Empty;

    public MainWindowViewModel(
        IReadOnlyList<SearchMode> modes,
        Action<string> openUrl,
        bool clearQueryOnHide = true)
    {
        _modes = modes;
        _openUrl = openUrl;
        _clearQueryOnHide = clearQueryOnHide;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SearchMode CurrentMode => _modes[_modeIndex];

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
        }
    }

    /// <summary>Tab 键按模式顺序循环切换。</summary>
    public void CycleMode()
    {
        _modeIndex = (_modeIndex + 1) % _modes.Count;
        OnPropertyChanged(nameof(CurrentMode));
    }

    /// <summary>窗口隐藏时按配置决定是否清空已输入内容。</summary>
    public void OnWindowHidden()
    {
        if (_clearQueryOnHide)
        {
            Query = string.Empty;
        }
    }

    /// <summary>
    /// Enter 键：对带 URL 模板的模式在默认浏览器打开结果。
    /// 返回是否真正执行了动作，供调用方决定是否隐藏搜索框。
    /// </summary>
    public bool ExecuteCurrent()
    {
        var mode = CurrentMode;
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}


