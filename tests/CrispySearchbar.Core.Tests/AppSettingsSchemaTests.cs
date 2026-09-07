using System.Reflection;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class AppSettingsSchemaTests
{
    [Fact]
    public void Discover_ReturnsEveryPublicWritableAppSettingsProperty()
    {
        var expected = typeof(AppSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.CanRead && property.CanWrite)
            .Select(property => property.Name)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        var actual = AppSettingsSchema
            .Discover()
            .Select(definition => definition.PropertyName)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void LanguageField_UsesSupportedValues()
    {
        var language = AppSettingsSchema.Discover()
            .Single(definition => definition.PropertyName == nameof(AppSettings.Language));

        Assert.Equal(SettingsSection.General, language.Section);
        Assert.Equal(SettingEditorKind.Choice, language.EditorKind);
        Assert.Equal(
            AppLanguage.Supported.Cast<object>().OrderBy(value => value?.ToString()),
            language.OptionValues.OrderBy(value => value?.ToString()));
    }

    [Fact]
    public void EditorKinds_MatchPropertyTypes()
    {
        foreach (var definition in AppSettingsSchema.Discover())
        {
            switch (definition.EditorKind)
            {
                case SettingEditorKind.Choice:
                    Assert.True(
                        definition.PropertyType.IsEnum
                        || (definition.PropertyType == typeof(string) && definition.OptionValues.Count > 0),
                        definition.PropertyName);
                    break;
                case SettingEditorKind.Toggle:
                    Assert.Equal(typeof(bool), definition.PropertyType);
                    break;
                case SettingEditorKind.Text:
                case SettingEditorKind.FilePath:
                    Assert.Equal(typeof(string), definition.PropertyType);
                    break;
                default:
                    Assert.Fail($"未知控件类型：{definition.EditorKind}");
                    break;
            }
        }
    }

    [Fact]
    public void DiscoveredSections_AreKnownDisplayCategories()
    {
        var sections = AppSettingsSchema.Discover()
            .Select(definition => definition.Section)
            .Distinct()
            .ToArray();

        Assert.All(
            sections,
            section => Assert.Contains(section, Enum.GetValues<SettingsSection>()));
    }

    [Fact]
    public void DisplayCategories_KeepAboutLast()
    {
        Assert.Equal(
            new[]
            {
                SettingsSection.General,
                SettingsSection.Appearance,
                SettingsSection.Search,
                SettingsSection.Dictionary,
                SettingsSection.About,
            },
            Enum.GetValues<SettingsSection>());
    }

    [Fact]
    public void DictionaryPathFields_ExposeSupportedFileTypeFilters()
    {
        var definitions = AppSettingsSchema.Discover();
        var chineseToEnglish = definitions.Single(
            definition => definition.PropertyName == nameof(AppSettings.DictionaryFilePath));
        var englishToChinese = definitions.Single(
            definition => definition.PropertyName == nameof(AppSettings.EcdictFilePath));

        Assert.Equal(SettingFileFilterKeys.CcCedict, chineseToEnglish.FileTypeFilterKey);
        Assert.Contains("*.u8", chineseToEnglish.FileTypePatterns);
        Assert.Equal(SettingFileFilterKeys.Ecdict, englishToChinese.FileTypeFilterKey);
        Assert.Contains("*.csv", englishToChinese.FileTypePatterns);
    }
}
