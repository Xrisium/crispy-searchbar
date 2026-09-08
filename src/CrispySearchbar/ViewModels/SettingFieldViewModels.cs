using System.Collections.ObjectModel;
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

    /// <summary>供 SettingsWindowViewModel 在校验后把错误落到具体编辑行。</summary>
    internal void SetErrorFromValidation(string? message) => SetError(message);

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

/// <summary>模式设置列表中的一行；开关、上/下按钮与拖拽共用同一 owner 的集合操作。</summary>
public sealed class ModeListItemViewModel : INotifyPropertyChanged
{
    private readonly ModeListSettingFieldViewModel _owner;
    private bool _enabled;
    private bool _canMoveUp;
    private bool _canMoveDown;

    internal ModeListItemViewModel(
        ModeListSettingFieldViewModel owner,
        string key,
        string title,
        bool enabled)
    {
        _owner = owner;
        Key = key;
        Title = title;
        _enabled = enabled;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ModeListSettingFieldViewModel Owner => _owner;

    public string Key { get; }

    public string Title { get; }

    public bool IsEnabled
    {
        get => _enabled;
        set
        {
            if (_enabled == value)
            {
                return;
            }

            _enabled = value;
            OnPropertyChanged();
            _owner.OnEnabledStateChanged();
        }
    }

    public bool CanMoveUp
    {
        get => _canMoveUp;
        private set
        {
            if (_canMoveUp == value)
            {
                return;
            }

            _canMoveUp = value;
            OnPropertyChanged();
        }
    }

    public bool CanMoveDown
    {
        get => _canMoveDown;
        private set
        {
            if (_canMoveDown == value)
            {
                return;
            }

            _canMoveDown = value;
            OnPropertyChanged();
        }
    }

    public void MoveUp() => _owner.MoveBy(this, -1);

    public void MoveDown() => _owner.MoveBy(this, 1);

    public void MoveToTop() => _owner.MoveToBoundary(this, top: true);

    public void MoveToBottom() => _owner.MoveToBoundary(this, top: false);

    internal void ApplyMoveState(bool canMoveUp, bool canMoveDown)
    {
        CanMoveUp = canMoveUp;
        CanMoveDown = canMoveDown;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// ModeList 设置项：以一行可拖拽/可移动的模式列表编辑 <see cref="AppSettings.ModePreferences"/>。
/// 允许编辑时暂时全部关闭；0 个启用时显示内联错误并阻止保存，重新启用任意一个即清除。
/// </summary>
public sealed class ModeListSettingFieldViewModel : SettingFieldViewModel
{
    private readonly IReadOnlyDictionary<string, string> _modeTitles;

    public ModeListSettingFieldViewModel(
        SettingDefinition definition,
        AppSettingsTexts texts,
        IReadOnlyDictionary<string, string> modeTitles)
        : base(definition, texts)
    {
        _modeTitles = modeTitles;
    }

    public ObservableCollection<ModeListItemViewModel> Items { get; } = [];

    public string ModeMoveUpToolTip => Texts.ModeMoveUpToolTip;

    public string ModeMoveDownToolTip => Texts.ModeMoveDownToolTip;

    public string ModeDragHandleToolTip => Texts.ModeDragHandleToolTip;

    public override void LoadFrom(AppSettings settings)
    {
        var preferences = ModePreferenceNormalizer.Normalize(settings.ModePreferences);
        Items.Clear();
        foreach (var preference in preferences)
        {
            var title = _modeTitles.TryGetValue(preference.Key, out var localizedTitle)
                ? localizedTitle
                : preference.Key;
            Items.Add(new ModeListItemViewModel(
                this,
                preference.Key,
                title,
                preference.Enabled));
        }

        RefreshMoveState();
        SetError(null);
    }

    public override void ApplyTo(AppSettings settings)
        => settings.ModePreferences = Items
            .Select(item => new ModePreference(item.Key, item.IsEnabled))
            .ToArray();

    internal void OnEnabledStateChanged()
    {
        if (Items.Any(candidate => candidate.IsEnabled))
        {
            SetError(null);
            return;
        }

        SetError(Texts.ModeListAtLeastOneEnabledError);
    }

    internal void MoveBy(ModeListItemViewModel item, int offset)
    {
        var oldIndex = Items.IndexOf(item);
        var newIndex = oldIndex + offset;
        if (oldIndex < 0 || newIndex < 0 || newIndex >= Items.Count)
        {
            return;
        }

        Items.Move(oldIndex, newIndex);
        RefreshMoveState();
    }

    internal void DropAt(ModeListItemViewModel source, int insertionIndex)
    {
        var sourceIndex = Items.IndexOf(source);
        if (sourceIndex < 0)
        {
            return;
        }

        insertionIndex = Math.Clamp(insertionIndex, 0, Items.Count);
        var finalIndex = sourceIndex < insertionIndex
            ? insertionIndex - 1
            : insertionIndex;
        if (finalIndex < 0 || finalIndex >= Items.Count || finalIndex == sourceIndex)
        {
            return;
        }

        Items.Move(sourceIndex, finalIndex);
        RefreshMoveState();
    }

    internal void MoveToBoundary(ModeListItemViewModel item, bool top)
    {
        var oldIndex = Items.IndexOf(item);
        var targetIndex = top ? 0 : Items.Count - 1;
        if (oldIndex < 0 || oldIndex == targetIndex)
        {
            return;
        }

        Items.Move(oldIndex, targetIndex);
        RefreshMoveState();
    }

    private void RefreshMoveState()
    {
        for (var index = 0; index < Items.Count; index++)
        {
            Items[index].ApplyMoveState(
                canMoveUp: index > 0,
                canMoveDown: index < Items.Count - 1);
        }
    }
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

/// <summary>快捷键设置行：展示当前键位，可进入捕获态由用户按下新组合键。</summary>
public sealed class ShortcutSettingFieldViewModel : SettingFieldViewModel
{
    private string _value = string.Empty;
    private bool _isRecording;

    public ShortcutSettingFieldViewModel(
        SettingDefinition definition,
        AppSettingsTexts texts)
        : base(definition, texts)
    {
        Action = ShortcutDefaults.TryGetAction(definition.PropertyName)
            ?? throw new ArgumentException(
                $"未知快捷键属性：{definition.PropertyName}",
                nameof(definition));
    }

    public ShortcutAction Action { get; }

    public string Value
    {
        get => _value;
        private set
        {
            if (string.Equals(_value, value, StringComparison.Ordinal))
            {
                return;
            }

            _value = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayValue));
            OnPropertyChanged(nameof(IsDefault));
            OnPropertyChanged(nameof(CaptureLabel));
            SetError(null);
        }
    }

    public string DisplayValue
        => ShortcutParser.TryParse(Value, out var binding)
            ? binding.ToDisplayString()
            : Value;

    public bool IsDefault => string.Equals(
        Value,
        ShortcutDefaults.GetDefaultValue(Action),
        StringComparison.OrdinalIgnoreCase);

    public bool IsRecording
    {
        get => _isRecording;
        private set
        {
            if (_isRecording == value)
            {
                return;
            }

            _isRecording = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CaptureLabel));
        }
    }

