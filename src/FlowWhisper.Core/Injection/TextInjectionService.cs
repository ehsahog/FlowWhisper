using System.Runtime.InteropServices;
using FlowWhisper.Core.Native;

namespace FlowWhisper.Core.Injection;

/// <summary>
/// Inserts dictated text into the focused app by pasting it (Ctrl+V) and then
/// restoring the user's clipboard. Typing the text as a burst of Unicode key events
/// was measured to garble modern Notepad (everything after "Ja," became dots) and
/// triggers apps' autocorrect; pasting is instant and exact in every app tested.
/// Paced typing remains as a fallback when the clipboard can't be opened.
/// </summary>
/// <param name="clipboardOwner">
/// A window handle owned by this process. Win32 requires one: with a null owner,
/// EmptyClipboard leaves no owner and SetClipboardData fails.
/// </param>
public sealed class TextInjectionService(nint clipboardOwner) : ITextInjectionService
{
    /// <summary>Electron apps (e.g. Claude Desktop) read the clipboard asynchronously after Ctrl+V.</summary>
    private static readonly TimeSpan RestoreDelay = TimeSpan.FromMilliseconds(800);

    private static readonly TimeSpan TypingCharDelay = TimeSpan.FromMilliseconds(8);

    /// <summary>
    /// Returned when a remote-desktop client has focus: the text is on the clipboard and
    /// the user pastes it. Omnissa Horizon ignores synthesized keystrokes (its keylogger
    /// protection), so an injected Ctrl+V never reaches the remote machine, while the user's
    /// own Ctrl+V does, and clipboard redirection carries the text over.
    /// </summary>
    public const string RemoteManualPaste = "RemoteManual";

    public Task<string> InjectTextAsync(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return Task.FromResult("Clipboard");
        }

        var remote = RemoteDesktopClients.IsForegroundRemote();
        if (remote)
        {
            // Not private (redirection is a clipboard monitor) and not restored afterwards:
            // the text must stay until the user has pasted it.
            return Task.FromResult(Clipboard.SetText(clipboardOwner, text, @private: false) is null
                ? throw new InvalidOperationException("The clipboard couldn't be opened.")
                : RemoteManualPaste);
        }

        var snapshot = Clipboard.TakeSnapshot(clipboardOwner);
        var sequence = Clipboard.SetText(clipboardOwner, text);
        if (sequence is null)
        {
            TypePaced(text);
            return Task.FromResult("Typed");
        }

        if (!SendCtrlV())
        {
            // Blocked (UIPI: the target runs elevated). Leave the text on the clipboard so
            // the user can paste it by hand instead of silently losing the dictation.
            throw new InvalidOperationException("Paste was blocked; the text was left on the clipboard.");
        }

        if (snapshot is not null)
        {
            ScheduleRestore(snapshot, sequence.Value, RestoreDelay);
        }
        else
        {
            System.Diagnostics.Trace.WriteLine("Clipboard snapshot unavailable; nothing to restore.");
        }

        return Task.FromResult("Clipboard");
    }

    private void ScheduleRestore(Clipboard.Snapshot snapshot, uint sequence, TimeSpan delay)
    {
        // Restore on the same thread when one has a synchronization context (the app's
        // UI thread); clipboard calls are then serialized with any other UI-thread use.
        var context = SynchronizationContext.Current;
        _ = Task.Delay(delay).ContinueWith(_ =>
        {
            void Restore() => Clipboard.Restore(clipboardOwner, snapshot, sequence);
            if (context is null)
            {
                Restore();
            }
            else
            {
                context.Post(_ => Restore(), null);
            }
        }, TaskScheduler.Default);
    }

    private static bool SendCtrlV()
    {
        // Both the virtual key (what local apps read) and the hardware scan code: remote-desktop
        // clients such as Omnissa Horizon forward scan codes to the remote machine, and with
        // scan code 0 the remote side received an empty keystroke and never pasted.
        var ctrlScan = (ushort)Win32.MapVirtualKey(Win32.VK_CONTROL, Win32.MAPVK_VK_TO_VSC);
        var vScan = (ushort)Win32.MapVirtualKey(Win32.VK_V, Win32.MAPVK_VK_TO_VSC);
        var inputs = new[]
        {
            KeyboardInput(Win32.VK_CONTROL, ctrlScan, 0),
            KeyboardInput(Win32.VK_V, vScan, 0),
            KeyboardInput(Win32.VK_V, vScan, Win32.KEYEVENTF_KEYUP),
            KeyboardInput(Win32.VK_CONTROL, ctrlScan, Win32.KEYEVENTF_KEYUP),
        };
        return Win32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Win32.INPUT>()) == inputs.Length;
    }

    /// <summary>One character per SendInput with a short pause: bursts get garbled by some apps.</summary>
    private static void TypePaced(string text)
    {
        foreach (var c in text)
        {
            var inputs = new[]
            {
                KeyboardInput(0, c, Win32.KEYEVENTF_UNICODE),
                KeyboardInput(0, c, Win32.KEYEVENTF_UNICODE | Win32.KEYEVENTF_KEYUP),
            };
            Win32.SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Win32.INPUT>());
            Thread.Sleep(TypingCharDelay);
        }
    }

    private static Win32.INPUT KeyboardInput(ushort vk, ushort scan, uint flags) => new()
    {
        type = Win32.INPUT_KEYBOARD,
        u = new Win32.InputUnion
        {
            ki = new Win32.KEYBDINPUT { wVk = vk, wScan = scan, dwFlags = flags },
        },
    };
}
