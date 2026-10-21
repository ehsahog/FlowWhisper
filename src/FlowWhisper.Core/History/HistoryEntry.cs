namespace FlowWhisper.Core.History;

public sealed class HistoryEntry
{
    public int Id { get; set; }
    public DateTime TimestampUtc { get; set; }
    public string Text { get; set; } = string.Empty;
    public string InjectionMethod { get; set; } = "SendInput";

    /// <summary>What the cleanup step did: one of the <see cref="Cleanup.CleanupStatus"/> codes.</summary>
    public string Cleanup { get; set; } = FlowWhisper.Core.Cleanup.CleanupStatus.Off;

    /// <summary>What the self-correction step did: one of the <see cref="Corrections.CorrectionStatus"/> codes.</summary>
    public string Correction { get; set; } = FlowWhisper.Core.Corrections.CorrectionStatus.None;
}
