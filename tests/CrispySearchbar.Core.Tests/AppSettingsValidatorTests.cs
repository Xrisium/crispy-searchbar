using CrispySearchbar.Core.Configuration;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class AppSettingsValidatorTests
{
    [Fact]
    public void Validate_ValidSettings_ReturnsNoErrors()
    {
        var settings = new AppSettings
        {
            AskAiUrlTemplate = "https://chat.deepseek.com/?q={0}",
        };

        Assert.Empty(AppSettingsValidator.Validate(settings));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_EmptyUrlTemplate_ReturnsRequiredError(string? value)
    {
        var settings = new AppSettings { AskAiUrlTemplate = value ?? string.Empty };

        var error = Assert.Single(AppSettingsValidator.Validate(settings));
        Assert.Equal(nameof(AppSettings.AskAiUrlTemplate), error.PropertyName);
        Assert.Equal(
            AppSettingsValidator.UrlTemplateRequiredError,
            error.ErrorKey);
    }

    [Fact]
    public void Validate_UrlTemplateWithoutPlaceholder_ReturnsPlaceholderError()
    {
        var settings = new AppSettings
        {
            AskAiUrlTemplate = "https://chat.deepseek.com/",
        };

        var error = Assert.Single(AppSettingsValidator.Validate(settings));
        Assert.Equal(nameof(AppSettings.AskAiUrlTemplate), error.PropertyName);
        Assert.Equal(
            AppSettingsValidator.UrlTemplatePlaceholderMissingError,
            error.ErrorKey);
    }

    [Fact]
    public void Validate_EmptyDictionaryPaths_IsAllowed()
    {
        var settings = new AppSettings
        {
            DictionaryFilePath = null,
            EcdictFilePath = null,
        };

        Assert.Empty(AppSettingsValidator.Validate(settings));
    }

    [Fact]
    public void Validate_AllModesDisabled_ReturnsAtLeastOneModeError()
    {
        var settings = new AppSettings
        {
            ModePreferences =
            [
                new ModePreference(ModePreferenceDefaults.WebSearch, enabled: false),
                new ModePreference(ModePreferenceDefaults.Wikipedia, enabled: false),
                new ModePreference(ModePreferenceDefaults.AskAi, enabled: false),
                new ModePreference(ModePreferenceDefaults.Dictionary, enabled: false),
            ],
        };

        var error = Assert.Single(AppSettingsValidator.Validate(settings));
        Assert.Equal(nameof(AppSettings.ModePreferences), error.PropertyName);
        Assert.Equal(
            AppSettingsValidator.AtLeastOneModeEnabledError,
            error.ErrorKey);
    }
}
