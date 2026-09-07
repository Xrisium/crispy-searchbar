namespace CrispySearchbar.Core.Localization;

/// <summary>
/// 应用界面语言代码（BCP 47）。
/// 当前支持简体中文与英文，默认简体中文；新增语言时登记代码并补充对应 AppStrings 实例。
/// </summary>
public static class AppLanguage
{
    public const string SimplifiedChinese = "zh-Hans";

    public const string English = "en";

    /// <summary>当前可用的语言代码，按此顺序维护配置文档。</summary>
    public static IReadOnlyList<string> Supported { get; } =
        new[] { SimplifiedChinese, English };

    /// <summary>把配置中的语言代码规范化为受支持代码；未知或空值回退到默认简体中文。</summary>
    public static string Normalize(string? language)
    {
        foreach (var supported in Supported)
        {
            if (string.Equals(language, supported, StringComparison.OrdinalIgnoreCase))
            {
                return supported;
            }
        }

        return SimplifiedChinese;
    }
}
