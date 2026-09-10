using Avalonia;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Themes.Fluent;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// headless 测试宿主默认不带主题与全局样式；这里补齐应用级样式，
/// 让测试窗口与真实应用一样解析 Fluent 模板和共享滚动条规范。
/// </summary>
internal static class TestStyles
{
    public static void Ensure()
    {
        if (Application.Current is not { } application || application.Styles.Count > 0)
        {
            return;
        }

        application.Styles.Add(new FluentTheme());
        application.Styles.Add(new StyleInclude(new Uri("avares://CrispySearchbar/"))
        {
            Source = new Uri("avares://CrispySearchbar/Assets/Styles/Scrollbars.axaml"),
        });
    }
}
