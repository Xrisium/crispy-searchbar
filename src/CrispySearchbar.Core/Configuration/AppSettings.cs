namespace CrispySearchbar.Core.Configuration;

/// <summary>应用设置。新增字段必须提供可直接运行的默认值。</summary>
public sealed class AppSettings
{
    public ThemePreference Theme { get; set; } = ThemePreference.System;

    /// <summary>网页搜索使用的默认搜索引擎，可选 baidu/google/bing，默认 baidu。</summary>
    public SearchEngineKind SearchEngine { get; set; } = SearchEngineKind.Baidu;

    /// <summary>搜索框隐藏后是否清空已输入内容，默认 true。</summary>
    public bool ClearQueryOnHide { get; set; } = true;

    /// <summary>询问 AI 地址模板，{0} 将被替换为 URL 编码后的查询词。</summary>
    public string AskAiUrlTemplate { get; set; } =
        "https://chat.deepseek.com/?q={0}";
    /// <summary>
    /// CC-CEDICT（汉英）词典文件路径；留空时依次使用用户数据目录、程序目录中的 cedict_ts.u8。
    /// </summary>
    public string? DictionaryFilePath { get; set; }

    /// <summary>
    /// ECDICT（英汉）词典文件路径；留空时依次使用用户数据目录、程序目录中的 ecdict.csv。
    /// </summary>
    public string? EcdictFilePath { get; set; }
}

