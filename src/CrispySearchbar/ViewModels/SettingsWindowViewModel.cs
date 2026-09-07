using System.Collections.ObjectModel;
using System.ComponentModel;
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
        IReadOnlyList<SettingFieldViewModel> fields)
    {
        Section = section;
        Title = title;
        Fields = fields;
    }

    public SettingsSection Section { get; }

    /// <summary>图标资源键后缀（SettingsIconTemplate-{Key}）。</summary>
    public string Key => Section.ToString().ToLowerInvariant();

    public string Title { get; }

    public IReadOnlyList<SettingFieldViewModel> Fields { get; }
}

/// <summary>
/// 设置窗口主视图模型：从磁盘设置构建、编辑并显式写回同一 settings.json。
/// 窗口只负责“修改配置文件”，不保存任何第二份设置状态。
/// </summary>
public sealed class SettingsWindowViewModel : INotifyPropertyChanged
{
    private AppSettings _settings;
    private AppSettingsTexts _texts;
    private readonly string _configFilePath;
    private string? _statusText;

    public SettingsWindowViewModel(
        AppSettings settings,
        AppSettingsTexts texts,
        string configFilePath)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(texts);

        _settings = settings;
        _texts = texts;
        _configFilePath = configFilePath;
        RebuildSections();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>保存成功并写盘后触发；App 据此即时应用。</summary>
    public event EventHandler<AppSettings>? Saved;

    public ObservableCollection<SettingsSectionViewModel> Sections { get; } = [];

    public string WindowTitle => _texts.WindowTitle;

    public string SaveText => _texts.Save;

    public string ConfigFilePathText => _texts.FormatConfigFilePath(_configFilePath);

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
    public void Reload(AppSettings settings, AppSettingsTexts texts)
    {
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(texts);

        _settings = settings;
        _texts = texts;
        RebuildSections();

        OnPropertyChanged(nameof(WindowTitle));
        OnPropertyChanged(nameof(SaveText));
        OnPropertyChanged(nameof(ConfigFilePathText));
    }

    /// <summary>校验并保存到配置文件；成功时通知 App 即时应用。</summary>
    public bool TrySave()
    {
        var hasFieldError = Sections
            .SelectMany(section => section.Fields)
            .Any(field => field.HasError);
        if (hasFieldError)
        {
            StatusText = _texts.ValidationFailedStatus;
            return false;
        }

        foreach (var field in Sections.SelectMany(section => section.Fields))
        {
            field.ApplyTo(_settings);
        }

        if (AppSettingsValidator.Validate(_settings).Count > 0)
        {
            StatusText = _texts.ValidationFailedStatus;
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

    private void RebuildSections()
    {
        Sections.Clear();
        var definitions = AppSettingsSchema.Discover();
        foreach (var section in Enum.GetValues<SettingsSection>())
        {
            var fields = new List<SettingFieldViewModel>();
            foreach (var definition in definitions.Where(item => item.Section == section))
            {
                var field = SettingFieldViewModelFactory.Create(definition, _texts);
                field.LoadFrom(_settings);
                fields.Add(field);
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

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
