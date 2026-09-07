using CrispySearchbar.Core.Configuration;

namespace CrispySearchbar.Core.Localization;

/// <summary>
/// 应用用户可见文案的强类型集合。
/// 每个受支持语言提供一个完整实例；新增语言时添加静态实例、在 AppLanguage.Supported 中登记，并在 For 中建立映射。
/// </summary>
public sealed class AppStrings
{
    public required string Language { get; init; }

    public required SearchModeTexts WebSearchMode { get; init; }

    public required SearchModeTexts AskAiMode { get; init; }

    public required SearchModeTexts DictionaryMode { get; init; }

    public required string TrayToolTip { get; init; }

    public required string ShowHideSearchBar { get; init; }

    public required string OpenSettings { get; init; }

    public required string Exit { get; init; }

    public required AppSettingsTexts SettingsTexts { get; init; }

    public required string DictionaryEmptyHint { get; init; }

    public required string DictionaryLoadingHint { get; init; }

    public required string DictionaryDataSourceNotConfigured { get; init; }

    public required string DictionaryNoResultsTemplate { get; init; }

    public required string DictionaryLoadFailedTemplate { get; init; }

    public required string DictionaryReadFailedTemplate { get; init; }

    public required string ConfiguredDictionaryFileMissingTemplate { get; init; }

    public required string DictionaryDataFileNotFoundTemplate { get; init; }

    public required string SearchEngineBaiduName { get; init; }

    public required string SearchEngineBingName { get; init; }

    public static AppStrings SimplifiedChinese { get; } = new()
    {
        Language = AppLanguage.SimplifiedChinese,
        WebSearchMode = new SearchModeTexts(
            "网页搜索",
            "输入关键词，按 Enter 搜索",
            "按 Enter 使用默认搜索引擎打开"),
        AskAiMode = new SearchModeTexts(
            "问问大肥鱼",
            "输入问题，按 Enter 跳转到 DeepSeek 网页端",
            "按 Enter 跳转到 DeepSeek 网页端"),
        DictionaryMode = new SearchModeTexts(
            "词典",
            "输入英文单词或中文词语",
            "输入后实时查词，Enter 查看释义"),
        TrayToolTip = "Crispy Searchbar（酥脆搜索）",
        ShowHideSearchBar = "显示 / 隐藏搜索框",
        OpenSettings = "设置",
        Exit = "退出",
        SettingsTexts = new AppSettingsTexts
        {
            WindowTitle = "设置",
            Save = "保存",
            Browse = "浏览…",
            Clear = "清空",
            SavedStatus = "已保存到 settings.json",
            ValidationFailedStatus = "请先修正带错误提示的设置项。",
            ConfigFileLabel = "配置文件：",
            OpenConfigFile = "打开配置文件",
            SaveFailedTemplate = "保存失败：{0}",
            UrlTemplateRequiredError = "请输入网址模板。",
            UrlTemplatePlaceholderError = "网址模板必须包含 {0} 占位符。",
            SectionTitles = new Dictionary<string, string>
            {
                [nameof(SettingsSection.General)] = "通用",
                [nameof(SettingsSection.Appearance)] = "外观",
                [nameof(SettingsSection.Search)] = "搜索",
                [nameof(SettingsSection.Dictionary)] = "词典",
            },
            FieldLabels = new Dictionary<string, string>
            {
                [nameof(AppSettings.Language)] = "界面语言",
                [nameof(AppSettings.Theme)] = "主题",
                [nameof(AppSettings.SearchEngine)] = "默认搜索引擎",
                [nameof(AppSettings.ClearQueryOnHide)] = "隐藏搜索框时清空输入",
                [nameof(AppSettings.AskAiUrlTemplate)] = "问问大肥鱼网址模板",
                [nameof(AppSettings.DictionaryFilePath)] = "汉英词典文件",
                [nameof(AppSettings.EcdictFilePath)] = "英汉词典文件",
            },
            FieldDescriptions = new Dictionary<string, string>
            {
                [nameof(AppSettings.Language)] = "保存后立即切换界面语言。",
                [nameof(AppSettings.Theme)] = "设置搜索框与设置窗口的外观。",
                [nameof(AppSettings.SearchEngine)] = "网页搜索模式使用的搜索引擎。",
                [nameof(AppSettings.ClearQueryOnHide)] = "搜索框隐藏后清空已输入内容。",
                [nameof(AppSettings.AskAiUrlTemplate)] = "需要包含 {0}，查询词会替换该占位符。",
                [nameof(AppSettings.DictionaryFilePath)] =
                    "支持 CC-CEDICT 的 UTF-8 文本文件（.u8，如 cedict_ts.u8）。留空时使用用户数据目录或程序内置文件。",
                [nameof(AppSettings.EcdictFilePath)] =
                    "支持 ECDICT 的 CSV 文件（.csv，如 ecdict.csv）。留空时使用用户数据目录或程序内置文件。",
            },
            OptionLabels = new Dictionary<string, string>
            {
                ["Language.zh-Hans"] = "简体中文",
                ["Language.en"] = "English",
                ["Theme.System"] = "跟随系统",
                ["Theme.Light"] = "浅色",
                ["Theme.Dark"] = "深色",
                ["SearchEngine.Baidu"] = "百度",
                ["SearchEngine.Google"] = "Google",
                ["SearchEngine.Bing"] = "必应",
            },
            FileTypeFilterNames = new Dictionary<string, string>
            {
                [SettingFileFilterKeys.CcCedict] = "CC-CEDICT 文本文件",
                [SettingFileFilterKeys.Ecdict] = "ECDICT CSV 文件",
            },
        },
        DictionaryEmptyHint = "输入英文单词或中文词语，↑/↓ 选择，Enter 查看释义",
        DictionaryLoadingHint = "正在加载词典数据…",
        DictionaryDataSourceNotConfigured = "词典数据源未配置。",
        DictionaryNoResultsTemplate = "没有找到“{0}”的条目",
        DictionaryLoadFailedTemplate = "词典数据加载失败：{0}",
        DictionaryReadFailedTemplate = "{0} 词典数据读取失败：{1}",
        ConfiguredDictionaryFileMissingTemplate =
            "在 settings.json 中指定的 {0} 词典文件不存在：{1}",
        DictionaryDataFileNotFoundTemplate =
            "未找到 {0} 词典数据文件（{1}）。可将文件放到：{2} 或 {3}。下载地址：{4}",
        SearchEngineBaiduName = "百度",
        SearchEngineBingName = "必应",
    };

