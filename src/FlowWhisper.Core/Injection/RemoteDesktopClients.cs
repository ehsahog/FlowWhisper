using System.Diagnostics;
using FlowWhisper.Core.Native;

namespace FlowWhisper.Core.Injection;

/// <summary>
/// Recognizes remote-desktop/VDI client windows. Pasting into them goes through the client's
/// clipboard redirection, which copies the local clipboard to the remote machine
/// asynchronously, so it needs different timing than a local app.
/// </summary>
public static class RemoteDesktopClients
{
    /// <summary>Process names (without .exe) of common VDI and remote-desktop clients.</summary>
    public static readonly IReadOnlySet<string> ProcessNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        // Omnissa (formerly VMware) Horizon: the launcher and the remote session window.
        "vmware-view", "vmware-remotemks", "horizon-client", "omnissa-horizon-client",
        // Citrix Workspace / Receiver.
        "wfica32", "CDViewer",
        // Microsoft Remote Desktop, Windows App / Azure Virtual Desktop.
        "mstsc", "msrdc", "msrdcw", "Windows365", "WindowsApp",
    };

    public static bool IsRemoteClient(string processName) => ProcessNames.Contains(processName);

    /// <summary>Whether the window that currently has focus belongs to a remote-desktop client.</summary>
    public static bool IsForegroundRemote()
    {
        try
        {
            var window = Win32.GetForegroundWindow();
            if (window == 0 || Win32.GetWindowThreadProcessId(window, out var pid) == 0)
            {
                return false;
            }

            using var process = Process.GetProcessById((int)pid);
            return IsRemoteClient(process.ProcessName);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            return false;
        }
    }
}
