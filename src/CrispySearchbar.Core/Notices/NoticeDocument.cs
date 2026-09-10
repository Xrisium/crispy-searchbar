namespace CrispySearchbar.Core.Notices;

/// <summary>文档内容类型：决定界面用 Markdown 子集渲染还是按纯文本原样展示。</summary>
public enum NoticeDocumentFormat
{
    /// <summary>纯文本（许可证原文），保持原始换行。</summary>
    PlainText,

    /// <summary>Markdown（THIRD_PARTY_NOTICES.md），按支持的语法子集渲染。</summary>
    Markdown,
}

/// <summary>
/// 一份随程序内嵌分发的第三方声明或许可证文档。
/// 单文件发布时这些文本全部在 exe 内，用户可离线查看。
/// </summary>
public sealed record NoticeDocument(
    string Id,
    string FileName,
    NoticeDocumentFormat Format,
    string Text)
{
    /// <summary>是否是本项目的第三方声明（标题由界面本地化，其它文档用 SPDX 标识作标题）。</summary>
    public bool IsThirdPartyNotices
        => string.Equals(Id, NoticesDocuments.ThirdPartyNoticesId, StringComparison.Ordinal);
}

/// <summary>声明/许可证文档里的一段内容。</summary>
public abstract record NoticesBlock;

/// <summary>标题：<c>Level</c> 为 1–6。</summary>
public sealed record NoticesHeading(int Level, IReadOnlyList<NoticesInline> Inlines) : NoticesBlock;

/// <summary>段落。</summary>
public sealed record NoticesParagraph(IReadOnlyList<NoticesInline> Inlines) : NoticesBlock;

/// <summary>无序列表项。</summary>
public sealed record NoticesBullet(IReadOnlyList<NoticesInline> Inlines) : NoticesBlock;

/// <summary>表格：第一行为表头。</summary>
public sealed record NoticesTable(
    IReadOnlyList<IReadOnlyList<IReadOnlyList<NoticesInline>>> Rows) : NoticesBlock;

/// <summary>行内片段：纯文本，可选粗体与链接目标。</summary>
public sealed record NoticesInline(string Text, bool IsBold = false, string? LinkUrl = null);
