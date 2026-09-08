using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using CrispySearchbar.Core.Configuration;
using CrispySearchbar.Core.Localization;

namespace CrispySearchbar.ViewModels;

/// <summary>设置窗口左侧导航与右侧长页中的一个分类区块。</summary>
public sealed class SettingsSectionViewModel
{
    public SettingsSectionViewModel(
        SettingsSection section,
        string title,
        IReadOnlyList<SettingFieldViewModel> fields,
        IReadOnlyList<object>? staticItems = null)
    {
        Section = section;
        Title = title;
        Fields = fields;
        StaticItems = staticItems ?? Array.Empty<object>();

        var items = new List<object>(Fields.Count + StaticItems.Count);
        items.AddRange(Fields);
        items.AddRange(StaticItems);
        Items = items;
    }

    public SettingsSection Section { get; }

    /// <summary>图标资源键后缀（SettingsIconTemplate-{Key}）。</summary>
    public string Key => Section.ToString().ToLowerInvariant();

    public string Title { get; }

    public IReadOnlyList<SettingFieldViewModel> Fields { get; }

    /// <summary>本分类内不绑定 AppSettings 的静态展示行。</summary>
    public IReadOnlyList<object> StaticItems { get; }

    /// <summary>右侧页面实际渲染的行：先设置编辑行，后接静态行。</summary>
    public IReadOnlyList<object> Items { get; }
}

/// <summary>
/// 设置窗口主视图模型：从磁盘设置构建、编辑并显式写回同一 settings.json。
/// 快捷键校验以设置面板当前各行的值为准；硬错误阻止保存，提醒不阻止。
/// </summary>
public sealed class SettingsWindowViewModel : INotifyPropertyChanged
{
    private const string GitHubRepositoryUrl = "https://github.com/Xrisium/crispy-searchbar";

    private const string FallbackAppVersion = "0.1.0";

    private AppSettings _settings;
    private AppSettingsTexts _texts;
    private AppStrings _strings;
    private readonly string _configFilePath;
    private readonly Func<string, bool>? _globalShortcutProbe;
    private readonly Func<string, bool>? _isCurrentGlobalShortcut;
    private readonly Dictionary<string, bool> _globalConflictByProperty = new(StringComparer.Ordinal);
    private int _globalProbeVersion;
    private string? _statusText;
    private bool _isResetConfirmationVisible;

    public SettingsWindowViewModel(
        AppSettings settings,
        AppStrings strings,
        string configFilePath,
        Func<string, bool>? globalShortcutProbe = null,
        Func<string, bool>? isCurrentGlobalShortcut = null)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(strings);

        _settings = settings;
        _strings = strings;
        _texts = strings.SettingsTexts;
        _configFilePath = configFilePath;
        _globalShortcutProbe = globalShortcutProbe;
        _isCurrentGlobalShortcut = isCurrentGlobalShortcut;
        RebuildSections();
        RevalidateShortcuts();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>保存成功并写盘后触发；App 据此即时应用。</summary>
    public event EventHandler<AppSettings>? Saved;

    /// <summary>保存校验失败且已定位到具体字段后触发；窗口据此滚动到该行。</summary>
    public event EventHandler<SettingFieldViewModel>? ValidationFailed;

    public ObservableCollection<SettingsSectionViewModel> Sections { get; } = [];

    public string WindowTitle => _texts.WindowTitle;

    public string SaveText => _texts.Save;

    public string ConfigFileLabel => _texts.ConfigFileLabel;

    public string ConfigFilePath => _configFilePath;

    public string ConfigFileOpenText => _texts.OpenConfigFile;

    public bool IsResetConfirmationVisible
    {
        get => _isResetConfirmationVisible;
        private set
        {
            if (_isResetConfirmationVisible == value)
            {
                return;
            }

            _isResetConfirmationVisible = value;
            OnPropertyChanged();
        }
    }

    public string ResetConfirmationTitle => _texts.ResetConfirmTitle;

    public string ResetConfirmationMessage => _texts.ResetConfirmMessage;

    public string ResetConfirmationAcceptText => _texts.ResetConfirmAcceptText;

    public string ResetConfirmationCancelText => _texts.ResetConfirmCancelText;

