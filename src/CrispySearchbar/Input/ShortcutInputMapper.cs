using Avalonia.Input;
using CrispySearchbar.Core.Configuration;

namespace CrispySearchbar.Input;

/// <summary>
/// 把 Avalonia 按键事件转换为与平台无关的 <see cref="ShortcutBinding"/>。
/// 捕获记录与主窗口按键分发共用同一映射，保证同一键显示/触发一致。
/// </summary>
public static class ShortcutInputMapper
{
    public static bool TryCreate(
        Key key,
        KeyModifiers modifiers,
        out ShortcutBinding binding)
    {
        var token = ToKeyToken(key);
        if (token is null)
        {
            binding = null!;
            return false;
        }

        binding = new ShortcutBinding(ToModifiers(modifiers), token);
        return true;
    }

    public static ShortcutModifiers ToModifiers(KeyModifiers modifiers)
    {
        var result = ShortcutModifiers.None;
        if (modifiers.HasFlag(KeyModifiers.Alt))
        {
            result |= ShortcutModifiers.Alt;
        }

        if (modifiers.HasFlag(KeyModifiers.Control))
        {
            result |= ShortcutModifiers.Control;
        }

        if (modifiers.HasFlag(KeyModifiers.Shift))
        {
            result |= ShortcutModifiers.Shift;
        }

        // Avalonia 的 Meta/Command 在 Windows 上对应 Win 键；跨平台键位留给未来实现细化。
        if (modifiers.HasFlag(KeyModifiers.Meta))
        {
            result |= ShortcutModifiers.Win;
        }

        return result;
    }

    public static string? ToKeyToken(Key key)
    {
        if (key >= Key.A && key <= Key.Z)
        {
            return key.ToString();
        }

        if (key >= Key.D0 && key <= Key.D9)
        {
            return ((int)key - (int)Key.D0).ToString();
        }

        if (key >= Key.F1 && key <= Key.F24)
        {
            return "F" + ((int)key - (int)Key.F1 + 1);
        }

        return key switch
        {
            Key.Space => "Space",
            Key.Tab => "Tab",
            Key.Escape => "Esc",
            Key.Enter => "Enter",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.Insert => "Insert",
            Key.Delete => "Delete",
            Key.Back => "Backspace",
            _ => null,
        };
    }
}