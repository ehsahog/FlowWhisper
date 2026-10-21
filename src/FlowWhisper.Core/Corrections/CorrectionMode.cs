namespace FlowWhisper.Core.Corrections;

/// <summary>
/// How spoken self-corrections ("kl 12, nej förresten kl 13") are handled. Stored as these
/// strings in settings.
/// </summary>
public static class CorrectionMode
{
    /// <summary>Text is used as transcribed.</summary>
    public const string Off = "off";

    /// <summary>Deterministic rules for times, numbers, weekdays and single words. Instant.</summary>
    public const string Rules = "rules";

    /// <summary>The local language model reads every dictation. Slower; output is guarded.</summary>
    public const string Ai = "ai";

    /// <summary>Rules first; the model only when a correction phrase was said but no rule fit.</summary>
    public const string Hybrid = "hybrid";

    public static bool UsesModel(string mode) => mode is Ai or Hybrid;
}
