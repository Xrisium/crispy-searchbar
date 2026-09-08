namespace CrispySearchbar.Core.Configuration;

/// <summary>settings.json 中保存的一种内置模式偏好：Key 为稳定标识，Enabled 决定是否参与 Tab 循环。</summary>
public sealed class ModePreference
{
    public ModePreference()
    {
    }

    public ModePreference(string key, bool enabled)
    {
        Key = key;
        Enabled = enabled;
    }

    public string Key { get; set; } = string.Empty;

    public bool Enabled { get; set; }
}

/// <summary>内置模式键与默认顺序的唯一来源；新增内置模式需同步更新此列表与 SearchModeCatalog。</summary>
public static class ModePreferenceDefaults
{
    public const string WebSearch = "web-search";

    public const string Wikipedia = "wikipedia";

    public const string AskAi = "ask-ai";

    public const string Dictionary = "dictionary";

    public static IReadOnlyList<string> BuiltInOrder { get; } =
        [WebSearch, Wikipedia, AskAi, Dictionary];

    public static ModePreference[] AllEnabled()
        => BuiltInOrder
            .Select(key => new ModePreference(key, enabled: true))
            .ToArray();
}

/// <summary>
/// 把模式偏好归一化为合法列表：丢弃未知/重复键、按默认序补齐缺失键，并保证至少启用一个模式。
/// 全禁用/空/无效输入回退为默认四模式全启用。
/// </summary>
public static class ModePreferenceNormalizer
{
    public static ModePreference[] Normalize(IReadOnlyList<ModePreference>? preferences)
    {
        var defaults = ModePreferenceDefaults.AllEnabled();
        if (preferences is null || preferences.Count == 0)
        {
            return defaults;
        }

        var knownKeys = new HashSet<string>(
            ModePreferenceDefaults.BuiltInOrder,
            StringComparer.Ordinal);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<ModePreference>();

        foreach (var preference in preferences)
        {
            if (preference?.Key is null
                || !knownKeys.Contains(preference.Key)
                || !seen.Add(preference.Key))
            {
                continue;
            }

            result.Add(new ModePreference(preference.Key, preference.Enabled));
        }

        foreach (var key in ModePreferenceDefaults.BuiltInOrder)
        {
            if (seen.Add(key))
            {
                result.Add(new ModePreference(key, enabled: true));
            }
        }

        return result.Any(item => item.Enabled)
            ? result.ToArray()
            : defaults;
    }

    public static bool IsEquivalent(
        IReadOnlyList<ModePreference>? current,
        IReadOnlyList<ModePreference>? normalized)
    {
        if (ReferenceEquals(current, normalized))
        {
            return true;
        }

        if (current is null || normalized is null || current.Count != normalized.Count)
        {
            return false;
        }

        for (var index = 0; index < current.Count; index++)
        {
            if (!string.Equals(
                    current[index].Key,
                    normalized[index].Key,
                    StringComparison.Ordinal)
                || current[index].Enabled != normalized[index].Enabled)
            {
                return false;
            }
        }

        return true;
    }
}