    public string? StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText == value)
            {
                return;
            }

            _statusText = value;
            OnPropertyChanged();
        }
    }

    /// <summary>语言等配置保存后由 App 用磁盘上的新文件重建编辑区。</summary>
    public void Reload(AppSettings settings, AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(strings);

        _settings = settings;
        _strings = strings;
        _texts = strings.SettingsTexts;
        _isResetConfirmationVisible = false;
        _globalProbeVersion++;
        _globalConflictByProperty.Clear();
        RebuildSections();
        RevalidateShortcuts();

        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(SaveText));
        OnPropertyChanged(nameof(ConfigFileLabel));
        OnPropertyChanged(nameof(ConfigFilePath));
        OnPropertyChanged(nameof(ConfigFileOpenText));
        OnPropertyChanged(nameof(IsResetConfirmationVisible));
        OnPropertyChanged(nameof(ResetConfirmationTitle));
        OnPropertyChanged(nameof(ResetConfirmationMessage));
        OnPropertyChanged(nameof(ResetConfirmationAcceptText));
        OnPropertyChanged(nameof(ResetConfirmationCancelText));
    }

    public void ShowResetConfirmation()
        => IsResetConfirmationVisible = true;

    public void HideResetConfirmation()
        => IsResetConfirmationVisible = false;

    /// <summary>把配置文件恢复为默认值并立即应用。</summary>
    public bool TryResetConfiguration()
    {
        AppSettings defaults;
        try
        {
            defaults = new AppSettings();
            AppSettingsStore.Save(defaults);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText = _texts.FormatSaveFailed(ex.Message);
            HideResetConfirmation();
            return false;
        }

        Saved?.Invoke(this, defaults);
        StatusText = _texts.ResetDoneStatus;
        HideResetConfirmation();
        return true;
    }

    /// <summary>校验并保存到配置文件；硬错误阻止保存，黄色提醒不阻止。</summary>
    public bool TrySave()
    {
        // 先按当前面板各行的值重算快捷键红/黄状态，避免残留旧错误抢占位置。
        RevalidateShortcuts();

        var invalidField = FindFirstFieldWithError();
        if (invalidField is not null)
        {
            ReportValidationFailure(invalidField.Value.Section, invalidField.Value.Field);
            return false;
        }

        foreach (var field in Sections.SelectMany(section => section.Fields))
        {
            field.ApplyTo(_settings);
        }

        try
        {
            AppSettingsStore.Save(_settings);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            StatusText = _texts.FormatSaveFailed(ex.Message);
            return false;
        }

        Saved?.Invoke(this, _settings);
        StatusText = _texts.SavedStatus;
        return true;
    }

    /// <summary>把快捷键区所有字段与 Esc 复选框重置为默认，只改当前面板不写盘。</summary>
    public void ResetAllShortcuts()
    {
        foreach (var field in CollectShortcutFields())
        {
            field.ResetToDefault();
        }

        var hideField = CollectFieldsByPropertyName(nameof(AppSettings.HideOnEscape))
            .OfType<ToggleSettingFieldViewModel>()
            .FirstOrDefault();
        if (hideField is not null)
        {
            hideField.IsChecked = true;
        }

        RevalidateShortcuts();
    }

    /// <summary>捕获/重置/加载后调用：重算重复与提醒，并异步探测全局占用。</summary>
    public void RevalidateShortcuts()
    {
        ApplyShortcutFeedback();
        RunGlobalConflictProbe();
    }

    private (SettingsSectionViewModel Section, SettingFieldViewModel Field)? FindFirstFieldWithError()
    {
        foreach (var section in Sections)
        {
            foreach (var field in section.Fields)
            {
                if (field.HasError)
                {
                    return (section, field);
                }
            }
        }

        return null;
    }

    private IReadOnlyList<ShortcutSettingFieldViewModel> CollectShortcutFields()
        => Sections
            .SelectMany(section => section.Fields)
            .OfType<ShortcutSettingFieldViewModel>()
            .ToArray();

    private IReadOnlyList<SettingFieldViewModel> CollectFieldsByPropertyName(string propertyName)
        => Sections
            .SelectMany(section => section.Fields)
            .Where(field => string.Equals(
                field.Definition.PropertyName,
                propertyName,
                StringComparison.Ordinal))
            .ToArray();

    private void ApplyShortcutFeedback()
    {
        var fields = CollectShortcutFields();
        foreach (var field in fields)
        {
            field.SetErrorFromValidation(null);
            field.SetWarningFromValidation(null);
        }

        var issues = ShortcutValidation.ValidatePanel(fields.Select(field => (
            field.Definition.PropertyName,
            field.Value)));
        foreach (var issue in issues)
        {
            var field = fields.FirstOrDefault(candidate => string.Equals(
                candidate.Definition.PropertyName,
                issue.PropertyName,
                StringComparison.Ordinal));
            if (field is null)
            {
                continue;
            }

            var message = _texts.GetValidationMessage(issue.ErrorKey);
            if (issue.Severity == ShortcutSeverity.Error)
            {
                field.SetErrorFromValidation(message);
            }
            else
            {
                field.SetWarningFromValidation(message);
            }
        }

        foreach (var pair in _globalConflictByProperty)
        {
            if (!pair.Value)
            {
                continue;
            }

            var field = fields.FirstOrDefault(candidate => string.Equals(
                candidate.Definition.PropertyName,
                pair.Key,
                StringComparison.Ordinal));
            field?.SetWarningFromValidation(_texts.ShortcutGlobalConflictWarning);
        }
    }

    private async void RunGlobalConflictProbe()
    {
        if (_globalShortcutProbe is null)
        {
            return;
        }

        var version = ++_globalProbeVersion;
        var toggleField = CollectShortcutFields().FirstOrDefault(field =>
            string.Equals(
                field.Definition.PropertyName,
                ShortcutDefaults.GetPropertyName(ShortcutAction.ToggleVisibility),
                StringComparison.Ordinal));
        var propertyName = ShortcutDefaults.GetPropertyName(ShortcutAction.ToggleVisibility);
        if (toggleField is null || toggleField.IsEmpty)
        {
            _globalConflictByProperty[propertyName] = false;
            ApplyShortcutFeedback();
            return;
        }

        var candidate = toggleField.Value;
        if (_isCurrentGlobalShortcut?.Invoke(candidate) == true)
        {
            _globalConflictByProperty[propertyName] = false;
            ApplyShortcutFeedback();
            return;
        }

        var succeeded = await Task.Run(() => TryProbe(candidate));
        if (version != _globalProbeVersion)
        {
            return;
        }

        _globalConflictByProperty[propertyName] = !succeeded;
        ApplyShortcutFeedback();
    }

    private bool TryProbe(string candidate)
    {
        try
        {
            return _globalShortcutProbe?.Invoke(candidate) == true;
        }
        catch
        {
            return false;
        }
    }

    private void ReportValidationFailure(
        SettingsSectionViewModel section,
        SettingFieldViewModel field)
    {
        StatusText = _texts.FormatValidationFailed(section.Title, field.Label);
        ValidationFailed?.Invoke(this, field);
    }

    private SettingFieldViewModel CreateSettingField(SettingDefinition definition)
    {
        if (definition.PropertyName != nameof(AppSettings.Language))
        {
            return SettingFieldViewModelFactory.Create(
                definition,
                _texts,
                BuildModeTitles());
        }

        var overrides = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [AppLanguage.System] = _texts.GetOptionLabel(
                nameof(AppSettings.Language),
                AppLanguage.System),
        };
        foreach (var language in TranslationCatalog.Default.Languages)
        {
            overrides[language.Language] = language.NativeName;
        }

        return new ChoiceSettingFieldViewModel(definition, _texts, overrides);
    }

    private IReadOnlyDictionary<string, string> BuildModeTitles()
        => new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ModePreferenceDefaults.WebSearch] = _strings.WebSearchMode.Title,
            [ModePreferenceDefaults.Wikipedia] = _strings.WikipediaMode.Title,
            [ModePreferenceDefaults.AskAi] = _strings.AskAiMode.Title,
            [ModePreferenceDefaults.Dictionary] = _strings.DictionaryMode.Title,
        };

    private void RebuildSections()
    {
        Sections.Clear();
        var definitions = AppSettingsSchema.Discover(TranslationCatalog.Default.UiLanguageOptionCodes);
        foreach (var section in Enum.GetValues<SettingsSection>())
        {
            var fields = new List<SettingFieldViewModel>();
            foreach (var definition in definitions.Where(item => item.Section == section))
            {
                var field = CreateSettingField(definition);
                field.LoadFrom(_settings);
                fields.Add(field);
            }

            if (section == SettingsSection.Shortcuts)
            {
                Sections.Add(new SettingsSectionViewModel(
                    section,
                    _texts.GetSectionTitle(section),
                    fields,
                    new List<object> { new ShortcutResetAllViewModel(_texts) }));
                continue;
            }

            if (section == SettingsSection.About)
            {
                Sections.Add(new SettingsSectionViewModel(
                    section,
                    _texts.GetSectionTitle(section),
                    fields,
                    BuildAboutItems()));
                continue;
            }

            if (fields.Count == 0)
            {
                continue;
            }

            Sections.Add(new SettingsSectionViewModel(
                section,
                _texts.GetSectionTitle(section),
                fields));
        }
    }

    private IReadOnlyList<object> BuildAboutItems()
        => new List<object>
        {
            new AboutParagraphViewModel(_texts.AboutIntro),
            new AboutVersionViewModel(_texts.FormatAboutVersion(GetAppVersion())),
            new AboutParagraphViewModel(_texts.AboutLicenseLine),
            new AboutLinkViewModel(
                _texts.AboutGitHubLinkLabel,
                GitHubRepositoryUrl,
                openAsFile: false),
            new AboutLinkViewModel(
                _texts.AboutThirdPartyNoticesLinkLabel,
                GitHubRepositoryUrl + "/blob/main/THIRD_PARTY_NOTICES.md",
                openAsFile: false),
            new AboutActionViewModel(_texts.ResetConfigText, _texts.ResetConfigDescription),
        };

    private static string GetAppVersion()
    {
        var informational = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;
        return string.IsNullOrWhiteSpace(informational)
            ? FallbackAppVersion
            : informational;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}