    /// <summary>捕获中显示提示，否则显示当前键位。</summary>
    public string CaptureLabel => IsRecording ? Texts.ShortcutRecordPrompt : DisplayValue;

    public string ResetText => Texts.ShortcutResetText;

    public void BeginRecording()
    {
        SetError(null);
        IsRecording = true;
    }

    public void CancelRecording()
    {
        SetError(null);
        IsRecording = false;
    }

    public bool TrySetCaptured(string canonical)
    {
        var errorKey = ShortcutValidation.ValidateCandidate(Action, canonical);
        if (errorKey is not null)
        {
            SetError(Texts.GetValidationMessage(errorKey));
            return false;
        }

        Value = canonical;
        IsRecording = false;
        return true;
    }

    public void ResetToDefault()
    {
        IsRecording = false;
        Value = ShortcutDefaults.GetDefaultValue(Action);
    }

    public override void LoadFrom(AppSettings settings)
        => Value = Definition.GetValue(settings) as string ?? string.Empty;

    public override void ApplyTo(AppSettings settings)
        => Definition.SetValue(settings, Value);
}

public static class SettingFieldViewModelFactory
{
    public static SettingFieldViewModel Create(
        SettingDefinition definition,
        AppSettingsTexts texts,
        IReadOnlyDictionary<string, string>? modeTitles = null)
        => definition.EditorKind switch
        {
            SettingEditorKind.Choice => new ChoiceSettingFieldViewModel(definition, texts),
            SettingEditorKind.Toggle => new ToggleSettingFieldViewModel(definition, texts),
            SettingEditorKind.Text => new TextSettingFieldViewModel(definition, texts),
            SettingEditorKind.FilePath => new FilePathSettingFieldViewModel(definition, texts),
            SettingEditorKind.ShortcutKey => new ShortcutSettingFieldViewModel(
                definition,
                texts),
            SettingEditorKind.ModeList => new ModeListSettingFieldViewModel(
                definition,
                texts,
                modeTitles
                    ?? throw new ArgumentException(
                        "ModeList 设置项需要模式标题字典。",
                        nameof(modeTitles))),
            _ => throw new ArgumentOutOfRangeException(
                nameof(definition),
                definition.EditorKind,
                "未知的设置控件类型。"),
        };
}
