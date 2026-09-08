using CrispySearchbar.Core.Configuration;

namespace CrispySearchbar.Core.Localization;

/// <summary>
/// 设置窗口的本地化文案。
/// 字段级文案按属性名/候选值键控，便于 AppSettingsSchema 自动生成任意新增设置行。
/// </summary>
public sealed class AppSettingsTexts
{
    public required string WindowTitle { get; init; }

    public required string Save { get; init; }

    public required string Browse { get; init; }

    public required string Clear { get; init; }

    public required string SavedStatus { get; init; }

    public required string ValidationFailedTemplate { get; init; }

    public required string ConfigFileLabel { get; init; }

    public required string OpenConfigFile { get; init; }

    public required string SaveFailedTemplate { get; init; }

    public required string UrlTemplateRequiredError { get; init; }

    public required string UrlTemplatePlaceholderError { get; init; }

    public required string AboutIntro { get; init; }

    public required string AboutVersionFormat { get; init; }

    public required string AboutLicenseLine { get; init; }

    public required string AboutGitHubLinkLabel { get; init; }

    public required string AboutThirdPartyNoticesLinkLabel { get; init; }

    public required string ResetConfigText { get; init; }

    public required string ResetConfigDescription { get; init; }

    public required string ResetConfirmTitle { get; init; }

    public required string ResetConfirmMessage { get; init; }

    public required string ResetConfirmAcceptText { get; init; }

    public required string ResetConfirmCancelText { get; init; }

    public required string ResetDoneStatus { get; init; }

    public required string ModeListAtLeastOneEnabledError { get; init; }

    public required string ModeMoveUpToolTip { get; init; }

    public required string ModeMoveDownToolTip { get; init; }

    public required string ModeDragHandleToolTip { get; init; }

    public required IReadOnlyDictionary<string, string> SectionTitles { get; init; }

    public required IReadOnlyDictionary<string, string> FieldLabels { get; init; }

    public required IReadOnlyDictionary<string, string> FieldDescriptions { get; init; }

    public required IReadOnlyDictionary<string, string> OptionLabels { get; init; }

    public required IReadOnlyDictionary<string, string> FileTypeFilterNames { get; init; }

    public string GetSectionTitle(SettingsSection section)
        => SectionTitles.TryGetValue(section.ToString(), out var title)
            ? title
            : section.ToString();

    public string GetFieldLabel(string propertyName)
        => FieldLabels.TryGetValue(propertyName, out var label)
            ? label
            : propertyName;

    public string? GetFieldDescription(string propertyName)
        => FieldDescriptions.TryGetValue(propertyName, out var description)
            ? description
            : null;

    public string GetOptionLabel(string propertyName, object? optionValue)
    {
        var key = propertyName + "." + optionValue;
        return OptionLabels.TryGetValue(key, out var label)
            ? label
            : optionValue?.ToString() ?? string.Empty;
    }

    public string GetFileTypeFilterName(string key)
        => FileTypeFilterNames.TryGetValue(key, out var label)
            ? label
            : key;

    public string FormatSaveFailed(string? detail)
        => string.Format(SaveFailedTemplate, detail);

    public string FormatValidationFailed(string sectionTitle, string fieldLabel)
        => string.Format(ValidationFailedTemplate, sectionTitle, fieldLabel);

    public string FormatAboutVersion(string version)
        => string.Format(AboutVersionFormat, version);
}
