namespace CrispySearchbar.Core.Configuration;

/// <summary>可自定义快捷键的应用操作。</summary>
public enum ShortcutAction
{
    /// <summary>全局呼出 / 隐藏搜索框。</summary>
    ToggleVisibility,

    /// <summary>轻按切换下一个模式，长按呼出模式轮盘。</summary>
    CycleMode,

    /// <summary>隐藏搜索框（模式轮盘打开时先取消轮盘）。</summary>
    Hide,

    /// <summary>执行当前项。</summary>
    Execute,

    /// <summary>上移候选/轮盘高亮。</summary>
    SelectPrevious,

    /// <summary>下移候选/轮盘高亮。</summary>
    SelectNext,
}