using CrispySearchbar.Core.Configuration;

namespace CrispySearchbar.Core.Localization;

/// <summary>
/// 把可见文案里的 {toggle}/{cycle}/{hide}/{execute}/{previous}/{next}
/// 占位符替换为当前实际快捷键的展示文本（如 "Enter"、"Alt+↑"）。
/// </summary>
public static class ShortcutTextFormatter
{
    public static SearchModeTexts Format(SearchModeTexts texts, ShortcutCatalog shortcuts)
        => texts with
        {
            Watermark = Format(texts.Watermark, shortcuts),
            ActionHint = Format(texts.ActionHint, shortcuts),
        };

    public static string Format(string template, ShortcutCatalog shortcuts)
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        return template
            .Replace("{toggle}", shortcuts.GetDisplay(ShortcutAction.ToggleVisibility))
            .Replace("{cycle}", shortcuts.GetDisplay(ShortcutAction.CycleMode))
            .Replace("{hide}", shortcuts.GetDisplay(ShortcutAction.Hide))
            .Replace("{execute}", shortcuts.GetDisplay(ShortcutAction.Execute))
            .Replace("{previous}", shortcuts.GetDisplay(ShortcutAction.SelectPrevious))
            .Replace("{next}", shortcuts.GetDisplay(ShortcutAction.SelectNext));
    }
}