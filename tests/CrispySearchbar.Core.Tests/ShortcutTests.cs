using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;
using Xunit;

namespace CrispySearchbar.Core.Tests;

public class ShortcutParserTests
{
    [Theory]
    [InlineData("Alt+Space", ShortcutModifiers.Alt, "Space")]
    [InlineData("alt+space", ShortcutModifiers.Alt, "Space")]
    [InlineData("Ctrl+Shift+F8", ShortcutModifiers.Control | ShortcutModifiers.Shift, "F8")]
    [InlineData("Esc", ShortcutModifiers.None, "Esc")]
    [InlineData("enter", ShortcutModifiers.None, "Enter")]
    [InlineData("Ctrl+Alt+Win+q", ShortcutModifiers.Control | ShortcutModifiers.Alt | ShortcutModifiers.Win, "Q")]
    public void Parse_NormalizesModifierOrderAndKeyNames(
        string text,
        ShortcutModifiers expectedModifiers,
        string expectedKey)
    {
        Assert.True(ShortcutParser.TryParse(text, out var binding));
        Assert.Equal(expectedModifiers, binding.Modifiers);
        Assert.Equal(expectedKey, binding.Key);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Alt")]
    [InlineData("Ctrl+Alt+Q+R")]
    [InlineData("Alt+Alt+Q")]
    [InlineData("UnknownKey")]
    [InlineData("Ctrl+Shift+Enter+Space")]
    public void Parse_RejectsInvalidShortcuts(string? text)
        => Assert.False(ShortcutParser.TryParse(text, out _));

    [Fact]
    public void Display_UsesArrowsForUpAndDown()
    {
        Assert.Equal("↑", new ShortcutBinding(ShortcutModifiers.None, "Up").ToDisplayString());
        Assert.Equal("Alt+↓", new ShortcutBinding(ShortcutModifiers.Alt, "Down").ToDisplayString());
        Assert.Equal("Alt+Space", new ShortcutBinding(ShortcutModifiers.Alt, "Space").ToDisplayString());
    }
}

public class ShortcutValidationTests
{
    [Fact]
    public void DefaultSettings_HaveNoValidationErrors()
        => Assert.Empty(AppSettingsValidator.Validate(new AppSettings()));

    [Fact]
    public void PrintableKeyWithoutModifier_IsRejectedForInAppActions()
    {
        Assert.Equal(
            ShortcutValidation.PrintableRequiresModifierError,
            ShortcutValidation.ValidateCandidate(ShortcutAction.Execute, "Q"));
        Assert.Equal(
            ShortcutValidation.PrintableRequiresModifierError,
            ShortcutValidation.ValidateCandidate(ShortcutAction.ToggleVisibility, "Space"));
        Assert.Null(ShortcutValidation.ValidateCandidate(ShortcutAction.Execute, "Ctrl+J"));
    }

    [Fact]
    public void ModeSwitchKey_RequiresSingleNonTypingKey()
    {
        Assert.Null(ShortcutValidation.ValidateCandidate(ShortcutAction.CycleMode, "F8"));
        Assert.Null(ShortcutValidation.ValidateCandidate(ShortcutAction.CycleMode, "Tab"));
        Assert.Equal(
            ShortcutValidation.ModeSwitchRequiresSingleKeyError,
            ShortcutValidation.ValidateCandidate(ShortcutAction.CycleMode, "Ctrl+Tab"));
        Assert.Equal(
            ShortcutValidation.PrintableRequiresModifierError,
            ShortcutValidation.ValidateCandidate(ShortcutAction.CycleMode, "Space"));
    }

    [Fact]
    public void TextEditingCombination_IsRejectedForInAppActions()
    {
        Assert.Equal(
            ShortcutValidation.ReservedForTextEditingError,
            ShortcutValidation.ValidateCandidate(ShortcutAction.Hide, "Ctrl+C"));
        Assert.Null(ShortcutValidation.ValidateCandidate(ShortcutAction.Hide, "Ctrl+Q"));
    }

    [Fact]
    public void DuplicateShortcut_ReportsErrorOnLaterField()
    {
        var settings = new AppSettings
        {
            HideShortcut = "Ctrl+Q",
            ExecuteShortcut = "Ctrl+Q",
        };

        var errors = ShortcutValidation.Validate(settings);

        var duplicate = Assert.Single(errors);
        Assert.Equal(nameof(AppSettings.ExecuteShortcut), duplicate.PropertyName);
        Assert.Equal(ShortcutValidation.DuplicateError, duplicate.ErrorKey);
    }

