namespace CrispySearchbar.Core.Configuration;

/// <summary>快捷键默认值、属性名与操作的一一对应关系。</summary>
public static class ShortcutDefaults
{
    private static readonly IReadOnlyDictionary<ShortcutAction, string> PropertyNames =
        new Dictionary<ShortcutAction, string>
        {
            [ShortcutAction.ToggleVisibility] = "ToggleVisibilityShortcut",
            [ShortcutAction.CycleMode] = "CycleModeShortcut",
            [ShortcutAction.Hide] = "HideShortcut",
            [ShortcutAction.Execute] = "ExecuteShortcut",
            [ShortcutAction.SelectPrevious] = "SelectPreviousShortcut",
            [ShortcutAction.SelectNext] = "SelectNextShortcut",
        };

    private static readonly IReadOnlyDictionary<ShortcutAction, string> Values =
        new Dictionary<ShortcutAction, string>
        {
            [ShortcutAction.ToggleVisibility] = "Alt+Space",
            [ShortcutAction.CycleMode] = "Tab",
            [ShortcutAction.Hide] = "Esc",
            [ShortcutAction.Execute] = "Enter",
            [ShortcutAction.SelectPrevious] = "Up",
            [ShortcutAction.SelectNext] = "Down",
        };

    public static string GetPropertyName(ShortcutAction action)
        => PropertyNames[action];

    public static string GetDefaultValue(ShortcutAction action)
        => Values[action];

    public static ShortcutAction? TryGetAction(string propertyName)
    {
        foreach (var pair in PropertyNames)
        {
            if (string.Equals(pair.Value, propertyName, StringComparison.Ordinal))
            {
                return pair.Key;
            }
        }

        return null;
    }

    public static string GetValue(AppSettings settings, ShortcutAction action)
        => action switch
        {
            ShortcutAction.ToggleVisibility => settings.ToggleVisibilityShortcut,
            ShortcutAction.CycleMode => settings.CycleModeShortcut,
            ShortcutAction.Hide => settings.HideShortcut,
            ShortcutAction.Execute => settings.ExecuteShortcut,
            ShortcutAction.SelectPrevious => settings.SelectPreviousShortcut,
            ShortcutAction.SelectNext => settings.SelectNextShortcut,
            _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
        };

    public static void SetValue(AppSettings settings, ShortcutAction action, string value)
    {
        switch (action)
        {
            case ShortcutAction.ToggleVisibility:
                settings.ToggleVisibilityShortcut = value;
                break;
            case ShortcutAction.CycleMode:
                settings.CycleModeShortcut = value;
                break;
            case ShortcutAction.Hide:
                settings.HideShortcut = value;
                break;
            case ShortcutAction.Execute:
                settings.ExecuteShortcut = value;
                break;
            case ShortcutAction.SelectPrevious:
                settings.SelectPreviousShortcut = value;
                break;
            case ShortcutAction.SelectNext:
                settings.SelectNextShortcut = value;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }
    }
}