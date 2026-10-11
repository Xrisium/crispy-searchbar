namespace CrispySearchbar.Platform;

internal readonly record struct EmojiShellKeyDecision(bool Suppress = false, bool Trigger = false, bool MaskWinMenu = false);

/// <summary>Tracks a physical chord independently of the Windows hook and UI dispatcher.</summary>
internal sealed class EmojiShellHotkeyState
{
    private readonly bool[] _down = new bool[256];
    private bool _periodSuppressed;
    private bool _menuMasked;

    public EmojiShellKeyDecision Handle(int key, bool up, bool injected, Func<bool> shouldSkip)
    {
        if (injected || key is < 0 or >= 256)
            return default;
        var repeated = _down[key];
        _down[key] = !up;
        var win = _down[0x5b] || _down[0x5c];
        if (!win)
            _menuMasked = false;
        if (key != 0xbe)
            return default;
        if (up)
        {
            var suppressed = _periodSuppressed;
            _periodSuppressed = false;
            return new(Suppress: suppressed);
        }
        // Keep the captured press suppressed until release, including repeats after Win-up.
        if (repeated)
            return new(Suppress: _periodSuppressed);
        var otherModifier = _down[0x10] || _down[0x11] || _down[0x12]
            || _down[0xa0] || _down[0xa1] || _down[0xa2] || _down[0xa3] || _down[0xa4] || _down[0xa5];
        if (!win || otherModifier || shouldSkip())
            return default;
        _periodSuppressed = true;
        var mask = !_menuMasked;
        _menuMasked = true;
        return new(Suppress: true, Trigger: true, MaskWinMenu: mask);
    }

    public void Reset()
    {
        Array.Clear(_down);
        _periodSuppressed = _menuMasked = false;
    }
}