    public static AppStrings English { get; } = new()
    {
        Language = AppLanguage.English,
        WebSearchMode = new SearchModeTexts(
            "Web Search",
            "Type keywords and press Enter to search",
            "Press Enter to search with your default search engine"),
        AskAiMode = new SearchModeTexts(
            "Ask DeepSeek",
            "Type a question and press Enter to open DeepSeek in your browser",
            "Press Enter to open DeepSeek in your browser"),
        DictionaryMode = new SearchModeTexts(
            "Dictionary",
            "Type an English word or Chinese term",
            "Search as you type; press Enter to view definitions"),
        TrayToolTip = "Crispy Searchbar",
        ShowHideSearchBar = "Show / Hide Search Bar",
        OpenSettings = "Settings",
        Exit = "Exit",
        SettingsTexts = new AppSettingsTexts
        {
            WindowTitle = "Settings",
            Save = "Save",
            Browse = "Browse…",
            Clear = "Clear",
            SavedStatus = "Saved to settings.json",
            ValidationFailedStatus = "Fix the settings marked with an error first.",
            ConfigFileLabel = "Configuration file:",
            OpenConfigFile = "Open configuration file",
            SaveFailedTemplate = "Failed to save: {0}",
            UrlTemplateRequiredError = "Enter a URL template.",
            UrlTemplatePlaceholderError = "The URL template must contain the {0} placeholder.",
            SectionTitles = new Dictionary<string, string>
            {
                [nameof(SettingsSection.General)] = "General",
                [nameof(SettingsSection.Appearance)] = "Appearance",
                [nameof(SettingsSection.Search)] = "Search",
                [nameof(SettingsSection.Dictionary)] = "Dictionary",
            },
            FieldLabels = new Dictionary<string, string>
            {
                [nameof(AppSettings.Language)] = "Interface language",
                [nameof(AppSettings.Theme)] = "Theme",
                [nameof(AppSettings.SearchEngine)] = "Default search engine",
                [nameof(AppSettings.ClearQueryOnHide)] = "Clear query when hidden",
                [nameof(AppSettings.AskAiUrlTemplate)] = "Ask DeepSeek URL template",
                [nameof(AppSettings.DictionaryFilePath)] = "Chinese-English dictionary file",
                [nameof(AppSettings.EcdictFilePath)] = "English-Chinese dictionary file",
            },
            FieldDescriptions = new Dictionary<string, string>
            {
                [nameof(AppSettings.Language)] = "The UI language updates immediately after saving.",
                [nameof(AppSettings.Theme)] = "Appearance of the search bar and the settings window.",
                [nameof(AppSettings.SearchEngine)] = "Search engine used by Web Search mode.",
                [nameof(AppSettings.ClearQueryOnHide)] = "Clear the typed query whenever the search bar hides.",
                [nameof(AppSettings.AskAiUrlTemplate)] = "Must contain {0}; the query replaces this placeholder.",
                [nameof(AppSettings.DictionaryFilePath)] =
                    "Accepts a CC-CEDICT UTF-8 text file, typically cedict_ts.u8 (*.u8). Leave empty to use the user data directory or the bundled file.",
                [nameof(AppSettings.EcdictFilePath)] =
                    "Accepts an ECDICT CSV file, typically ecdict.csv (*.csv). Leave empty to use the user data directory or the bundled file.",
            },
            OptionLabels = new Dictionary<string, string>
            {
                ["Language.zh-Hans"] = "简体中文",
                ["Language.en"] = "English",
                ["Theme.System"] = "System",
                ["Theme.Light"] = "Light",
                ["Theme.Dark"] = "Dark",
                ["SearchEngine.Baidu"] = "Baidu",
                ["SearchEngine.Google"] = "Google",
                ["SearchEngine.Bing"] = "Bing",
            },
            FileTypeFilterNames = new Dictionary<string, string>
            {
                [SettingFileFilterKeys.CcCedict] = "CC-CEDICT text file",
                [SettingFileFilterKeys.Ecdict] = "ECDICT CSV file",
            },
        },
        DictionaryEmptyHint =
            "Type an English word or Chinese term; use ↑/↓ to select and press Enter to view the definition",
        DictionaryLoadingHint = "Loading dictionary data…",
        DictionaryDataSourceNotConfigured = "Dictionary data sources are not configured.",
        DictionaryNoResultsTemplate = "No entries found for “{0}”.",
        DictionaryLoadFailedTemplate = "Failed to load dictionary data: {0}",
        DictionaryReadFailedTemplate = "Failed to read {0} dictionary data: {1}",
        ConfiguredDictionaryFileMissingTemplate =
            "The {0} dictionary file specified in settings.json does not exist: {1}",
        DictionaryDataFileNotFoundTemplate =
            "Could not find the {0} dictionary data file ({1}). "
            + "You can place the file at: {2} or {3}. Download URL: {4}",
        SearchEngineBaiduName = "Baidu",
        SearchEngineBingName = "Bing",
    };

