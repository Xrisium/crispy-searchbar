namespace CrispySearchbar.Core.Configuration;

/// <summary>快捷键修饰键；与平台无关，便于将来映射到各桌面系统。</summary>
[Flags]
public enum ShortcutModifiers
{
    None = 0,
    Alt = 1 << 0,
    Control = 1 << 1,
    Shift = 1 << 2,
    Win = 1 << 3,
}