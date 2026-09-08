namespace CrispySearchbar.Core.Configuration;

/// <summary>把 settings.json 中无法解析的快捷键恢复为对应默认值。</summary>
public static class ShortcutNormalizer
{
    public static bool Normalize(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var changed = false;
        foreach (var action in Enum.GetValues<ShortcutAction>())
        {
            var current = ShortcutDefaults.GetValue(settings, action);
            if (!ShortcutParser.TryParse(current, out _))
            {
                ShortcutDefaults.SetValue(
                    settings,
                    action,
                    ShortcutDefaults.GetDefaultValue(action));
                changed = true;
            }
        }

        return changed;
    }
}