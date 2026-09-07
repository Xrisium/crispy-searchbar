namespace CrispySearchbar.Core.Dictionary;

/// <summary>词典查询方向：根据输入文本特征决定使用的数据源。</summary>
public enum DictionaryQueryDirection
{
    /// <summary>输入含中文，按汉→英查询（CC-CEDICT）。</summary>
    ChineseToEnglish,

    /// <summary>输入不含中文（如英文单词），按英→汉查询（ECDICT）。</summary>
    EnglishToChinese,
}

/// <summary>根据输入文本特征判定词典查询方向。</summary>
public static class DictionaryQueryClassifier
{
    public static DictionaryQueryDirection Detect(string? rawQuery)
        => ContainsCjk(rawQuery)
            ? DictionaryQueryDirection.ChineseToEnglish
            : DictionaryQueryDirection.EnglishToChinese;

    /// <summary>是否包含 CJK 统一表意文字（含扩展 A 与兼容表意区）。</summary>
    public static bool ContainsCjk(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        foreach (var character in text)
        {
            if (IsCjk(character))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsCjk(char character)
        => (character >= '\u3400' && character <= '\u4DBF')
            || (character >= '\u4E00' && character <= '\u9FFF')
            || (character >= '\uF900' && character <= '\uFAFF');
}
