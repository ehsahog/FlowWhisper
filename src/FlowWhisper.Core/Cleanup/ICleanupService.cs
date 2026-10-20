namespace FlowWhisper.Core.Cleanup;

public enum CleanupOutcome
{
    Applied,
    Rejected,
}

/// <param name="Text">The text to inject: the cleaned text if applied, otherwise the raw transcript.</param>
/// <param name="ModelOutput">What the model actually produced, kept for evaluating rejections.</param>
public sealed record CleanupResult(string Text, CleanupOutcome Outcome, string ModelOutput);

public interface ICleanupService
{
    /// <summary>
    /// Asks the local LLM to remove hesitation sounds and fix punctuation. The
    /// model's output is only used if <see cref="TranscriptGuard"/> judges it
    /// faithful; otherwise the raw text is returned with <see cref="CleanupOutcome.Rejected"/>.
    /// Cancelling stops waiting, not the native generation (which is bounded by
    /// a max-token limit proportional to the input).
    /// </summary>
    Task<CleanupResult> CleanupAsync(string rawText, CancellationToken cancellationToken = default);
}
