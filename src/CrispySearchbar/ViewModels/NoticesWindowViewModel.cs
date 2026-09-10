using System.ComponentModel;
using System.Runtime.CompilerServices;
using CrispySearchbar.Core.Localization;
using CrispySearchbar.Core.Notices;

namespace CrispySearchbar.ViewModels;

/// <summary>内嵌第三方声明/许可证查看窗口的视图模型。</summary>
public sealed class NoticesWindowViewModel : INotifyPropertyChanged
{
    private NoticesDocumentItemViewModel? _selectedDocument;

    public NoticesWindowViewModel(AppStrings strings)
    {
        ArgumentNullException.ThrowIfNull(strings);

        WindowTitle = strings.SettingsTexts.NoticesWindowTitle;
        Intro = strings.SettingsTexts.NoticesIntro;
        Documents = NoticesDocuments.All
            .Select(document => new NoticesDocumentItemViewModel(
                document,
                document.IsThirdPartyNotices
                    ? strings.SettingsTexts.AboutThirdPartyNoticesLinkLabel
                    : document.Id))
            .ToArray();
        _selectedDocument = Documents.FirstOrDefault();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string WindowTitle { get; }

    public string Intro { get; }

    public IReadOnlyList<NoticesDocumentItemViewModel> Documents { get; }

    public NoticesDocumentItemViewModel? SelectedDocument
    {
        get => _selectedDocument;
        set
        {
            if (ReferenceEquals(_selectedDocument, value))
            {
                return;
            }

            _selectedDocument = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>查看窗口左侧列表中的一份文档。</summary>
public sealed class NoticesDocumentItemViewModel
{
    public NoticesDocumentItemViewModel(NoticeDocument document, string label)
    {
        ArgumentNullException.ThrowIfNull(document);
        Document = document;
        Label = label;
    }

    public NoticeDocument Document { get; }

    public string Label { get; }
}
