using System.ComponentModel;
using System.Runtime.CompilerServices;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;

namespace CrispySearchbar.ViewModels;

/// <summary>设置编辑行的抽象视图模型；子类按 SettingEditorKind 对应一种控件。</summary>
public abstract class SettingFieldViewModel : INotifyPropertyChanged
{
    private string? _error;

    protected SettingFieldViewModel(SettingDefinition definition, AppSettingsTexts texts)
    {
        Definition = definition;
        Texts = texts;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public SettingDefinition Definition { get; }

    protected AppSettingsTexts Texts { get; }

    public string Label => Texts.GetFieldLabel(Definition.PropertyName);

    public string? Description => Texts.GetFieldDescription(Definition.PropertyName);

    public bool HasDescription => !string.IsNullOrWhiteSpace(Description);

    public string? Error
    {
        get => _error;
        private set
        {
            if (_error == value)
            {
                return;
            }

            _error = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrWhiteSpace(Error);

    public abstract void LoadFrom(AppSettings settings);

    public abstract void ApplyTo(AppSettings settings);

    protected void SetError(string? error) => Error = error;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public sealed class ChoiceOptionViewModel
{
    public ChoiceOptionViewModel(object value, string displayName)
    {
        Value = value;
        DisplayName = displayName;
    }

    public object Value { get; }

    public string DisplayName { get; }
}

public sealed class ChoiceSettingFieldViewModel : SettingFieldViewModel
{
    private ChoiceOptionViewModel? _selectedOption;

    public ChoiceSettingFieldViewModel(
        SettingDefinition definition,
        AppSettingsTexts texts,
        IReadOnlyDictionary<string, string>? optionLabelOverrides = null)
        : base(definition, texts)
    {
        Options = definition.OptionValues
            .Where(value => value is not null)
            .Select(value => new ChoiceOptionViewModel(
                value!,
                ResolveOptionLabel(definition, texts, value!, optionLabelOverrides)))
            .ToArray();
    }

    private static string ResolveOptionLabel(
        SettingDefinition definition,
        AppSettingsTexts texts,
        object value,
        IReadOnlyDictionary<string, string>? overrides)
    {
        if (overrides is not null
            && value.ToString() is { } key
            && overrides.TryGetValue(key, out var label))
        {
            return label;
        }

        return texts.GetOptionLabel(definition.PropertyName, value);
    }

    public IReadOnlyList<ChoiceOptionViewModel> Options { get; }

    public ChoiceOptionViewModel? SelectedOption
    {
        get => _selectedOption;
        set
        {
            if (ReferenceEquals(_selectedOption, value))
            {
                return;
            }

            _selectedOption = value;
            OnPropertyChanged();
        }
    }

    public override void LoadFrom(AppSettings settings)
    {
        var current = Definition.GetValue(settings);
        SelectedOption = Options.FirstOrDefault(option => Equals(option.Value, current));
    }

    public override void ApplyTo(AppSettings settings)
    {
        if (SelectedOption is not null)
        {
            Definition.SetValue(settings, SelectedOption.Value);
        }
    }
}

public sealed class ToggleSettingFieldViewModel : SettingFieldViewModel
{
    private bool _isChecked;

    public ToggleSettingFieldViewModel(SettingDefinition definition, AppSettingsTexts texts)
        : base(definition, texts)
    {
    }

    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value)
            {
                return;
            }

            _isChecked = value;
            OnPropertyChanged();
        }
    }

    public override void LoadFrom(AppSettings settings)
        => IsChecked = Definition.GetValue(settings) is true;

    public override void ApplyTo(AppSettings settings)
        => Definition.SetValue(settings, IsChecked);
}

public sealed class TextSettingFieldViewModel : SettingFieldViewModel
{
    private string _text = string.Empty;

    public TextSettingFieldViewModel(SettingDefinition definition, AppSettingsTexts texts)
        : base(definition, texts)
    {
    }

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value ?? string.Empty;
            OnPropertyChanged();
            SetError(Validate(_text));
        }
    }

    public override void LoadFrom(AppSettings settings)
        => Text = Definition.GetValue(settings) as string ?? string.Empty;

    public override void ApplyTo(AppSettings settings)
        => Definition.SetValue(settings, Text);

    private string? Validate(string? value)
    {
        if (Definition.Validation != SettingValidation.UrlTemplate)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(value))
        {
            return Texts.UrlTemplateRequiredError;
        }

        return value.Contains("{0}", StringComparison.Ordinal)
            ? null
            : Texts.UrlTemplatePlaceholderError;
    }
}

public sealed class FilePathSettingFieldViewModel : SettingFieldViewModel
{
    private string? _filePath;

    public FilePathSettingFieldViewModel(SettingDefinition definition, AppSettingsTexts texts)
        : base(definition, texts)
    {
    }

    public string? FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath == value)
            {
                return;
            }

            _filePath = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasFilePath));
        }
    }

    public bool HasFilePath => !string.IsNullOrWhiteSpace(FilePath);

    public string BrowseText => Texts.Browse;

    public string ClearText => Texts.Clear;

    public bool HasFileTypeFilter => Definition.FileTypePatterns.Count > 0;

    public string? FileTypeFilterName
        => string.IsNullOrWhiteSpace(Definition.FileTypeFilterKey)
            ? null
            : Texts.GetFileTypeFilterName(Definition.FileTypeFilterKey);

    public IReadOnlyList<string> FileTypePatterns => Definition.FileTypePatterns;

    public void ClearPath() => FilePath = null;

    public override void LoadFrom(AppSettings settings)
        => FilePath = Definition.GetValue(settings) as string;

    public override void ApplyTo(AppSettings settings)
        => Definition.SetValue(
            settings,
            string.IsNullOrWhiteSpace(FilePath) ? null : FilePath);
}

public static class SettingFieldViewModelFactory
{
    public static SettingFieldViewModel Create(
        SettingDefinition definition,
        AppSettingsTexts texts)
        => definition.EditorKind switch
        {
            SettingEditorKind.Choice => new ChoiceSettingFieldViewModel(definition, texts),
            SettingEditorKind.Toggle => new ToggleSettingFieldViewModel(definition, texts),
            SettingEditorKind.Text => new TextSettingFieldViewModel(definition, texts),
            SettingEditorKind.FilePath => new FilePathSettingFieldViewModel(definition, texts),
            _ => throw new ArgumentOutOfRangeException(
                nameof(definition),
                definition.EditorKind,
                "未知的设置控件类型。"),
        };
}
