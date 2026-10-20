namespace FlowWhisper.Core.Cleanup;

/// <summary>
/// Language-neutral codes stored in history for what the cleanup step did; the UI
/// translates them when displaying. Early builds stored Swedish words instead, which
/// <see cref="Localization.UiStrings.DescribeCleanup"/> still understands.
/// </summary>
public static class CleanupStatus
{
    public const string Off = "off";
    public const string Applied = "applied";
    public const string Rejected = "rejected";
    public const string Timeout = "timeout";
    public const string Error = "error";
    public const string NotReady = "notready";
}
