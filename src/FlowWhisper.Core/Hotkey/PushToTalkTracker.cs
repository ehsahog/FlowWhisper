namespace FlowWhisper.Core.Hotkey;

/// <summary>
/// Decides when push-to-talk starts and stops from the key events the hook sees, plus a
/// periodic tick. The tick catches a release whose key-up never arrived: remote-desktop
/// clients (Omnissa Horizon, Citrix, RDP) grab the keyboard with their own hook and can
/// swallow it, which left the recording running forever.
/// </summary>
public sealed class PushToTalkTracker
{
    /// <summary>
    /// A held key auto-repeats at least every ~0.4 s (Windows' slowest repeat rate). Once
    /// repeats have been seen, a gap this long means the key was released unseen.
    /// </summary>
    public static readonly TimeSpan RepeatGap = TimeSpan.FromSeconds(1.5);

    /// <summary>Last resort for keys that never auto-repeat to us.</summary>
    public static readonly TimeSpan MaxHold = TimeSpan.FromMinutes(3);

    private DateTime _pressedAt;
    private DateTime _lastDown;
    private int _repeats;

    public bool IsDown { get; private set; }

    /// <summary>Returns true when this key-down starts push-to-talk.</summary>
    public bool KeyDown(DateTime now)
    {
        if (IsDown)
        {
            _repeats++;
            _lastDown = now;
            return false;
        }

        IsDown = true;
        _pressedAt = _lastDown = now;
        _repeats = 0;
        return true;
    }

    /// <summary>Returns true when this key-up stops push-to-talk.</summary>
    public bool KeyUp()
    {
        if (!IsDown)
        {
            return false;
        }

        IsDown = false;
        return true;
    }

    /// <summary>Returns true when push-to-talk should stop because the release was missed.</summary>
    public bool Tick(DateTime now)
    {
        if (!IsDown)
        {
            return false;
        }

        var repeatsStopped = _repeats > 0 && now - _lastDown > RepeatGap;
        if (repeatsStopped || now - _pressedAt > MaxHold)
        {
            IsDown = false;
            return true;
        }

        return false;
    }

    public void Reset() => IsDown = false;
}
