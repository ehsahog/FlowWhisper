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
/// Runs the configured <see cref="CorrectionMode"/>. Each mode uses only its own method, so
/// a test of one mode never shows another's result: "AI" never falls back to the rules, and
/// "rules + AI" only asks the model when the rules changed nothing. Whenever the model is
/// missing, slow, failing or rejected by <see cref="CorrectionGuard"/>, the dictation is used
/// exactly as said, so dictation is never blocked by this step.
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

        switch (mode)
        {
            case CorrectionMode.Rules:
                return ByRules(text);

            case CorrectionMode.Hybrid:
                // The rules handled it, or nothing sounds like a correction: no model needed.
                var byRules = ByRules(text);
                if (byRules.Status == CorrectionStatus.Rules || !SelfCorrectionRules.HasCue(text))
                {
                    return byRules;
                }

                return await AskModelAsync(text, model, timeout);

            case CorrectionMode.Ai:
                return await AskModelAsync(text, model, timeout);

            default:
                return (text, CorrectionStatus.None);
        }
    }

    private static (string Text, string Status) ByRules(string text)
    {
        var corrected = SelfCorrectionRules.Apply(text);
        return corrected != text ? (corrected, CorrectionStatus.Rules) : (text, CorrectionStatus.None);
    }

    private static async Task<(string Text, string Status)> AskModelAsync(string text, ModelCorrection? model, TimeSpan? timeout)
    {
        if (model is null)
        {
            return (text, CorrectionStatus.AiNotReady);
        }

        try
        {
            using var cts = new CancellationTokenSource(timeout ?? ModelTimeout);
            var (corrected, accepted, _) = await model(text, cts.Token);
            if (!accepted)
            {
                return (text, CorrectionStatus.AiRejected);
            }

            return SameWords(corrected, text) ? (text, CorrectionStatus.None) : (corrected, CorrectionStatus.Ai);
        }
        catch (OperationCanceledException)
        {
            return (text, CorrectionStatus.AiTimeout);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException)
        {
            return (text, CorrectionStatus.AiError);
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
