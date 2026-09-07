namespace CrispySearchbar.Core.Configuration;

/// <summary>
/// 设置界面的大类。顺序即左侧导航与右侧页面的展示顺序。
/// 新增大类时：补充本地化标题、补一枚分类图标 DataTemplate（SettingsIconTemplate-{lowercase key}）。
/// </summary>
public enum SettingsSection
{
    General,
    Appearance,
    Search,
    Dictionary,
}
