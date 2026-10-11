using CrispySearchbar.Platform;
using CrispySearchbar.ViewModels;

namespace CrispySearchbar.Emoji;

/// <summary>Owns one external target per search-bar session and serializes focus transfers.</summary>
public sealed class EmojiInputCoordinator(IEmojiTextInputService service)
{
    private ExternalInputTarget? _target;
    private CancellationTokenSource? _pending;
    public bool IsBusy
    {
        get; private set;
    }
    public void CaptureTarget() => _target = service.CaptureTarget();
    public void Cancel()
    {
        _pending?.Cancel();
    }

    public async Task InsertAsync(EmojiModeViewModel emoji, Action hideTemporarily, Action restore, Action finish)
    {
        if (IsBusy || emoji.Selected is not { } selected)
            return;
        if (emoji.CommitVariant())
            return;
        var pending = _pending = new CancellationTokenSource();
        IsBusy = true;
        try
        {
            hideTemporarily();
            var succeeded = await service.InsertAsync(_target, selected.Entry.Sequence, pending.Token);
            if (pending.IsCancellationRequested)
                return;
            if (succeeded)
            {
                await emoji.RecordAsync(selected.Entry.Sequence);
                if (pending.IsCancellationRequested)
                    return;
                if (emoji.KeepOpenAfterInsert)
                    restore();
                else
                    finish();
            }
            else
            {
                restore();
                emoji.ReportInputFailed();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception) { if (!pending.IsCancellationRequested) { restore(); emoji.ReportInputFailed(); } }
        finally { IsBusy = false; pending.Dispose(); if (ReferenceEquals(_pending, pending)) _pending = null; }
    }
}
