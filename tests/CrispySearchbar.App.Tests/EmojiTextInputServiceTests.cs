using CrispySearchbar.Platform;
using Xunit;

namespace CrispySearchbar.Ui.Tests;

public sealed class EmojiTextInputServiceTests
{
    private static readonly ExternalInputTarget Target = new(new(42), 7, new(43));

    private sealed class Platform : IEmojiInputPlatform
    {
        public bool Valid { get; set; } = true;
        public bool FocusRestored { get; set; } = true;
        public bool Foreground { get; set; }
        public int KeysHeldChecks { get; set; }
        public int ActivationChecks { get; set; }
        public uint? SentCount { get; set; }
        public List<string> Calls { get; } = [];
        public Action? OnRestore { get; set; }
        public EmojiTextInputService.NativeInput[]? Inputs { get; private set; }
        public ExternalInputTarget? CaptureTarget() => Target;
        public bool IsValidTarget(ExternalInputTarget target) => Valid;
        public bool AnyInputKeyDown() => KeysHeldChecks-- > 0;
        public void RequestActivation(ExternalInputTarget target) => Calls.Add("activate");
        public bool IsForeground(ExternalInputTarget target)
        {
            if (ActivationChecks-- == 0)
                Foreground = true;
            return Foreground;
        }
        public bool RestoreFocus(ExternalInputTarget target)
        {
            Assert.Equal(Target, target);
            Calls.Add("focus");
            OnRestore?.Invoke();
            return FocusRestored;
        }
        public uint Send(EmojiTextInputService.NativeInput[] inputs)
        {
            Calls.Add("send");
            Inputs = inputs;
            return SentCount ?? (uint)inputs.Length;
        }
    }

    [Fact]
    public async Task ReleasedKeysAndDelayedActivationRestoreOriginalFocusBeforeUnicodeInput()
    {
        var platform = new Platform { KeysHeldChecks = 2, ActivationChecks = 2 };
        var service = new EmojiTextInputService(platform);
        Assert.Equal(Target, service.CaptureTarget());
        const string sequence = "👩🏽‍💻❤️🇨🇳";

        Assert.True(await service.InsertAsync(Target, sequence, CancellationToken.None));

        Assert.Equal(new[] { "activate", "focus", "send" }, platform.Calls);
        Assert.Equal(sequence, new string(platform.Inputs!.Where((_, i) => i % 2 == 0)
            .Select(input => (char)input.Keyboard.Scan).ToArray()));
    }

    [Fact]
    public async Task MissingOrDestroyedTargetDoesNotActivateOrSend()
    {
        var platform = new Platform { Valid = false };
        var service = new EmojiTextInputService(platform);
        Assert.False(await service.InsertAsync(null, "👍", CancellationToken.None));
        Assert.False(await service.InsertAsync(Target, "👍", CancellationToken.None));
        Assert.Empty(platform.Calls);
    }

    [Fact]
    public async Task FailedFocusRestorationDoesNotReportSuccessfulInput()
    {
        var platform = new Platform { FocusRestored = false };
        Assert.False(await new EmojiTextInputService(platform).InsertAsync(Target, "👍", CancellationToken.None));
        Assert.Equal(new[] { "activate", "focus" }, platform.Calls);
        Assert.Null(platform.Inputs);
    }

    [Fact]
    public async Task LostForegroundDuringFocusRestorationDoesNotSendToAnotherApp()
    {
        var platform = new Platform();
        platform.OnRestore = () => platform.Foreground = false;
        Assert.False(await new EmojiTextInputService(platform).InsertAsync(Target, "👍", CancellationToken.None));
        Assert.Null(platform.Inputs);
    }

    [Fact]
    public async Task PartialSubmissionIsNeverRetried()
    {
        var platform = new Platform { SentCount = 1 };
        Assert.False(await new EmojiTextInputService(platform).InsertAsync(Target, "👍", CancellationToken.None));
        Assert.Single(platform.Calls, call => call == "send");
    }

    [Fact]
    public async Task CancellationWhileWaitingForReleaseDoesNotActivateOrSend()
    {
        using var cancellation = new CancellationTokenSource();
        var platform = new Platform { KeysHeldChecks = int.MaxValue };
        var insertion = new EmojiTextInputService(platform).InsertAsync(Target, "👍", cancellation.Token);
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => insertion);
        Assert.Empty(platform.Calls);
    }
}
