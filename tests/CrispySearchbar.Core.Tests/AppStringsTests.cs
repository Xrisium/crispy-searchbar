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
        Assert.False(string.IsNullOrWhiteSpace(strings.OpenConfigFile));
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
}
