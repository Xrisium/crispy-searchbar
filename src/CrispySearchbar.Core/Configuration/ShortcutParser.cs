using System.Diagnostics.CodeAnalysis;

namespace CrispySearchbar.Core.Configuration;

/// <summary>
/// 在“修饰键+键名”规范文本与 <see cref="ShortcutBinding"/> 间转换。
/// 空字符串/空值表示“未设定快捷键”；非空值必须由 <see cref="TryParse"/> 解析。
/// </summary>
public static class ShortcutParser
{
    private static readonly IReadOnlyDictionary<string, string> ModifierAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ctrl"] = "Control",
            ["control"] = "Control",
            ["alt"] = "Alt",
            ["shift"] = "Shift",
            ["win"] = "Win",
            ["windows"] = "Win",
            ["meta"] = "Win",
            ["super"] = "Win",
            ["cmd"] = "Win",
        };

    private static readonly IReadOnlyDictionary<string, string> KeyAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["space"] = "Space",
            ["spacebar"] = "Space",
            ["tab"] = "Tab",
            ["esc"] = "Esc",
            ["escape"] = "Esc",
            ["enter"] = "Enter",
            ["return"] = "Enter",
            ["up"] = "Up",
            ["uparrow"] = "Up",
            ["down"] = "Down",
            ["downarrow"] = "Down",
            ["left"] = "Left",
            ["leftarrow"] = "Left",
            ["right"] = "Right",
            ["rightarrow"] = "Right",
            ["home"] = "Home",
            ["end"] = "End",
            ["pageup"] = "PageUp",
            ["pgup"] = "PageUp",
            ["pagedown"] = "PageDown",
            ["pgdn"] = "PageDown",
            ["insert"] = "Insert",
            ["ins"] = "Insert",
            ["delete"] = "Delete",
            ["del"] = "Delete",
            ["backspace"] = "Backspace",
            ["back"] = "Backspace",
            ["minus"] = "-",
            ["hyphen"] = "-",
            ["oemminus"] = "-",
            ["equal"] = "=",
            ["oemplus"] = "=",
            ["leftbracket"] = "[",
            ["oemopenbrackets"] = "[",
            ["rightbracket"] = "]",
            ["oemclosebrackets"] = "]",
            ["backslash"] = "\\",
            ["oem5"] = "\\",
            ["semicolon"] = ";",
            ["oem1"] = ";",
            ["quote"] = "'",
            ["apostrophe"] = "'",
            ["comma"] = ",",
            ["oemcomma"] = ",",
            ["period"] = ".",
            ["oemperiod"] = ".",
            ["slash"] = "/",
            ["oem2"] = "/",
            ["grave"] = "`",
            ["backquote"] = "`",
            ["oem3"] = "`",
            ["xbutton1"] = "XButton1",
            ["mousebutton1"] = "XButton1",
            ["xbutton2"] = "XButton2",
            ["mousebutton2"] = "XButton2",
        };

    public static bool IsEmpty([NotNullWhen(false)] string? text)
        => string.IsNullOrWhiteSpace(text);

    public static bool TryParse(string? text, out ShortcutBinding binding)
    {
        binding = default!;
        if (IsEmpty(text))
        {
            return false;
        }

        var modifiers = ShortcutModifiers.None;
        string? key = null;
        foreach (var rawPart in text.Split('+'))
        {
            var part = rawPart.Trim();
            if (part.Length == 0)
            {
                return false;
            }

            if (ModifierAliases.TryGetValue(part, out var modifierName))
            {
                var modifier = modifierName switch
                {
                    "Alt" => ShortcutModifiers.Alt,
                    "Control" => ShortcutModifiers.Control,
                    "Shift" => ShortcutModifiers.Shift,
                    "Win" => ShortcutModifiers.Win,
                    _ => ShortcutModifiers.None,
                };
                if (modifier == ShortcutModifiers.None
                    || modifiers.HasFlag(modifier))
                {
                    return false;
                }

                modifiers |= modifier;
                continue;
            }

            if (key is not null)
            {
                return false;
            }

            key = NormalizeKeyToken(part);
            if (key is null)
            {
                return false;
            }
        }

        if (key is null)
        {
            return false;
        }

        binding = new ShortcutBinding(modifiers, key);
        return true;
    }

    /// <summary>是否属于会向文本框输入内容的键（字母/数字/空格/主键盘标点）。</summary>
    public static bool IsTypingKey(string keyToken)
    {
        if (keyToken.Length == 1)
        {
            var c = keyToken[0];
            return c is >= ' ' and <= '~';
        }

        return string.Equals(keyToken, "Space", StringComparison.Ordinal);
    }

    public static bool IsFunctionKey(string keyToken)
        => keyToken.Length is >= 2 and <= 3
            && keyToken[0] == 'F'
            && int.TryParse(keyToken.AsSpan(1), out var number)
            && number is >= 1 and <= 24;

    private static string? NormalizeKeyToken(string token)
    {
        if (token.Length == 1)
        {
            var c = token[0];
            if (char.IsAsciiLetter(c))
            {
                return char.ToUpperInvariant(c).ToString();
            }

            return c is >= '!' and <= '~' ? token : null;
        }

        if (KeyAliases.TryGetValue(token, out var canonical))
        {
            return canonical;
        }

        if (token.Length is >= 2 and <= 3 && token[0] is 'F' or 'f'
            && int.TryParse(token.AsSpan(1), out var functionNumber)
            && functionNumber is >= 1 and <= 24)
        {
            return "F" + functionNumber;
        }

        return null;
    }
}