    public static AppStrings For(string? language)
        => AppLanguage.Normalize(language) switch
        {
            AppLanguage.English => English,
            AppLanguage.SimplifiedChinese => SimplifiedChinese,
            _ => SimplifiedChinese,
        };

    public string FormatDictionaryNoResults(string query)
        => string.Format(DictionaryNoResultsTemplate, query);

    public string FormatDictionaryLoadFailed(string? detail)
        => string.Format(DictionaryLoadFailedTemplate, detail);

    public string FormatDictionaryReadFailed(string source, string? detail)
        => string.Format(DictionaryReadFailedTemplate, source, detail);

    public string FormatConfiguredDictionaryFileMissing(string source, string path)
        => string.Format(ConfiguredDictionaryFileMissingTemplate, source, path);

    public string FormatDictionaryDataFileNotFound(
        string source,
        string fileName,
        string userDataFilePath,
        string bundledFilePath,
        string downloadUrl)
        => string.Format(
            DictionaryDataFileNotFoundTemplate,
            source,
            fileName,
            userDataFilePath,
            bundledFilePath,
            downloadUrl);

    public string GetSearchEngineDisplayName(SearchEngineKind engine) => engine switch
    {
        SearchEngineKind.Baidu => SearchEngineBaiduName,
        SearchEngineKind.Bing => SearchEngineBingName,
        _ => engine.ToString(),
    };
}

