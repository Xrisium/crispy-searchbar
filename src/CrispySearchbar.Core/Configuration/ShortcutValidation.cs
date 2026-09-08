namespace CrispySearchbar.Core.Configuration;

/// <summary>快捷键问题的严重级别：Error 阻止保存；Warning 仅提示。</summary>
public enum ShortcutSeverity
{
    Error,
    Warning,
}

/// <summary>一次快捷键校验结果，由设置界面映射为红字或黄字。</summary>
public sealed record ShortcutIssue(
    string PropertyName,
    ShortcutSeverity Severity,
    string ErrorKey);

/// <summary>
/// 快捷键合法性规则：空键位合法；应用内重复/保留组合为 Error；
/// 可打印键缺少修饰键为 Warning。全局外部占用由界面层异步探测并按 Warning 展示。
/// </summary>
public static class ShortcutValidation
{
    public const string InvalidError = "ShortcutInvalid";

    public const string DuplicateError = "ShortcutDuplicate";

    public const string PrintableRequiresModifierError = "ShortcutPrintableRequiresModifier";

    public const string ModeSwitchRequiresSingleKeyError = "ShortcutModeSwitchRequiresSingleKey";

    public const string ReservedForTextEditingError = "ShortcutReservedForTextEditing";

    public static ShortcutIssue? ValidateCandidate(ShortcutAction action, string? raw)
    {
        if (ShortcutParser.IsEmpty(raw))
        {
            return null;
        }

        if (!ShortcutParser.TryParse(raw, out var binding))
        {
            return Issue(action, ShortcutSeverity.Error, InvalidError);
        }

        if (action == ShortcutAction.CycleMode)
        {
            if (binding.Modifiers != ShortcutModifiers.None)
            {
                return Issue(
                    action,
                    ShortcutSeverity.Error,
                    ModeSwitchRequiresSingleKeyError);
            }

            if (ShortcutParser.IsTypingKey(binding.Key))
            {
                return Issue(
                    action,
                    ShortcutSeverity.Warning,
                    PrintableRequiresModifierError);
            }

            return null;
        }

        if (ShortcutParser.IsTypingKey(binding.Key)
            && !HasNonShiftModifier(binding))
        {
            return Issue(
                action,
                ShortcutSeverity.Warning,
                PrintableRequiresModifierError);
        }

        if (action != ShortcutAction.ToggleVisibility
            && IsReservedTextEditingBinding(binding))
        {
            return Issue(
                action,
                ShortcutSeverity.Error,
                ReservedForTextEditingError);
        }

        return null;
    }

    /// <summary>
    /// 对设置面板当前各行值做整体校验（调用方传入 UI 顺序与当前编辑值，而非磁盘配置）。
    /// 重复键按出现顺序把后一项标为 Error。
    /// </summary>
    public static IReadOnlyList<ShortcutIssue> ValidatePanel(
        IEnumerable<(string PropertyName, string Raw)> values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var byProperty = values.ToDictionary(
            pair => pair.PropertyName,
            pair => pair.Raw,
            StringComparer.Ordinal);
        var issues = new List<ShortcutIssue>();
        var seenBindings = new Dictionary<ShortcutBinding, ShortcutAction>();
        foreach (var action in Enum.GetValues<ShortcutAction>())
        {
            var propertyName = ShortcutDefaults.GetPropertyName(action);
            if (!byProperty.TryGetValue(propertyName, out var raw)
                || ShortcutParser.IsEmpty(raw))
            {
                continue;
            }

            if (!ShortcutParser.TryParse(raw, out var binding))
            {
                issues.Add(new ShortcutIssue(
                    propertyName,
                    ShortcutSeverity.Error,
                    InvalidError));
                continue;
            }

            if (seenBindings.ContainsKey(binding))
            {
                issues.Add(new ShortcutIssue(
                    propertyName,
                    ShortcutSeverity.Error,
                    DuplicateError));
                continue;
            }

            seenBindings[binding] = action;
            if (ValidateCandidate(action, raw) is { } issue)
            {
                issues.Add(issue);
            }
        }

        return issues;
    }

    public static IReadOnlyList<SettingValidationError> Validate(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return ValidatePanel(Enum.GetValues<ShortcutAction>()
            .Select(action => (
                ShortcutDefaults.GetPropertyName(action),
                ShortcutDefaults.GetValue(settings, action))))
            .Where(issue => issue.Severity == ShortcutSeverity.Error)
            .Select(issue => new SettingValidationError(
                issue.PropertyName,
                issue.ErrorKey))
            .ToArray();
    }

    private static ShortcutIssue Issue(
        ShortcutAction action,
        ShortcutSeverity severity,
        string errorKey)
        => new(
            ShortcutDefaults.GetPropertyName(action),
            severity,
            errorKey);

    private static bool HasNonShiftModifier(ShortcutBinding binding)
        => (binding.Modifiers
            & (ShortcutModifiers.Alt | ShortcutModifiers.Control | ShortcutModifiers.Win))
            != ShortcutModifiers.None;

    private static bool IsReservedTextEditingBinding(ShortcutBinding binding)
    {
        if (!binding.Modifiers.HasFlag(ShortcutModifiers.Control))
        {
            return false;
        }

        return binding.Key is "A" or "C" or "V" or "X" or "Z"
            or "Backspace" or "Delete" or "Left" or "Right"
            or "Up" or "Down" or "Home" or "End";
    }
}