    [Fact]
    public void FunctionKeyAlone_IsAllowedForGlobalHotkey()
    {
        Assert.Null(ShortcutValidation.ValidateCandidate(ShortcutAction.ToggleVisibility, "F9"));
    }
}

public class ShortcutDefaultsTests
{
    [Fact]
    public void PropertyNames_MatchAppSettingsProperties()
    {
        foreach (var action in Enum.GetValues<ShortcutAction>())
        {
            var propertyName = ShortcutDefaults.GetPropertyName(action);
            var property = typeof(AppSettings).GetProperty(propertyName);
            Assert.NotNull(property);
            Assert.Equal(typeof(string), property!.PropertyType);
            Assert.True(property.CanWrite);
        }
    }
}

public class ShortcutCatalogTests
{
    [Fact]
    public void Defaults_KeepDocumentedBindings()
    {
        var catalog = ShortcutCatalog.Default;

        Assert.Equal(new ShortcutBinding(ShortcutModifiers.Alt, "Space"), catalog[ShortcutAction.ToggleVisibility]);
        Assert.Equal(new ShortcutBinding(ShortcutModifiers.None, "Tab"), catalog[ShortcutAction.CycleMode]);
        Assert.Equal(new ShortcutBinding(ShortcutModifiers.None, "Esc"), catalog[ShortcutAction.Hide]);
        Assert.Equal(new ShortcutBinding(ShortcutModifiers.None, "Enter"), catalog[ShortcutAction.Execute]);
        Assert.Equal(new ShortcutBinding(ShortcutModifiers.None, "Up"), catalog[ShortcutAction.SelectPrevious]);
        Assert.Equal(new ShortcutBinding(ShortcutModifiers.None, "Down"), catalog[ShortcutAction.SelectNext]);
    }

    [Fact]
    public void FindAction_MatchesPressedBinding()
    {
        var catalog = ShortcutCatalog.Create(new AppSettings
        {
            ExecuteShortcut = "Ctrl+J",
            SelectPreviousShortcut = "Up",
            SelectNextShortcut = "Down",
        });

        Assert.Equal(
            ShortcutAction.Execute,
            catalog.FindAction(new ShortcutBinding(ShortcutModifiers.Control, "J")));
        Assert.Equal(
            ShortcutAction.SelectPrevious,
            catalog.FindAction(new ShortcutBinding(ShortcutModifiers.None, "Up")));
        Assert.Equal(
            ShortcutAction.SelectNext,
            catalog.FindAction(new ShortcutBinding(ShortcutModifiers.None, "Down")));
        Assert.Null(catalog.FindAction(new ShortcutBinding(ShortcutModifiers.None, "F12")));
    }

    [Fact]
    public void Create_FallsBackToDefaultForUnparseableField()
    {
        var settings = new AppSettings { HideShortcut = "Not a shortcut" };
        var catalog = ShortcutCatalog.Create(settings);

        Assert.Equal(new ShortcutBinding(ShortcutModifiers.None, "Esc"), catalog[ShortcutAction.Hide]);
    }
}

public class ShortcutTextFormatterTests
{
    [Fact]
    public void DefaultBindings_ProduceOriginalDefaultHints()
    {
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese);
        var formatted = ShortcutTextFormatter.Format(strings.AskAiMode, ShortcutCatalog.Default);

        Assert.Equal("输入问题，按 Enter 跳转到 DeepSeek 网页端", formatted.Watermark);
        Assert.Equal("按 Enter 跳转到 DeepSeek 网页端", formatted.ActionHint);

        var hint = ShortcutTextFormatter.Format(
            strings.DictionaryEmptyHint,
            ShortcutCatalog.Default);
        Assert.Contains("↑/↓ 选择", hint);
        Assert.Contains("Enter 查看释义", hint);
    }

    [Fact]
    public void CustomBindings_UpdateVisibleHints()
    {
        var shortcuts = ShortcutCatalog.Create(new AppSettings
        {
            ExecuteShortcut = "Ctrl+J",
            SelectPreviousShortcut = "Alt+Up",
            SelectNextShortcut = "Alt+Down",
        });

        Assert.Equal("按 Ctrl+J 查看释义", ShortcutTextFormatter.Format("按 {execute} 查看释义", shortcuts));
        Assert.Equal(
            "Alt+↑ / Alt+↓",
            ShortcutTextFormatter.Format("{previous} / {next}", shortcuts));
    }
}