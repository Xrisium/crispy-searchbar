using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Core.Notices;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

/// <summary>
/// 内嵌第三方声明/许可证查看窗口：文档列表来自程序集资源，断网可读。
/// </summary>
public sealed class NoticesWindowTests
{
    [AvaloniaFact]
    public void Window_ListsEveryEmbeddedDocument_AndRendersThirdPartyNoticesFirst()
    {
        TestStyles.Ensure();
        var window = CreateWindow();

        Assert.Equal(NoticesDocuments.All.Count, window.ViewModel.Documents.Count);
        Assert.True(window.ViewModel.Documents.Count >= 6);
        Assert.Equal(
            NoticesDocuments.ThirdPartyNoticesId,
            window.ViewModel.SelectedDocument!.Document.Id);

        var host = FindDocumentHost(window);
        Assert.NotEmpty(host.Children);

        // Markdown 文档渲染成一个块容器，里面是标题/段落/表格对应的控件。
        var markdownHost = Assert.IsType<StackPanel>(Assert.Single(host.Children));
        Assert.NotEmpty(markdownHost.Children);
        Assert.Contains(markdownHost.Children, child => child is TextBlock);

        window.Close();
    }

    [AvaloniaFact]
    public void SelectingLicense_RendersPlainTextLicenseText()
    {
        TestStyles.Ensure();
        var window = CreateWindow();
        var list = window.FindControl<ListBox>("DocumentList");
        Assert.NotNull(list);

        list!.SelectedItem = window.ViewModel.Documents.Single(
            item => item.Document.Id == "MIT");

        var host = FindDocumentHost(window);
        var plainText = Assert.Single(host.Children.OfType<SelectableTextBlock>());
        Assert.Contains("MIT License", plainText.Text!, StringComparison.Ordinal);

        window.Close();
    }

    [AvaloniaFact]
    public void WindowTitle_IsLocalized()
    {
        TestStyles.Ensure();
        var chinese = TranslationCatalog.Default.Resolve(AppLanguage.SimplifiedChinese);
        var window = new NoticesWindow(chinese);
        window.Show();

        Assert.Equal(
            chinese.SettingsTexts.NoticesWindowTitle,
            window.ViewModel.WindowTitle);
        Assert.Equal(chinese.SettingsTexts.NoticesWindowTitle, window.Title);

        window.Close();
    }

    private static NoticesWindow CreateWindow()
    {
        var strings = TranslationCatalog.Default.Resolve(AppLanguage.English);
        var window = new NoticesWindow(strings);
        window.Show();
        return window;
    }

    private static StackPanel FindDocumentHost(NoticesWindow window)
    {
        var host = window.FindControl<StackPanel>("DocumentHost");
        Assert.NotNull(host);
        return host!;
    }
}
