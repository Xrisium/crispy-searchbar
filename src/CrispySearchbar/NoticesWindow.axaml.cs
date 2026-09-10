using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Core.Notices;
using CrispySearchbar.Notices;
using CrispySearchbar.ViewModels;

namespace CrispySearchbar;

/// <summary>
/// 内嵌第三方声明与许可证原文的查看窗口：左侧选文档，右侧按 Markdown 子集/纯文本渲染。
/// 内容来自程序集内嵌资源，断网也能完整查看。
/// </summary>
public partial class NoticesWindow : Window
{
    private bool _initialized;

    public NoticesWindow()
        : this(null)
    {
    }

    public NoticesWindow(AppStrings? strings)
    {
        InitializeComponent();

        var resolved = strings ?? TranslationCatalog.Default.Resolve(AppLanguage.System);
        ViewModel = new NoticesWindowViewModel(resolved);
        DataContext = ViewModel;

        DocumentList.SelectedItem = ViewModel.SelectedDocument;
        _initialized = true;
        RenderSelectedDocument();

        AddHandler(KeyDownEvent, OnWindowKeyDown, RoutingStrategies.Tunnel);
    }

    public NoticesWindowViewModel ViewModel { get; }

    private void OnDocumentSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_initialized)
        {
            RenderSelectedDocument();
        }
    }

    private void RenderSelectedDocument()
    {
        DocumentHost.Children.Clear();
        if (ViewModel.SelectedDocument is not { } selected)
        {
            return;
        }

        DocumentHost.Children.Add(
            NoticesDocumentRenderer.Build(selected.Document, OnLinkActivated));
        DocumentScroll.Offset = new Vector(0, 0);
    }

    /// <summary>
    /// 声明内部的相对链接（licenses/MIT.txt）切换到对应文档，外部 http(s) 链接交给默认浏览器。
    /// </summary>
    private void OnLinkActivated(string url)
    {
        if (NoticesDocuments.ResolveLink(url) is { } document)
        {
            ViewModel.SelectedDocument = ViewModel.Documents.FirstOrDefault(
                item => string.Equals(
                    item.Document.Id,
                    document.Id,
                    StringComparison.OrdinalIgnoreCase));
            DocumentList.SelectedItem = ViewModel.SelectedDocument;
            RenderSelectedDocument();
            return;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        {
            BrowserLauncher.Open(url);
        }
    }

    private void OnWindowKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close();
            e.Handled = true;
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            BeginMoveDrag(e);
        }
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e) => Close();
}
