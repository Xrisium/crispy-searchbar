using CrispySearchbar.Core.Dictionary;

namespace CrispySearchbar.Dictionary;

/// <summary>
/// 词典资源加载结果：两个方向各带索引或失败提示。
/// 加载一旦完成便在整个应用生命周期内复用，不会重复加载。
/// </summary>
public sealed record DictionaryLoadResult(
    CedictIndex? CedictIndex,
    string? CedictMessage,
    EcdictIndex? EcdictIndex,
    string? EcdictMessage)
{
    public bool HasCedict => CedictIndex is not null;

    public bool HasEcdict => EcdictIndex is not null;

    public static DictionaryLoadResult MissingAll(string message)
        => new(CedictIndex: null, CedictMessage: message, EcdictIndex: null, EcdictMessage: message);
}
