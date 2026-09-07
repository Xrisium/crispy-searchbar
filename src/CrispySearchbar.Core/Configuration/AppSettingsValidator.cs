namespace CrispySearchbar.Core.Configuration;

/// <summary>一次设置校验的错误结果；文案由界面按 ErrorKey 本地化。</summary>
public sealed record SettingValidationError(string PropertyName, string ErrorKey);

/// <summary>按 AppSettingsSchema 上的校验规则验证一份设置。</summary>
public static class AppSettingsValidator
{
    public const string UrlTemplateRequiredError = "UrlTemplateRequired";

    public const string UrlTemplatePlaceholderMissingError = "UrlTemplatePlaceholderMissing";

    public static IReadOnlyList<SettingValidationError> Validate(AppSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var errors = new List<SettingValidationError>();
        foreach (var definition in AppSettingsSchema.Discover())
        {
            if (definition.Validation != SettingValidation.UrlTemplate)
            {
                continue;
            }

            var value = definition.GetValue(settings) as string;
            if (string.IsNullOrWhiteSpace(value))
            {
                errors.Add(new SettingValidationError(
                    definition.PropertyName,
                    UrlTemplateRequiredError));
            }
            else if (!value.Contains("{0}", StringComparison.Ordinal))
            {
                errors.Add(new SettingValidationError(
                    definition.PropertyName,
                    UrlTemplatePlaceholderMissingError));
            }
        }

        return errors;
    }
}
