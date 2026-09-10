using System.Reflection;

namespace CrispySearchbar.Core.Notices;

/// <summary>
/// 随 <c>CrispySearchbar</c> 程序集内嵌分发的第三方声明与许可证原文。
/// 资源逻辑名形如 <c>CrispySearchbar.Notices.{文件名}</c>，文档 Id 取文件名（去扩展名）。
/// </summary>
public static class NoticesDocuments
{
    /// <summary>第三方声明文档的 Id（标题由界面本地化）。</summary>
    public const string ThirdPartyNoticesId = "THIRD_PARTY_NOTICES";

    /// <summary>内嵌资源的逻辑名前缀。</summary>
    public const string ResourcePrefix = "CrispySearchbar.Notices.";

    private const string MarkdownExtension = ".md";

    private static readonly Lazy<IReadOnlyList<NoticeDocument>> LazyAll = new(
        Load,
        LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>全部文档：第三方声明在最前，其余按 SPDX 标识升序。</summary>
    public static IReadOnlyList<NoticeDocument> All => LazyAll.Value;

    /// <summary>按 Id 查找文档（大小写不敏感）；找不到返回 null。</summary>
    public static NoticeDocument? Find(string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return null;
        }

        return All.FirstOrDefault(document => string.Equals(
            document.Id,
            id,
            StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>把声明里的相对链接（如 licenses/MIT.txt）解析成文档 Id；不是内嵌文档时返回 null。</summary>
    public static NoticeDocument? ResolveLink(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        var normalized = url.Replace('\\', '/').Trim();
        var fileName = normalized[(normalized.LastIndexOf('/') + 1)..];
        var id = Path.GetFileNameWithoutExtension(fileName);
        return Find(id);
    }

    private static IReadOnlyList<NoticeDocument> Load()
    {
        var assembly = typeof(NoticesDocuments).Assembly;
        var documents = new List<NoticeDocument>();
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            {
                continue;
            }

            var fileName = resourceName[ResourcePrefix.Length..];
            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"无法读取内嵌声明资源：{resourceName}");
            using var reader = new StreamReader(stream);
            documents.Add(new NoticeDocument(
                Path.GetFileNameWithoutExtension(fileName),
                fileName,
                fileName.EndsWith(MarkdownExtension, StringComparison.OrdinalIgnoreCase)
                    ? NoticeDocumentFormat.Markdown
                    : NoticeDocumentFormat.PlainText,
                reader.ReadToEnd()));
        }

        return documents
            .OrderBy(document => document.IsThirdPartyNotices ? 0 : 1)
            .ThenBy(document => document.Id, StringComparer.Ordinal)
            .ToArray();
    }
}
