using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class AppStringsTests
{
    [Theory]
    [InlineData(AppLanguage.SimplifiedChinese)]
    [InlineData(AppLanguage.English)]
    public void ForSupportedLanguage_ReturnsCompleteTexts(string language)
    {
        var strings = AppStrings.For(language);

        Assert.Equal(language, strings.Language);
        Assert.False(string.IsNullOrWhiteSpace(strings.ShowHideSearchBar));
        Assert.False(string.IsNullOrWhiteSpace(strings.Exit));
        Assert.False(string.IsNullOrWhiteSpace(strings.TrayToolTip));

        Assert.False(string.IsNullOrWhiteSpace(strings.WebSearchMode.Title));
        Assert.False(string.IsNullOrWhiteSpace(strings.WebSearchMode.Watermark));
        Assert.False(string.IsNullOrWhiteSpace(strings.WebSearchMode.ActionHint));
        Assert.False(string.IsNullOrWhiteSpace(strings.AskAiMode.Title));
        Assert.False(string.IsNullOrWhiteSpace(strings.AskAiMode.Watermark));
        Assert.False(string.IsNullOrWhiteSpace(strings.AskAiMode.ActionHint));
        Assert.False(string.IsNullOrWhiteSpace(strings.DictionaryMode.Title));
        Assert.False(string.IsNullOrWhiteSpace(strings.DictionaryMode.Watermark));
        Assert.False(string.IsNullOrWhiteSpace(strings.DictionaryMode.ActionHint));

        Assert.False(string.IsNullOrWhiteSpace(strings.DictionaryEmptyHint));
        Assert.False(string.IsNullOrWhiteSpace(strings.DictionaryLoadingHint));
        Assert.False(string.IsNullOrWhiteSpace(strings.DictionaryDataSourceNotConfigured));
        Assert.Contains("{0}", strings.DictionaryNoResultsTemplate);
        Assert.Contains("{0}", strings.DictionaryLoadFailedTemplate);
        Assert.Contains("{0}", strings.DictionaryReadFailedTemplate);
        Assert.Contains("{0}", strings.ConfiguredDictionaryFileMissingTemplate);
        Assert.Contains("{0}", strings.DictionaryDataFileNotFoundTemplate);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fr")]
    [InlineData("zh-CN")]
    public void UnsupportedOrMissingLanguage_FallsBackToSimplifiedChinese(string? language)
    {
        var strings = AppStrings.For(language);

        Assert.Equal(AppLanguage.SimplifiedChinese, strings.Language);
        Assert.Equal("网页搜索", strings.WebSearchMode.Title);
    }

    [Fact]
    public void Normalize_IsCaseInsensitive()
    {
        Assert.Equal(AppLanguage.English, AppLanguage.Normalize("EN"));
        Assert.Equal(AppLanguage.SimplifiedChinese, AppLanguage.Normalize("ZH-HANS"));
    }

    [Fact]
    public void FormatDictionaryNoResults_EmbedsQuery()
    {
        var chinese = AppStrings.SimplifiedChinese.FormatDictionaryNoResults("apple");
        Assert.Contains("apple", chinese);

        var english = AppStrings.English.FormatDictionaryNoResults("apple");
        Assert.Contains("apple", english);
    }

    [Fact]
    public void SearchEngineDisplayNames_AreLocalized()
    {
        Assert.Equal("百度", AppStrings.SimplifiedChinese.GetSearchEngineDisplayName(SearchEngineKind.Baidu));
        Assert.Equal("Baidu", AppStrings.English.GetSearchEngineDisplayName(SearchEngineKind.Baidu));
    }

    [Fact]
    public void SettingsTexts_CoverEverySchemaFieldAndOptionInBothLanguages()
    {
        var chinese = AppStrings.SimplifiedChinese.SettingsTexts;
        var english = AppStrings.English.SettingsTexts;

        Assert.Equal(
            chinese.FieldLabels.Keys.OrderBy(key => key, StringComparer.Ordinal),
            english.FieldLabels.Keys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal(
            chinese.FieldDescriptions.Keys.OrderBy(key => key, StringComparer.Ordinal),
            english.FieldDescriptions.Keys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal(
            chinese.OptionLabels.Keys.OrderBy(key => key, StringComparer.Ordinal),
            english.OptionLabels.Keys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal(
            chinese.FileTypeFilterNames.Keys.OrderBy(key => key, StringComparer.Ordinal),
            english.FileTypeFilterNames.Keys.OrderBy(key => key, StringComparer.Ordinal));
        Assert.Equal(
            chinese.SectionTitles.Keys.OrderBy(key => key, StringComparer.Ordinal),
            english.SectionTitles.Keys.OrderBy(key => key, StringComparer.Ordinal));

        foreach (var definition in AppSettingsSchema.Discover())
        {
            Assert.True(
                chinese.FieldLabels.ContainsKey(definition.PropertyName),
                $"缺少字段标签：{definition.PropertyName}");
            Assert.True(
                english.FieldLabels.ContainsKey(definition.PropertyName),
                $"缺少字段标签：{definition.PropertyName}");
            Assert.True(
                chinese.FieldDescriptions.ContainsKey(definition.PropertyName),
                $"缺少字段说明：{definition.PropertyName}");
            Assert.True(
                english.FieldDescriptions.ContainsKey(definition.PropertyName),
                $"缺少字段说明：{definition.PropertyName}");

            foreach (var option in definition.OptionValues)
            {
                var key = definition.PropertyName + "." + option;
                Assert.True(chinese.OptionLabels.ContainsKey(key), $"缺少选项文案：{key}");
                Assert.True(english.OptionLabels.ContainsKey(key), $"缺少选项文案：{key}");
            }

            if (definition.EditorKind == SettingEditorKind.FilePath
                && !string.IsNullOrWhiteSpace(definition.FileTypeFilterKey))
            {
                Assert.True(
                    chinese.FileTypeFilterNames.ContainsKey(definition.FileTypeFilterKey),
                    $"缺少文件类型文案：{definition.FileTypeFilterKey}");
                Assert.True(
                    english.FileTypeFilterNames.ContainsKey(definition.FileTypeFilterKey),
                    $"缺少文件类型文案：{definition.FileTypeFilterKey}");
                Assert.True(
                    definition.FileTypePatterns.Count > 0,
                    $"文件类型文案存在但缺少通配符：{definition.PropertyName}");
            }
        }

        foreach (var section in Enum.GetValues<SettingsSection>())
        {
            Assert.True(chinese.SectionTitles.ContainsKey(section.ToString()), $"缺少分类标题：{section}");
            Assert.True(english.SectionTitles.ContainsKey(section.ToString()), $"缺少分类标题：{section}");
        }
    }
}
