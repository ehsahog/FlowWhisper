namespace FlowWhisper.Core.Corrections;

/// <summary>What the self-correction step did with a dictation; stored in history.</summary>
public static class CorrectionStatus
{
    public const string None = "none";
    public const string Rules = "rules";
    public const string Ai = "ai";
    public const string AiRejected = "ai-rejected";
    public const string AiNotReady = "ai-notready";
    public const string AiTimeout = "ai-timeout";
    public const string AiError = "ai-error";
}

/// <summary>
/// Runs the configured <see cref="CorrectionMode"/>. The model is only a helper: whenever it's
/// missing, slow, failing or rejected by <see cref="CorrectionGuard"/>, the rules' result
/// (or the dictation as said) is used, so dictation is never blocked by this step.
/// </summary>
public static class SelfCorrector
{
    /// <summary>Asks the model; returns its text and whether the guard accepted it.</summary>
    public delegate Task<(string Text, bool Accepted, string ModelOutput)> ModelCorrection(string text, CancellationToken cancellationToken);

    public static readonly TimeSpan ModelTimeout = TimeSpan.FromSeconds(8);

    public static async Task<(string Text, string Status)> ApplyAsync(string text, string mode, ModelCorrection? model, TimeSpan? timeout = null)
    {
        if (mode == CorrectionMode.Off || string.IsNullOrWhiteSpace(text))
        {
            return (text, CorrectionStatus.None);
        }

        var byRules = SelfCorrectionRules.Apply(text);
        var rulesStatus = byRules != text ? CorrectionStatus.Rules : CorrectionStatus.None;

        switch (mode)
        {
            case CorrectionMode.Rules:
                return (byRules, rulesStatus);

            case CorrectionMode.Hybrid:
                // The rules handled it, or nothing sounds like a correction: no model needed.
                if (rulesStatus == CorrectionStatus.Rules || !SelfCorrectionRules.HasCue(text))
                {
                    return (byRules, rulesStatus);
                }

                return await AskModelAsync(text, model, (byRules, rulesStatus), timeout);

            case CorrectionMode.Ai:
                return await AskModelAsync(text, model, (byRules, rulesStatus), timeout);

            default:
                return (text, CorrectionStatus.None);
        }
    }

    private static async Task<(string Text, string Status)> AskModelAsync(
        string text, ModelCorrection? model, (string Text, string Status) fallback, TimeSpan? timeout)
    {
        if (model is null)
        {
            return (fallback.Text, fallback.Status == CorrectionStatus.None ? CorrectionStatus.AiNotReady : fallback.Status);
        }

        try
        {
            using var cts = new CancellationTokenSource(timeout ?? ModelTimeout);
            var (corrected, accepted, _) = await model(text, cts.Token);
            if (!accepted)
            {
                return (fallback.Text, CorrectionStatus.AiRejected);
            }

            return SameWords(corrected, text) ? (fallback.Text, fallback.Status) : (corrected, CorrectionStatus.Ai);
        }
        catch (OperationCanceledException)
        {
            return (fallback.Text, CorrectionStatus.AiTimeout);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return (fallback.Text, CorrectionStatus.AiError);
        }
    }

    /// <summary>
    /// The model only re-punctuated (nothing was taken back): keep the dictation as transcribed,
    /// since Whisper's punctuation is better than the small model's.
    /// </summary>
    private static bool SameWords(string a, string b) =>
        string.Equals(Normalize(a), Normalize(b), StringComparison.Ordinal);

    private static string Normalize(string s) =>
        string.Join(' ', System.Text.RegularExpressions.Regex.Matches(s.ToLowerInvariant(), @"[\p{L}\p{N}]+").Select(m => m.Value));
}
