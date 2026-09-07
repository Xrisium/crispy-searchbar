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

    public required string OpenConfigFile { get; init; }

    public required string Exit { get; init; }

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
        OpenConfigFile = "打开配置文件",
        Exit = "退出",
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
        OpenConfigFile = "Open Configuration File",
        Exit = "Exit",
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

