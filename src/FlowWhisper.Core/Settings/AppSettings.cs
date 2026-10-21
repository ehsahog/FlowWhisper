namespace FlowWhisper.Core.Settings;

public sealed class AppSettings
{
    /// <summary>
    /// Right Ctrl by default: holding Right Shift for 8 s makes Windows offer Filter Keys,
    /// and pressing Shift five times offers Sticky Keys; neither is triggered by Ctrl.
    /// </summary>
    public string PushToTalkHotkey { get; set; } = "RightCtrl";

    /// <summary>How see-through the FlowBar and splash glass is, 0–100 %. 0 = the frosted default.</summary>
    public int GlassTransparency { get; set; }

    /// <summary>
    /// "turbo" (large-v3-turbo: best Swedish, 1.4 s on GPU in benchmarks) or
    /// "small" (0.7 s on NPU, weaker Swedish). Renamed from the earlier
    /// WhisperModelSize, whose ggml values no longer apply.
    /// </summary>
    public string WhisperModel { get; set; } = Assets.AssetManifest.DefaultWhisperModel;

    /// <summary>
    /// "GPU", "NPU" or "CPU". GPU is the default: fastest for turbo, and its first-run
    /// compile took 34 s versus 9 minutes for turbo on the NPU.
    /// </summary>
    public string WhisperDevice { get; set; } = "GPU";

    /// <summary>Show text in the FlowBar while still speaking.</summary>
    public bool LivePreviewEnabled { get; set; } = true;

    /// <summary>
    /// Language spoken when dictating: an ISO 639-1 code for Whisper, "auto" to detect,
    /// or empty to follow the Windows display language (see LanguageResolver).
    /// </summary>
    public string Language { get; set; } = "";

    /// <summary>UI language: "sv", "en", or empty to follow the Windows display language.</summary>
    public string UiLanguage { get; set; } = "";

    /// <summary>
    /// Off by default: Phi-3.5-mini proved unreliable on Swedish in benchmarks,
    /// so it's an opt-in experiment rather than part of the default pipeline.
    /// </summary>
    public bool CleanupEnabled { get; set; } = false;

    /// <summary>"GPU" (fastest in benchmarks) or "NPU" (lower power).</summary>
    public string CleanupDevice { get; set; } = "GPU";

    public bool StartAtLogin { get; set; } = false;
    public string? PreferredMicrophoneId { get; set; }
}
