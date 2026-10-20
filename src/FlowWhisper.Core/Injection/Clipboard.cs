using System.Runtime.InteropServices;
using FlowWhisper.Core.Native;

namespace FlowWhisper.Core.Injection;

/// <summary>
/// Win32 clipboard access for paste-based injection: snapshot whatever the user had,
/// put the dictation on the clipboard (kept out of Win+V history and cloud sync), and
/// put the user's content back afterwards.
/// </summary>
internal static class Clipboard
{
    // Handle-based (GDI) formats can't be copied as raw memory. CF_BITMAP and friends are
    // synthesized by Windows from CF_DIB, which is memory-based and is preserved.
    private static readonly HashSet<uint> NonMemoryFormats = [2, 3, 9, 14, 0x80, 0x82, 0x83, 0x8E];

    private static readonly uint ExcludeFromMonitoring = Win32.RegisterClipboardFormat("ExcludeClipboardContentFromMonitorProcessing");
    private static readonly uint CanIncludeInHistory = Win32.RegisterClipboardFormat("CanIncludeInClipboardHistory");
    private static readonly uint CanUploadToCloud = Win32.RegisterClipboardFormat("CanUploadToCloudClipboard");

    public sealed record Snapshot(IReadOnlyList<(uint Format, byte[] Data)> Items);

    public static Snapshot? TakeSnapshot(nint owner)
    {
        if (!TryOpen(owner))
        {
            return null;
        }

        try
        {
            var items = new List<(uint, byte[])>();
            for (var format = Win32.EnumClipboardFormats(0); format != 0; format = Win32.EnumClipboardFormats(format))
            {
                if (NonMemoryFormats.Contains(format) || ReadMemory(Win32.GetClipboardData(format)) is not { } data)
                {
                    continue;
                }

                items.Add((format, data));
            }

            return new Snapshot(items);
        }
        finally
        {
            Win32.CloseClipboard();
        }
    }

    /// <summary>Places text on the clipboard. Returns the clipboard sequence number afterwards, or null on failure.</summary>
    public static uint? SetText(nint owner, string text)
    {
        if (!TryOpen(owner))
        {
            return null;
        }

        try
        {
            if (!Win32.EmptyClipboard())
            {
                return null;
            }

            var bytes = new byte[(text.Length + 1) * 2];
            System.Text.Encoding.Unicode.GetBytes(text, bytes);
            if (!SetMemory(Win32.CF_UNICODETEXT, bytes))
            {
                return null;
            }

            // Dictations shouldn't pile up in Win+V history or roam to other devices.
            SetMemory(ExcludeFromMonitoring, [0]);
            SetMemory(CanIncludeInHistory, BitConverter.GetBytes(0));
            SetMemory(CanUploadToCloud, BitConverter.GetBytes(0));
        }
        finally
        {
            Win32.CloseClipboard();
        }

        return Win32.GetClipboardSequenceNumber();
    }

    /// <summary>
    /// Puts the snapshot back — but only if the clipboard still holds our dictation
    /// (sequence number unchanged), so something the user copied meanwhile isn't overwritten.
    /// </summary>
    public static void Restore(nint owner, Snapshot snapshot, uint sequenceAfterSet)
    {
        var current = Win32.GetClipboardSequenceNumber();
        if (current != sequenceAfterSet)
        {
            System.Diagnostics.Trace.WriteLine($"Clipboard restore skipped: changed since paste ({sequenceAfterSet} -> {current}).");
            return;
        }

        if (!TryOpen(owner))
        {
            System.Diagnostics.Trace.WriteLine("Clipboard restore skipped: could not open clipboard.");
            return;
        }


        try
        {
            var emptied = Win32.EmptyClipboard();
            var restored = snapshot.Items.Count(item => SetMemory(item.Format, item.Data));
            System.Diagnostics.Trace.WriteLine(
                $"Clipboard restore: emptied={emptied}, restored {restored}/{snapshot.Items.Count} formats " +
                $"[{string.Join(", ", snapshot.Items.Select(i => $"{i.Format}:{i.Data.Length}B"))}]");
        }
        finally
        {
            Win32.CloseClipboard();
        }

        System.Diagnostics.Trace.WriteLine($"Clipboard sequence after restore: {Win32.GetClipboardSequenceNumber()}");
    }

    private static bool TryOpen(nint owner)
    {
        // Another app may hold the clipboard for a moment (clipboard managers, RDP).
        for (var attempt = 0; attempt < 10; attempt++)
        {
            if (Win32.OpenClipboard(owner))
            {
                return true;
            }

            Thread.Sleep(20);
        }

        return false;
    }

    private static byte[]? ReadMemory(nint handle)
    {
        if (handle == 0)
        {
            return null;
        }

        var size = (int)Win32.GlobalSize(handle);
        var ptr = Win32.GlobalLock(handle);
        if (ptr == 0)
        {
            return null;
        }

        try
        {
            var data = new byte[size];
            Marshal.Copy(ptr, data, 0, size);
            return data;
        }
        finally
        {
            Win32.GlobalUnlock(handle);
        }
    }

    private static bool SetMemory(uint format, byte[] data)
    {
        var handle = Win32.GlobalAlloc(Win32.GMEM_MOVEABLE, (nuint)Math.Max(1, data.Length));
        if (handle == 0)
        {
            return false;
        }

        var ptr = Win32.GlobalLock(handle);
        Marshal.Copy(data, 0, ptr, data.Length);
        Win32.GlobalUnlock(handle);

        if (Win32.SetClipboardData(format, handle) != 0)
        {
            return true; // The clipboard owns the memory now.
        }

        Win32.GlobalFree(handle);
        return false;
    }
}
