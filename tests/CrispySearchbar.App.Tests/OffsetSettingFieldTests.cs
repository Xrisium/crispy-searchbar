using System.Globalization;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>搜索框位置设置行：文本解析、夹取、失焦规范化与“恢复默认位置”。</summary>
public class OffsetSettingFieldTests
{
    [Fact]
    public void LoadFrom_ReflectsConfiguredOffset()
    {
        var field = CreateField();

        field.LoadFrom(new AppSettings { SearchBarOffset = new ScreenOffset(160, -90) });

        Assert.Equal("160", field.XText);
        Assert.Equal("-90", field.YText);
        Assert.Equal(160, field.X);
        Assert.Equal(-90, field.Y);
        Assert.False(field.IsDefault);
    }

    [Fact]
    public void ApplyTo_WritesParsedWholeNumbers()
    {
        var field = CreateField();
        field.XText = " 12 ";
        field.YText = "-7";
        var settings = new AppSettings();

        field.ApplyTo(settings);

        Assert.Equal(new ScreenOffset(12, -7), settings.SearchBarOffset);
    }

    [Fact]
    public void ResetToDefault_ReturnsToCenteredPosition()
    {
        var field = CreateField();
        field.LoadFrom(new AppSettings { SearchBarOffset = new ScreenOffset(220, 140) });

        field.ResetToDefault();

        Assert.Equal("0", field.XText);
        Assert.Equal("0", field.YText);
        Assert.True(field.IsDefault);

        var settings = new AppSettings();
        field.ApplyTo(settings);
        Assert.Equal(ScreenOffset.Default, settings.SearchBarOffset);
    }

    [Fact]
    public void OutOfRangeValues_AreClampedToAllowedRange()
    {
        var field = CreateField();

        field.XText = "9000";
        field.YText = "-9000";

        Assert.Equal(OffsetSettingFieldViewModel.MaxOffset, field.X);
        Assert.Equal(OffsetSettingFieldViewModel.MinOffset, field.Y);

        field.NormalizeText();
        Assert.Equal(
            OffsetSettingFieldViewModel.MaxOffset.ToString(CultureInfo.InvariantCulture),
            field.XText);
        Assert.Equal(
            OffsetSettingFieldViewModel.MinOffset.ToString(CultureInfo.InvariantCulture),
            field.YText);
    }

    [Fact]
    public void InvalidOrEmptyInput_KeepsPreviousValueUntilNormalized()
    {
        var field = CreateField();
        field.XText = "80";

        field.XText = string.Empty;
        Assert.Equal(80, field.X);

        field.XText = "abc";
        Assert.Equal(80, field.X);

        field.NormalizeText();
        Assert.Equal("80", field.XText);
        Assert.Equal(80, field.X);
    }

    private static OffsetSettingFieldViewModel CreateField()
    {
        var definition = AppSettingsSchema.Discover()
            .Single(item => item.PropertyName == nameof(AppSettings.SearchBarOffset));
        var texts = TranslationCatalog.Default.Resolve(AppLanguage.English).SettingsTexts;
        return new OffsetSettingFieldViewModel(definition, texts);
    }
}
