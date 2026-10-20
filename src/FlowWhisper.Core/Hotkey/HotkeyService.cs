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
    private Win32.LowLevelKeyboardProc? _proc;
    private nint _hookHandle;
    private uint _targetVk;
    private bool _isDown;

    public event Action? PushToTalkStarted;
    public event Action? PushToTalkStopped;

    public void Start(string hotkeyName)
    {
        if (_hookHandle != 0)
        {
            throw new InvalidOperationException("Hotkey hook is already running.");
        }

        _targetVk = VirtualKeyMap.Resolve(hotkeyName);

        // The delegate must be kept alive in a field — otherwise the GC can
        // collect it while native code still holds a pointer to it.
        _proc = HookCallback;
        _hookHandle = Win32.SetWindowsHookEx(Win32.WH_KEYBOARD_LL, _proc, nint.Zero, 0);

        if (_hookHandle == 0)
        {
            throw new InvalidOperationException(
                $"Failed to install keyboard hook (Win32 error {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}).");
        }
    }

    public void ChangeKey(string hotkeyName)
    {
        _targetVk = VirtualKeyMap.Resolve(hotkeyName);
        _isDown = false;
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
                    if (isKeyDown && !_isDown)
                    {
                        _isDown = true;
                        PushToTalkStarted?.Invoke();
                    }
                    else if (isKeyUp && _isDown)
                    {
                        _isDown = false;
                        PushToTalkStopped?.Invoke();
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

    public void Dispose()
    {
        if (_hookHandle != 0)
        {
            Win32.UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }
        _proc = null;
    }
}
