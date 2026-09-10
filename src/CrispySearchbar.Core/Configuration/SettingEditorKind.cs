namespace CrispySearchbar.Core.Configuration;

/// <summary>设置编辑行的控件种类。</summary>
public enum SettingEditorKind
{
    Choice,
    Toggle,
    Text,
    FilePath,
    ModeList,
    ShortcutKey,

    /// <summary>搜索框位置微调：水平/垂直数字输入加“恢复默认位置”按钮。</summary>
    Offset,
}
