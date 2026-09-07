using System.Reflection;

namespace CrispySearchbar.Core.Configuration;

/// <summary>单个可视化设置项的元数据与读写入口。</summary>
public sealed class SettingDefinition
{
    internal SettingDefinition(
        PropertyInfo property,
        SettingsSection section,
        SettingEditorKind editorKind,
        int order,
        SettingValidation validation,
        IReadOnlyList<object?>? optionValues)
    {
        Property = property;
        Section = section;
        EditorKind = editorKind;
        Order = order;
        Validation = validation;
        OptionValues = optionValues ?? Array.Empty<object?>();
    }

    public PropertyInfo Property { get; }

    public string PropertyName => Property.Name;

    public Type PropertyType => Property.PropertyType;

    public SettingsSection Section { get; }

    public SettingEditorKind EditorKind { get; }

    public int Order { get; }

    public SettingValidation Validation { get; }

    /// <summary>Choice 字段的候选值；枚举字段为枚举值，字符串字段来自 SettingValues。</summary>
    public IReadOnlyList<object?> OptionValues { get; }

    public object? GetValue(AppSettings settings)
        => Property.GetValue(settings);

    public void SetValue(AppSettings settings, object? value)
        => Property.SetValue(settings, value);
}

/// <summary>
/// 反射 AppSettings 上带 SettingAttribute 的属性，生成设置界面的渲染数据源。
/// 新增配置项只需注解属性；测试会强制每个公共可写属性都已注解。
/// </summary>
public static class AppSettingsSchema
{
    public static IReadOnlyList<SettingDefinition> Discover()
    {
        var entries = new List<(SettingDefinition Definition, int Order)>();

        foreach (var property in typeof(AppSettings).GetProperties(
            BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite)
            {
                continue;
            }

            var setting = property.GetCustomAttribute<SettingAttribute>()
                ?? throw new InvalidOperationException(
                    $"AppSettings.{property.Name} 缺少 [Setting] 注解，无法在设置界面显示。");

            ValidatePropertyType(property, setting);
            var optionValues = ResolveOptionValues(property, setting);
            entries.Add((
                new SettingDefinition(
                    property,
                    setting.Section,
                    setting.EditorKind,
                    setting.Order,
                    setting.Validation,
                    optionValues),
                setting.Order));
        }

        return entries
            .OrderBy(entry => (int)entry.Definition.Section)
            .ThenBy(entry => entry.Order)
            .ThenBy(entry => entry.Definition.PropertyName, StringComparer.Ordinal)
            .Select(entry => entry.Definition)
            .ToArray();
    }

    private static IReadOnlyList<object?>? ResolveOptionValues(
        PropertyInfo property,
        SettingAttribute setting)
    {
        if (setting.EditorKind != SettingEditorKind.Choice)
        {
            return null;
        }

        if (property.PropertyType.IsEnum)
        {
            return Enum.GetValues(property.PropertyType).Cast<object>().ToArray();
        }

        var values = property.GetCustomAttribute<SettingValuesAttribute>()?.Values;
        if (values is null || values.Length == 0)
        {
            throw new InvalidOperationException(
                $"AppSettings.{property.Name} 是字符串型 Choice，必须提供 [SettingValues]。");
        }

        return values.Cast<object>().ToArray();
    }

    private static void ValidatePropertyType(PropertyInfo property, SettingAttribute setting)
    {
        var type = property.PropertyType;
        var valid = setting.EditorKind switch
        {
            SettingEditorKind.Choice => type.IsEnum || type == typeof(string),
            SettingEditorKind.Toggle => type == typeof(bool),
            SettingEditorKind.Text => type == typeof(string),
            SettingEditorKind.FilePath => type == typeof(string),
            _ => false,
        };

        if (!valid)
        {
            throw new InvalidOperationException(
                $"AppSettings.{property.Name} 的 [Setting(EditorKind={setting.EditorKind})] 与属性类型 {type.Name} 不匹配。");
        }
    }
}
