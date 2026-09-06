namespace CrispySearchbar.Core.Search;

/// <summary>把用户查询填入 URL 模板：将查询词 URL 编码后替换 {0}。</summary>
public static class OpenUrlBuilder
{
    public static string Build(string template, string query)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(query);

        var encoded = Uri.EscapeDataString(query);
        return template.Contains("{0}", StringComparison.Ordinal)
            ? template.Replace("{0}", encoded, StringComparison.Ordinal)
            : template + (template.Contains("?", StringComparison.Ordinal) ? "&q=" : "?q=") + encoded;
    }
}
