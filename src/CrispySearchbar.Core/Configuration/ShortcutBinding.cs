namespace CrispySearchbar.Core.Configuration;

/// <summary>
/// 一份规范化快捷键：修饰键 + 规范键名（如 "Space"、"Tab"、"Esc"、"Up"）。
/// 记录相等性用于查重与按键匹配。
/// </summary>
public sealed record ShortcutBinding(ShortcutModifiers Modifiers, string Key)
{
    /// <summary>写入 settings.json / 界面显示的规范文本，如 "Alt+Space"。</summary>
    public string ToStorageString()
        => string.Join("+", ModifierSegments.Append(Key));

    /// <summary>面向用户的展示文本；Up/Down 显示为 ↑/↓，其余键名原样显示。</summary>
    public string ToDisplayString()
        => string.Join("+", ModifierSegments.Append(GetDisplayKey(Key)));

    private IEnumerable<string> ModifierSegments
    {
        get
        {
            if (Modifiers.HasFlag(ShortcutModifiers.Control))
            {
                yield return "Ctrl";
            }

            if (Modifiers.HasFlag(ShortcutModifiers.Alt))
            {
                yield return "Alt";
            }

            if (Modifiers.HasFlag(ShortcutModifiers.Shift))
            {
                yield return "Shift";
            }

            if (Modifiers.HasFlag(ShortcutModifiers.Win))
            {
                yield return "Win";
            }
        }
    }

    private static string GetDisplayKey(string key)
        => key switch
        {
            "Up" => "↑",
            "Down" => "↓",
            _ => key,
        };
}