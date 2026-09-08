using CrispySearchbar.Core.Configuration;

namespace CrispySearchbar.Core.Localization;

/// <summary>
/// 把可见文案里的 {toggle}/{cycle}/{execute}/{previous}/{next}
/// 占位符替换为当前实际快捷键展示文本；未设定的动作替换为传入的“未设定”文案。
/// </summary>
public static class ShortcutTextFormatter
{
    public static SearchModeTexts Format(
        SearchModeTexts texts,
        ShortcutCatalog shortcuts,
        string unsetText)
        => texts with
        {
            Watermark = Format(texts.Watermark, shortcuts, unsetText),
            ActionHint = Format(texts.ActionHint, shortcuts, unsetText),
        };

    public static string Format(
        string template,
        ShortcutCatalog shortcuts,
        string unsetText = "")
    {
        if (string.IsNullOrEmpty(template))
        {
            return template;
        }

        return template
            .Replace("{toggle}", GetLabel(ShortcutAction.ToggleVisibility))
            .Replace("{cycle}", GetLabel(ShortcutAction.CycleMode))
            .Replace("{execute}", GetLabel(ShortcutAction.Execute))
            .Replace("{previous}", GetLabel(ShortcutAction.SelectPrevious))
            .Replace("{next}", GetLabel(ShortcutAction.SelectNext));

        string GetLabel(ShortcutAction action)
            => shortcuts.IsBound(action)
                ? shortcuts.GetDisplay(action)
                : unsetText;
    }
}