using FlowWhisper.Core.Injection;
using FlowWhisper.Core.Native;

namespace FlowWhisper.Core.Hotkey;

/// <summary>
/// Push-to-talk via a WH_KEYBOARD_LL hook. RegisterHotKey only reports key-down,
/// so a low-level hook is required to detect key-up (release) for push-to-talk.
/// Must be started on the UI thread: WH_KEYBOARD_LL callbacks only fire on a
/// thread that is running a Win32 message loop.
/// </summary>
public sealed class HotkeyService : IHotkeyService
{
    private static readonly TimeSpan WatchdogInterval = TimeSpan.FromMilliseconds(200);

    private readonly PushToTalkTracker _tracker = new();
    private Win32.LowLevelKeyboardProc? _proc;
    private nint _hookHandle;
    private uint _targetVk;
    private SynchronizationContext? _uiContext;
    private Timer? _watchdog;

    // Remote-desktop clients (Omnissa Horizon) install their own low-level hook when their
    // window gets focus and swallow keys, so the dictation key never reached this hook. Hooks
    // run newest first, so this one re-installs itself after every focus change, and keeps
    // doing so while a remote client has focus in case the client re-hooks.
    private static readonly TimeSpan[] RehookDelaysAfterFocusChange = [TimeSpan.FromMilliseconds(300), TimeSpan.FromMilliseconds(1500)];
    private static readonly TimeSpan RemoteRehookInterval = TimeSpan.FromSeconds(3);
    private Win32.WinEventProc? _foregroundProc;
    private nint _foregroundHook;
    private Timer? _remoteRehook;

    public event Action? PushToTalkStarted;
    public event Action? PushToTalkStopped;

    public void Start(string hotkeyName)
    {
        if (_hookHandle != 0)
        {
            throw new InvalidOperationException("Hotkey hook is already running.");
        }

        _targetVk = VirtualKeyMap.Resolve(hotkeyName);
        _uiContext = SynchronizationContext.Current;

        // The delegate must be kept alive in a field — otherwise the GC can
        // collect it while native code still holds a pointer to it.
        _proc = HookCallback;
        _hookHandle = InstallHook();

        // Out-of-context: delivered on this (UI) thread through its message loop.
        _foregroundProc = OnForegroundChanged;
        _foregroundHook = Win32.SetWinEventHook(
            Win32.EVENT_SYSTEM_FOREGROUND, Win32.EVENT_SYSTEM_FOREGROUND, 0, _foregroundProc, 0, 0, Win32.WINEVENT_OUTOFCONTEXT);
        OnForegroundChanged(0, 0, 0, 0, 0, 0, 0);
    }

    private void OnForegroundChanged(nint hook, uint eventType, nint hwnd, int idObject, int idChild, uint thread, uint time)
    {
        foreach (var delay in RehookDelaysAfterFocusChange)
        {
            _ = Task.Delay(delay).ContinueWith(_ => _uiContext?.Post(_ => MoveHookToFront(), null), TaskScheduler.Default);
        }

        var remote = RemoteDesktopClients.IsForegroundRemote();
        if (remote && _remoteRehook is null)
        {
            _remoteRehook = new Timer(_ => _uiContext?.Post(_ => MoveHookToFront(), null), null, RemoteRehookInterval, RemoteRehookInterval);
        }
        else if (!remote)
        {
            _remoteRehook?.Dispose();
            _remoteRehook = null;
        }
    }

    public void ChangeKey(string hotkeyName)
    {
        _targetVk = VirtualKeyMap.Resolve(hotkeyName);
        _tracker.Reset();
    }

    private nint InstallHook()
    {
        var handle = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, _proc!, nint.Zero, 0);
        if (handle == 0)
        {
            throw new InvalidOperationException(
                $"Failed to install keyboard hook (Win32 error {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}).");
        }

        return handle;
    }

    /// <summary>
    /// Low-level hooks run newest first, and remote-desktop clients install theirs when they
    /// get focus and may swallow keys. Re-installing puts this hook first again, so the
    /// key-up of the current press is seen before anyone can swallow it.
    /// </summary>
    private void MoveHookToFront()
    {
        if (_hookHandle == 0)
        {
            return;
        }

        try
        {
            var fresh = InstallHook();
            Win32.UnhookWindowsHookEx(Interlocked.Exchange(ref _hookHandle, fresh));
        }
        catch (InvalidOperationException ex)
        {
            // The old hook is still installed and working; just stay behind the others.
            System.Diagnostics.Debug.WriteLine($"FlowWhisper hook re-install failed: {ex.Message}");
        }
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0)
        {
            var data = System.Runtime.InteropServices.Marshal.PtrToStructure<Win32.KBDLLHOOKSTRUCT>(lParam);
            if (data.vkCode == _targetVk)
            {
                var isKeyDown = wParam == Win32.WM_KEYDOWN || wParam == Win32.WM_SYSKEYDOWN;
                var isKeyUp = wParam == Win32.WM_KEYUP || wParam == Win32.WM_SYSKEYUP;

                // A WH_KEYBOARD_LL callback runs on the UI thread inside the
                // global input pipeline: an unhandled exception here would
                // take the whole process down mid-keystroke, so subscriber
                // failures are swallowed rather than propagated.
                try
                {
                    if (isKeyDown && _tracker.KeyDown(DateTime.UtcNow))
                    {
                        // Not from inside the callback: unhooking the running hook here is unsafe.
                        _uiContext?.Post(_ => MoveHookToFront(), null);
                        StartWatchdog();
                        PushToTalkStarted?.Invoke();
                    }
                    else if (isKeyUp && _tracker.KeyUp())
                    {
                        Stop();
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"FlowWhisper hotkey handler failed: {ex}");
                }
            }
        }

        // Always pass the event through: this is an observer, not a filter,
        // so normal use of Shift/Ctrl/etc. for typing is never affected.
        return Win32.CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void StartWatchdog()
    {
        _watchdog?.Dispose();
        _watchdog = new Timer(_ => _uiContext?.Post(_ => OnWatchdogTick(), null), null, WatchdogInterval, WatchdogInterval);
    }

    private void OnWatchdogTick()
    {
        if (_tracker.Tick(DateTime.UtcNow))
        {
            Stop();
        }
        else if (!_tracker.IsDown)
        {
            _watchdog?.Dispose();
            _watchdog = null;
        }
    }

    private void Stop()
    {
        _watchdog?.Dispose();
        _watchdog = null;
        try
        {
            PushToTalkStopped?.Invoke();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"FlowWhisper hotkey handler failed: {ex}");
        }
    }

    public void Dispose()
    {
        _watchdog?.Dispose();
        _watchdog = null;
        _remoteRehook?.Dispose();
        _remoteRehook = null;
        if (_foregroundHook != 0)
        {
            Win32.UnhookWinEvent(_foregroundHook);
            _foregroundHook = 0;
        }
        if (_hookHandle != 0)
        {
            Win32.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }
        _proc = null;
    }
}
