using System.Runtime.InteropServices;
using FlowWhisper.Core.Injection;

namespace FlowWhisper.Core.Tests;

/// <summary>
/// Exercises the real Windows clipboard (no key presses, so nothing is pasted anywhere).
/// The user's clipboard is snapshotted first and put back afterwards.
/// </summary>
[Collection("Clipboard")]
public sealed class ClipboardTests : IDisposable
{
    private readonly nint _owner = CreateWindowEx(0, "STATIC", "fw-test", 0, 0, 0, 0, 0, new nint(-3), 0, 0, 0);
    private readonly Clipboard.Snapshot? _userClipboard;

    public ClipboardTests() => _userClipboard = Clipboard.TakeSnapshot(_owner);

    public void Dispose()
    {
        if (_userClipboard is not null)
        {
            Clipboard.Restore(_owner, _userClipboard, Clipboard.SetText(_owner, "")!.Value);
        }
    }

    [Fact]
    public void RestoresPreviousContentAfterDictation()
    {
        Clipboard.SetText(_owner, "användarens urklipp");
        var snapshot = Clipboard.TakeSnapshot(_owner)!;

        var sequence = Clipboard.SetText(_owner, "dikterad text")!.Value;
        Assert.Equal("dikterad text", ReadText());

        Clipboard.Restore(_owner, snapshot, sequence);
        Assert.Equal("användarens urklipp", ReadText());
    }

    [Fact]
    public void DoesNotOverwriteSomethingCopiedAfterThePaste()
    {
        Clipboard.SetText(_owner, "gammalt");
        var snapshot = Clipboard.TakeSnapshot(_owner)!;
        var sequence = Clipboard.SetText(_owner, "dikterad text")!.Value;

        Clipboard.SetText(_owner, "nyss kopierat av användaren");
        Clipboard.Restore(_owner, snapshot, sequence);

        Assert.Equal("nyss kopierat av användaren", ReadText());
    }

    private string? ReadText()
    {
        Assert.True(OpenClipboard(_owner));
        try
        {
            var handle = GetClipboardData(13);
            return handle == 0 ? null : Marshal.PtrToStringUni(GlobalLock(handle));
        }
        finally
        {
            CloseClipboard();
        }
    }

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern nint CreateWindowEx(int exStyle, string cls, string name, int style, int x, int y, int w, int h,
        nint parent, nint menu, nint instance, nint param);

    [DllImport("user32.dll")] private static extern bool OpenClipboard(nint hwnd);
    [DllImport("user32.dll")] private static extern bool CloseClipboard();
    [DllImport("user32.dll")] private static extern nint GetClipboardData(uint format);
    [DllImport("kernel32.dll")] private static extern nint GlobalLock(nint h);
}
