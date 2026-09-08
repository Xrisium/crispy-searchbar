namespace CrispySearchbar.Core.Configuration;

/// <summary>
/// 快捷键合法性规则：阻止会挡住输入的键位、防止重复，并保证模式切换键保留长按语义。
/// 返回的 ErrorKey 由设置界面按语言映射文案。
/// </summary>
public static class ShortcutValidation
{
    public const string InvalidError = "ShortcutInvalid";

    public const string DuplicateError = "ShortcutDuplicate";

    public const string PrintableRequiresModifierError = "ShortcutPrintableRequiresModifier";

    public const string ModeSwitchRequiresSingleKeyError = "ShortcutModeSwitchRequiresSingleKey";

    public const string ReservedForTextEditingError = "ShortcutReservedForTextEditing";

    public static string? ValidateCandidate(ShortcutAction action, string? raw)
    {
        if (!ShortcutParser.TryParse(raw, out var binding))
        {
            return InvalidError;
        }

        if (action == ShortcutAction.CycleMode)
        {
            if (binding.Modifiers != ShortcutModifiers.None)
            {
                return ModeSwitchRequiresSingleKeyError;
            }

            if (ShortcutParser.IsTypingKey(binding.Key))
            {
                return PrintableRequiresModifierError;
            }

            return null;
        }

        if (ShortcutParser.IsTypingKey(binding.Key)
            && !HasNonShiftModifier(binding))
        {
            return PrintableRequiresModifierError;
        }

        if (action != ShortcutAction.ToggleVisibility
            && IsReservedTextEditingBinding(binding))
        {
            return ReservedForTextEditingError;
        }

        return null;
    }

    public static IReadOnlyList<SettingValidationError> Validate(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var errors = new List<SettingValidationError>();
        var seenBindings = new Dictionary<ShortcutBinding, ShortcutAction>();
        foreach (var action in Enum.GetValues<ShortcutAction>())
        {
            var propertyName = ShortcutDefaults.GetPropertyName(action);
            var raw = ShortcutDefaults.GetValue(settings, action);
            if (!ShortcutParser.TryParse(raw, out var binding))
            {
                errors.Add(new SettingValidationError(propertyName, InvalidError));
                continue;
            }

            if (seenBindings.TryGetValue(binding, out _))
            {
                errors.Add(new SettingValidationError(propertyName, DuplicateError));
                continue;
            }

            seenBindings[binding] = action;
            if (ValidateCandidate(action, raw) is { } errorKey)
            {
                errors.Add(new SettingValidationError(propertyName, errorKey));
            }
        }

        return errors;
    }

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