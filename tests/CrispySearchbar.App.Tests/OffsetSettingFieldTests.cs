using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.ViewModels;
using Xunit;

namespace CrispySearchbar.App.Tests;

/// <summary>搜索框位置设置行：读写、取整夹取与“恢复默认位置”。</summary>
public class OffsetSettingFieldTests
{
    [Fact]
    public void LoadFrom_ReflectsConfiguredOffset()
    {
        var field = CreateField();

        field.LoadFrom(new AppSettings { SearchBarOffset = new ScreenOffset(160, -90) });

        Assert.Equal(160m, field.X);
        Assert.Equal(-90m, field.Y);
        Assert.False(field.IsDefault);
    }

    [Fact]
    public void ApplyTo_WritesRoundedWholeNumbers()
    {
        var field = CreateField();
        field.X = 12.6m;
        field.Y = -7.4m;
        var settings = new AppSettings();

        field.ApplyTo(settings);

        Assert.Equal(new ScreenOffset(13, -7), settings.SearchBarOffset);
    }

    [Fact]
    public void ResetToDefault_ReturnsToCenteredPosition()
    {
        var field = CreateField();
        field.LoadFrom(new AppSettings { SearchBarOffset = new ScreenOffset(220, 140) });

        field.ResetToDefault();

        Assert.Equal(0m, field.X);
        Assert.Equal(0m, field.Y);
        Assert.True(field.IsDefault);

        var settings = new AppSettings();
        field.ApplyTo(settings);
        Assert.Equal(ScreenOffset.Default, settings.SearchBarOffset);
    }

    [Fact]
    public void OutOfRangeValues_AreClampedToAllowedRange()
    {
        var field = CreateField();

        field.X = 9000m;
        field.Y = -9000m;

        Assert.Equal((decimal)OffsetSettingFieldViewModel.MaxOffset, field.X);
        Assert.Equal((decimal)OffsetSettingFieldViewModel.MinOffset, field.Y);
    }

    [Fact]
    public void ClearedInput_KeepsPreviousValue()
    {
        var field = CreateField();
        field.X = 80m;

        field.X = null;

        Assert.Equal(80m, field.X);
        Assert.False(field.IsDefault);
    }

    private static OffsetSettingFieldViewModel CreateField()
    {
        var definition = AppSettingsSchema.Discover()
            .Single(item => item.PropertyName == nameof(AppSettings.SearchBarOffset));
        var texts = TranslationCatalog.Default.Resolve(AppLanguage.English).SettingsTexts;
        return new OffsetSettingFieldViewModel(definition, texts);
    }
}
