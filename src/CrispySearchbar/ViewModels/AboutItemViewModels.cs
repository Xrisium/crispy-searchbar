namespace CrispySearchbar.ViewModels;

/// <summary>“关于”分类中的静态说明段落；不绑定 AppSettings，不参与保存。</summary>
public sealed class AboutParagraphViewModel
{
    public AboutParagraphViewModel(string text)
    {
        Text = text;
    }

    public string Text { get; }
}

/// <summary>“关于”分类中的版本行，整行显示本地化的版本句。</summary>
public sealed class AboutVersionViewModel
{
    public AboutVersionViewModel(string text)
    {
        Text = text;
    }

    public string Text { get; }
}

/// <summary>“关于”分类中的可点击链接行。</summary>
public sealed class AboutLinkViewModel
{
    public AboutLinkViewModel(string label, string target, bool openAsFile)
    {
        Label = label;
        Target = target;
        OpenAsFile = openAsFile;
    }

    public string Label { get; }

    public string Target { get; }

    /// <summary>true 表示用默认应用打开本地文件；false 表示用默认浏览器打开 URL。</summary>
    public bool OpenAsFile { get; }
}

/// <summary>“关于”分类中的动作按钮行，当前承载“重置配置文件”。</summary>
public sealed class AboutActionViewModel
{
    public AboutActionViewModel(string text, string description)
    {
        Text = text;
        Description = description;
    }

    public string Text { get; }

    public string Description { get; }
}
