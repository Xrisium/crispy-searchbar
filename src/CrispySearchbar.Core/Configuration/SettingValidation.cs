namespace CrispySearchbar.Core.Configuration;

/// <summary>设置字段的额外校验规则。</summary>
public enum SettingValidation
{
    None,

    /// <summary>文本必须非空，且包含 URL 查询占位符 {0}。</summary>
    UrlTemplate,
}
