namespace CrispySearchbar.Core.Configuration;

/// <summary>
/// 标记 AppSettings 上可作为可视化设置项编辑的公共属性。
/// 设置窗口由 AppSettingsSchema 反射本特性自动生成编辑行。
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SettingAttribute : Attribute
{
    public SettingAttribute(SettingsSection section, SettingEditorKind editorKind)
    {
        Section = section;
        EditorKind = editorKind;
    }

    public SettingsSection Section { get; }

    public SettingEditorKind EditorKind { get; }

    /// <summary>同类（Section）内的展示顺序，从小到大。</summary>
    public int Order { get; set; }

    public SettingValidation Validation { get; set; }
}

/// <summary>
/// 为“候选值不是枚举”的 Choice 字段提供候选值，
/// 例如 Language 使用 BCP 47 字符串而非枚举。
/// </summary>
[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
public sealed class SettingValuesAttribute : Attribute
{
    public string[] Values { get; set; } = [];
}
