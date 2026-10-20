namespace FlowWhisper.Core.Hotkey;

public interface IHotkeyService : IDisposable
{
    /// <summary>Raised when the configured push-to-talk key is pressed down.</summary>
    event Action? PushToTalkStarted;

    /// <summary>Raised when the configured push-to-talk key is released.</summary>
    event Action? PushToTalkStopped;

    /// <summary>Installs the low-level keyboard hook. Must be called from the UI thread.</summary>
    void Start(string hotkeyName);

    /// <summary>Changes which key is watched, without reinstalling the hook.</summary>
    void ChangeKey(string hotkeyName);
}
