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
        if (_hookHandle != 0)
        {
            Win32.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }
        _proc = null;
    }
}
