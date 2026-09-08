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
    [InlineData("Alt+-", ShortcutModifiers.Alt, "-")]
    [InlineData("Ctrl+,", ShortcutModifiers.Control, ",")]
    [InlineData("Ctrl+Alt+XButton1", ShortcutModifiers.Control | ShortcutModifiers.Alt, "XButton1")]
    [InlineData("shift+xbutton2", ShortcutModifiers.Shift, "XButton2")]
    [InlineData("`", ShortcutModifiers.None, "`")]
    public void Parse_NormalizesModifierOrderAndKeyNames(
        string text,
        ShortcutModifiers expectedModifiers,
        string expectedKey)
    {
        Assert.True(ShortcutParser.TryParse(text, out var binding));
        Assert.Equal(expectedModifiers, binding.Modifiers);
        Assert.Equal(expectedKey, binding.Key);
    }

    [Fact]
    public void Empty_IsAcceptedAsUnset()
    {
        Assert.True(ShortcutParser.IsEmpty(null));
        Assert.True(ShortcutParser.IsEmpty(string.Empty));
        Assert.True(ShortcutParser.IsEmpty("   "));
        Assert.False(ShortcutParser.IsEmpty("Alt+Space"));
    }

    [Theory]
    [InlineData("Alt")]
    [InlineData("Ctrl+Alt+Q+R")]
    [InlineData("Alt+Alt+Q")]
    [InlineData("UnknownKey")]
    [InlineData("Ctrl+Shift+Enter+Space")]
    public void Parse_RejectsInvalidShortcuts(string? text)
        => Assert.False(ShortcutParser.TryParse(text, out _));

    [Fact]
    public void Display_UsesArrowsForAllDirections()
    {
        Assert.Equal("↑", new ShortcutBinding(ShortcutModifiers.None, "Up").ToDisplayString());
        Assert.Equal("↓", new ShortcutBinding(ShortcutModifiers.None, "Down").ToDisplayString());
        Assert.Equal("←", new ShortcutBinding(ShortcutModifiers.None, "Left").ToDisplayString());
        Assert.Equal("→", new ShortcutBinding(ShortcutModifiers.None, "Right").ToDisplayString());
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
    public void PrintableKeyWithoutModifier_IsOnlyAWarning()
    {
        Assert.Equal(
            ShortcutSeverity.Warning,
            ShortcutValidation.ValidateCandidate(ShortcutAction.Execute, "Q")?.Severity);
        Assert.Equal(
            ShortcutSeverity.Warning,
            ShortcutValidation.ValidateCandidate(ShortcutAction.ToggleVisibility, "Space")?.Severity);
        Assert.Null(ShortcutValidation.ValidateCandidate(ShortcutAction.Execute, "Ctrl+J"));
    }

    [Fact]
    public void ModeSwitchKey_RequiresSingleNonTypingKey()
    {
        Assert.Null(ShortcutValidation.ValidateCandidate(ShortcutAction.CycleMode, "F8"));
        Assert.Null(ShortcutValidation.ValidateCandidate(ShortcutAction.CycleMode, "Tab"));
        Assert.Equal(
            ShortcutSeverity.Error,
            ShortcutValidation.ValidateCandidate(ShortcutAction.CycleMode, "Ctrl+Tab")?.Severity);
        Assert.Equal(
            ShortcutSeverity.Warning,
            ShortcutValidation.ValidateCandidate(ShortcutAction.CycleMode, "Space")?.Severity);
    }

    [Fact]
    public void TextEditingCombination_IsAHardError()
    {
        Assert.Equal(
            ShortcutSeverity.Error,
            ShortcutValidation.ValidateCandidate(ShortcutAction.Execute, "Ctrl+C")?.Severity);
        Assert.Null(ShortcutValidation.ValidateCandidate(ShortcutAction.Execute, "Ctrl+Q"));
    }

    [Fact]
    public void DuplicateShortcut_ReportsErrorOnLaterField()
    {
        var settings = new AppSettings
        {
            ToggleVisibilityShortcut = "Ctrl+Q",
            ExecuteShortcut = "Ctrl+Q",
        };

        var errors = ShortcutValidation.Validate(settings);

        var duplicate = Assert.Single(errors);
        Assert.Equal(nameof(AppSettings.ExecuteShortcut), duplicate.PropertyName);
        Assert.Equal(ShortcutValidation.DuplicateError, duplicate.ErrorKey);
    }

    [Fact]
    public void PanelValidation_UsesCurrentValuesAndDistinguishesSeverity()
    {
        var issues = ShortcutValidation.ValidatePanel(
        [
            (nameof(AppSettings.ToggleVisibilityShortcut), "Ctrl+Q"),
            (nameof(AppSettings.ExecuteShortcut), "Q"),
        ]);

        var warning = Assert.Single(issues, issue =>
            issue.PropertyName == nameof(AppSettings.ExecuteShortcut));
        Assert.Equal(ShortcutSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void EmptyValues_ProduceNoIssues()
    {
        var issues = ShortcutValidation.ValidatePanel(
        [
            (nameof(AppSettings.ToggleVisibilityShortcut), ""),
            (nameof(AppSettings.CycleModeShortcut), ""),
        ]);

        Assert.Empty(issues);
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
        Assert.Null(catalog.FindAction(new ShortcutBinding(ShortcutModifiers.None, "F12")));
    }

    [Fact]
    public void Create_FallsBackToDefaultForUnparseableField()
    {
        var settings = new AppSettings { CycleModeShortcut = "Not a shortcut" };
        var catalog = ShortcutCatalog.Create(settings);

        Assert.Equal(new ShortcutBinding(ShortcutModifiers.None, "Tab"), catalog[ShortcutAction.CycleMode]);
    }

    [Fact]
    public void EmptyBinding_IsOmittedFromCatalog()
    {
        var settings = new AppSettings
        {
            ToggleVisibilityShortcut = string.Empty,
            ExecuteShortcut = string.Empty,
        };
        var catalog = ShortcutCatalog.Create(settings);

        Assert.False(catalog.IsBound(ShortcutAction.ToggleVisibility));
        Assert.False(catalog.IsBound(ShortcutAction.Execute));
        Assert.True(catalog.IsBound(ShortcutAction.CycleMode));
    }
}

public class ShortcutTextFormatterTests
{
    [Fact]
    public void DefaultBindings_ProduceOriginalDefaultHints()
    {
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese);
        var formatted = ShortcutTextFormatter.Format(
            strings.AskAiMode,
            ShortcutCatalog.Default,
            strings.SettingsTexts.ShortcutUnsetText);

        Assert.Equal("输入问题，按 Enter 跳转到 DeepSeek 网页端", formatted.Watermark);
        Assert.Equal("按 Enter 跳转到 DeepSeek 网页端", formatted.ActionHint);

        var hint = ShortcutTextFormatter.Format(
            strings.DictionaryEmptyHint,
            ShortcutCatalog.Default,
            strings.SettingsTexts.ShortcutUnsetText);
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

        Assert.Equal(
            "按 Ctrl+J 查看释义",
            ShortcutTextFormatter.Format("按 {execute} 查看释义", shortcuts));
        Assert.Equal(
            "Alt+↑ / Alt+↓",
            ShortcutTextFormatter.Format("{previous} / {next}", shortcuts));
    }

    [Fact]
    public void EmptyBindings_UseUnsetText()
    {
        var shortcuts = ShortcutCatalog.Create(new AppSettings { ExecuteShortcut = string.Empty });
        Assert.Equal(
            "按 未设定快捷键 查看",
            ShortcutTextFormatter.Format("按 {execute} 查看", shortcuts, "未设定快捷键"));
    }
}