using System.Globalization;

namespace CrispySearchbar.Core.Localization;

/// <summary>
/// 界面语言代码与解析辅助。语言代码本身由 <see cref="TranslationCatalog"/>
/// 按 locales/*.json 动态发现，这里只保留稳定常量与回退规则。
/// </summary>
public static class AppLanguage
{
    /// <summary>“跟随系统”：按当前 UI 文化匹配可用的界面语言。</summary>
    public const string System = "system";

    public const string SimplifiedChinese = "zh-Hans";

    public const string English = "en";

    /// <summary>空值、空白或 system 均表示跟随系统。</summary>
    public static bool IsFollowSystem(string? language)
        => string.IsNullOrWhiteSpace(language)
           || string.Equals(language, System, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// 从可用语言代码中为给定文化选择最合适的代码：
    /// 先精确匹配完整文化名；否则匹配两字母基础语言（多个候选按传入顺序取首个）；仍无匹配时回退 <paramref name="fallback"/>。
    /// </summary>
    public static string SelectSupportedCode(
        IEnumerable<string> availableCodes,
        CultureInfo culture,
        string fallback = English)
    {
        ArgumentNullException.ThrowIfNull(availableCodes);
        ArgumentNullException.ThrowIfNull(culture);

        var codes = availableCodes
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (codes.Length == 0)
        {
            return fallback;
        }

        var cultureName = culture.Name.Replace('_', '-');
        if (cultureName.Length > 0)
        {
            var exact = codes.FirstOrDefault(code =>
                string.Equals(code, cultureName, StringComparison.OrdinalIgnoreCase));
            if (exact is not null)
            {
                return exact;
            }
        }

        var languageName = culture.TwoLetterISOLanguageName;
        var baseMatches = codes
            .Where(code => GetBaseLanguage(code).Equals(
                languageName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (baseMatches.Length > 0)
        {
            return baseMatches[0];
        }

        return fallback;
    }

    private static string GetBaseLanguage(string code)
    {
        var dash = code.IndexOf('-');
        return dash < 0 ? code : code[..dash];
    }
}
