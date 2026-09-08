using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace CrispySearchbar.Core.Localization;

/// <summary>
/// 从嵌入的 locales/*.json 构建各语言文案目录。
/// en 是完整基准：翻译文件可只提供部分词条，缺失/空值继承英文；
/// “system” 或无匹配的语言代码最终也回退英文。
/// </summary>
public sealed class TranslationCatalog
{
    private const string ResourceFolderMarker = ".locales.";

    private const string JsonSuffix = ".json";

    private static readonly Lazy<TranslationCatalog> LazyDefault = new(
        () => LoadFromAssembly(typeof(TranslationCatalog).Assembly),
        LazyThreadSafetyMode.ExecutionAndPublication);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Skip,
    };

    private readonly IReadOnlyDictionary<string, AppStrings> _languages;

    private TranslationCatalog(IReadOnlyDictionary<string, AppStrings> languages)
    {
        if (!languages.ContainsKey(AppLanguage.English))
        {
            throw new InvalidOperationException(
                "缺少英文基准翻译 en.json，无法启动本地化目录。");
        }

        _languages = languages;
    }

    /// <summary>进程级目录，读取当前程序集内全部内置翻译。</summary>
    public static TranslationCatalog Default => LazyDefault.Value;

    /// <summary>可用语言代码，按 BCP 47 升序排列（不含 system）。</summary>
    public IReadOnlyList<string> LanguageCodes =>
        _languages.Keys.OrderBy(code => code, StringComparer.Ordinal).ToArray();

    /// <summary>按 LanguageCodes 顺序返回全部语言文案。</summary>
    public IReadOnlyList<AppStrings> Languages =>
        LanguageCodes.Select(code => _languages[code]).ToArray();

    /// <summary>
    /// 设置页语言候选项顺序：跟随系统 → en → 其余按代码排序。
    /// </summary>
    public IReadOnlyList<string> UiLanguageOptionCodes
    {
        get
        {
            var codes = new List<string> { AppLanguage.System };
            codes.Add(AppLanguage.English);
            codes.AddRange(LanguageCodes
                .Where(code => !string.Equals(
                    code,
                    AppLanguage.English,
                    StringComparison.OrdinalIgnoreCase)));
            return codes;
        }
    }

    public AppStrings Resolve(string? language)
        => Resolve(language, CultureInfo.CurrentUICulture);

    public AppStrings Resolve(string? language, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        var code = AppLanguage.IsFollowSystem(language)
            ? AppLanguage.SelectSupportedCode(
                _languages.Keys,
                culture,
                AppLanguage.English)
            : FindCode(language) ?? AppLanguage.English;
        return _languages[code];
    }

    public string GetNativeName(string code)
        => FindCode(code) is { } found
            ? _languages[found].NativeName
            : code;

    private string? FindCode(string? code)
        => string.IsNullOrWhiteSpace(code)
            ? null
            : _languages.Keys.FirstOrDefault(candidate =>
                string.Equals(candidate, code, StringComparison.OrdinalIgnoreCase));

    private static TranslationCatalog LoadFromAssembly(Assembly assembly)
        => Create(ReadEmbeddedSources(assembly));

    internal static Dictionary<string, string> ReadEmbeddedSources(
        Assembly assembly)
    {
        ArgumentNullException.ThrowIfNull(assembly);

        var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.Contains(
                    ResourceFolderMarker,
                    StringComparison.OrdinalIgnoreCase)
                || !resourceName.EndsWith(JsonSuffix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var code = ExtractCode(resourceName);
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException(
                    $"无法读取嵌入翻译资源：{resourceName}");
            using var reader = new StreamReader(stream);
            sources[code] = reader.ReadToEnd();
        }

        return sources;
    }

    internal static TranslationCatalog Create(
        IReadOnlyDictionary<string, string> jsonByCode)
    {
        ArgumentNullException.ThrowIfNull(jsonByCode);

        if (!jsonByCode.TryGetValue(AppLanguage.English, out var englishJson))
        {
            throw new InvalidOperationException(
                "翻译目录缺少 en.json 基准文件。");
        }

        var englishNode = ParseObject(englishJson, AppLanguage.English);
        var languages = new Dictionary<string, AppStrings>(StringComparer.OrdinalIgnoreCase);
        foreach (var pair in jsonByCode)
        {
            languages[pair.Key] = BuildAppStrings(pair.Key, englishNode, pair.Value);
        }

        return new TranslationCatalog(languages);
    }

    private static AppStrings BuildAppStrings(
        string code,
        JsonObject englishNode,
        string partialJson)
    {
        var merged = (JsonObject)englishNode.DeepClone();
        MergeOverEnglish(merged, ParseObject(partialJson, code));

        merged["language"] = code;
        if (!HasNonBlankString(merged, "nativeName"))
        {
            merged["nativeName"] = code;
        }

        if (!HasNonBlankString(merged, "wikipediaLanguage"))
        {
            merged["wikipediaLanguage"] = AppLanguage.English;
        }

        return merged.Deserialize<AppStrings>(JsonOptions)
            ?? throw new InvalidOperationException(
                $"无法解析翻译文件：{code}");
    }

    /// <summary>只覆盖非空字符串；整段缺失/空值保持英文基准。</summary>
    private static void MergeOverEnglish(JsonObject target, JsonObject overlay)
    {
        foreach (var property in overlay)
        {
            var value = property.Value;
            if (value is null || IsBlankString(value))
            {
                continue;
            }

            if (target.TryGetPropertyValue(property.Key, out var existing)
                && existing is JsonObject existingObject
                && value is JsonObject valueObject)
            {
                MergeOverEnglish(existingObject, valueObject);
                continue;
            }

            target[property.Key] = value.DeepClone();
        }
    }

    private static JsonObject ParseObject(string json, string code)
        => JsonNode.Parse(json) as JsonObject
           ?? throw new JsonException(
               $"翻译文件 {code} 的顶层必须是 JSON 对象。");

    private static bool HasNonBlankString(JsonObject node, string propertyName)
    {
        if (!node.TryGetPropertyValue(propertyName, out var value))
        {
            return false;
        }

        return !IsBlankString(value);
    }

    private static bool IsBlankString(JsonNode? node)
        => node is JsonValue value
           && value.TryGetValue<string>(out var text)
           && string.IsNullOrWhiteSpace(text);

    private static string ExtractCode(string resourceName)
    {
        var markerIndex = resourceName.IndexOf(
            ResourceFolderMarker,
            StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            throw new InvalidOperationException(
                $"不是翻译资源：{resourceName}");
        }

        var tail = resourceName[(markerIndex + ResourceFolderMarker.Length)..];
        if (!tail.EndsWith(JsonSuffix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"翻译资源缺少 .json 后缀：{resourceName}");
        }

        var code = tail[..^JsonSuffix.Length];
        return string.IsNullOrWhiteSpace(code)
            ? throw new InvalidOperationException(
                $"翻译资源缺少语言代码：{resourceName}")
            : code;
    }
}
