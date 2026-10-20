namespace FlowWhisper.Core.Hotkey;

/// <summary>
/// Maps a small set of human-readable key names (as stored in AppSettings)
/// to Windows virtual-key codes. Extend as Milestone 7's hotkey picker needs more keys.
/// </summary>
internal static class VirtualKeyMap
{
    private static readonly Dictionary<string, uint> NameToVk = new(StringComparer.OrdinalIgnoreCase)
    {
        ["RightShift"] = 0xA1,
        ["LeftShift"] = 0xA0,
        ["RightCtrl"] = 0xA3,
        ["LeftCtrl"] = 0xA2,
        ["RightAlt"] = 0xA5,
        ["CapsLock"] = 0x14,
    };

    public static uint Resolve(string hotkeyName) =>
        NameToVk.TryGetValue(hotkeyName, out var vk)
            ? vk
            : throw new ArgumentException($"Unknown hotkey name '{hotkeyName}'.", nameof(hotkeyName));
}
