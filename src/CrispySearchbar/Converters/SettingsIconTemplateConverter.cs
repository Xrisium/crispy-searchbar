using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Styling;

namespace CrispySearchbar.Converters;

/// <summary>把设置大类 Key 映射到 Assets/Styles/SettingsIcons.axaml 中的图标模板。</summary>
public sealed class SettingsIconTemplateConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string sectionKey
            && Application.Current is { } app
            && app.TryGetResource($"SettingsIconTemplate-{sectionKey}", ThemeVariant.Default, out var resource)
            && resource is IDataTemplate template)
        {
            return template;
        }

        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
