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

    /// <summary>本分类内不绑定 AppSettings 的静态展示行（当前仅“关于”使用）。</summary>
    public IReadOnlyList<object> StaticItems { get; }

    /// <summary>右侧页面实际渲染的行：先设置编辑行，后接静态行。</summary>
    public IReadOnlyList<object> Items { get; }
}

/// <summary>
/// 设置窗口主视图模型：从磁盘设置构建、编辑并显式写回同一 settings.json。
/// 窗口只负责“修改配置文件”，不保存任何第二份设置状态。
/// </summary>
public sealed class SettingsWindowViewModel : INotifyPropertyChanged
{
    private const string GitHubRepositoryUrl = "https://github.com/Xrisium/crispy-searchbar";

    private const string ThirdPartyNoticesFileName = "THIRD_PARTY_NOTICES.md";

    private const string FallbackAppVersion = "0.1.0";

    private AppSettings _settings;
    private AppSettingsTexts _texts;
    private AppStrings _strings;
    private readonly string _configFilePath;
    private string? _statusText;
    private bool _isResetConfirmationVisible;

    public SettingsWindowViewModel(
        AppSettings settings,
        AppStrings strings,
        string configFilePath)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(strings);

        _settings = settings;
        _strings = strings;
        _texts = strings.SettingsTexts;
        _configFilePath = configFilePath;
        RebuildSections();
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
        RebuildSections();

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

    /// <summary>显示“重置配置文件”确认层。</summary>
    public void ShowResetConfirmation()
        => IsResetConfirmationVisible = true;

    /// <summary>关闭“重置配置文件”确认层且不修改任何配置。</summary>
    public void HideResetConfirmation()
        => IsResetConfirmationVisible = false;

    /// <summary>把配置文件恢复为默认值并立即应用；成功时沿用 Saved 事件链路让 App 刷新。</summary>
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

        // App 处理事件时会同步重读文件并触发 Reload，随后这里再用新语言显示完成状态。
        Saved?.Invoke(this, defaults);
        StatusText = _texts.ResetDoneStatus;
        HideResetConfirmation();
        return true;
    }

    /// <summary>校验并保存到配置文件；成功时通知 App 即时应用。</summary>
    public bool TrySave()
    {
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

        var validatorErrorField = FindFirstValidatorErrorField(_settings);
        if (validatorErrorField is not null)
        {
            ReportValidationFailure(
                validatorErrorField.Value.Section,
                validatorErrorField.Value.Field);
            return false;
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

        // App 处理事件时会同步重读文件并触发 Reload，随后这里再用新语言显示成功状态。
        Saved?.Invoke(this, _settings);
        StatusText = _texts.SavedStatus;
        return true;
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

    private (SettingsSectionViewModel Section, SettingFieldViewModel Field)? FindFirstValidatorErrorField(
        AppSettings settings)
    {
        var errorProperties = AppSettingsValidator
            .Validate(settings)
            .Select(error => error.PropertyName)
            .ToHashSet(StringComparer.Ordinal);
        if (errorProperties.Count == 0)
        {
            return null;
        }

        foreach (var section in Sections)
        {
            foreach (var field in section.Fields)
            {
                if (errorProperties.Contains(field.Definition.PropertyName))
                {
                    return (section, field);
                }
            }
        }

        return null;
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
                Path.Combine(AppContext.BaseDirectory, ThirdPartyNoticesFileName),
                openAsFile: true),
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
