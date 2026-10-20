namespace FlowWhisper.Core.Localization;

/// <summary>
/// Every user-visible text in the app, in one place per language. All members are
/// <c>required</c>, so a language that misses a text doesn't compile.
/// </summary>
public sealed class UiStrings
{
    public required string Code { get; init; }

    // Tray
    public required string TrayOpenSettings { get; init; }
    public required string TrayExit { get; init; }

    // FlowBar
    public required string Listening { get; init; }
    public required string Notice { get; init; }

    // Download cards
    public required string WhisperSetupTitle { get; init; }
    public required Func<string, string, string> WhisperSetupBody { get; init; } // model, size
    public required string CleanupSetupTitle { get; init; }
    public required Func<string, string> CleanupSetupBody { get; init; } // size
    public required string Download { get; init; }
    public required string Cancel { get; init; }
    public required string DownloadDone { get; init; }
    public required string DownloadCancelled { get; init; }
    public required Func<string, string> DownloadFailed { get; init; } // error
    public required Func<string, string, string> DownloadProgress { get; init; } // done, total

    // Settings
    public required string SettingsHeader { get; init; }
    public required string AppLanguageLabel { get; init; }
    public required string FollowWindows { get; init; }
    public required string DictationLanguageLabel { get; init; }
    public required string DictationLanguageAuto { get; init; }
    public required string HotkeyLabel { get; init; }
    public required string KeyRightShift { get; init; }
    public required string KeyLeftShift { get; init; }
    public required string KeyRightCtrl { get; init; }
    public required string KeyLeftCtrl { get; init; }
    public required string KeyRightAlt { get; init; }
    public required string ModelLabel { get; init; }
    public required string ModelTurbo { get; init; }
    public required string ModelSmall { get; init; }
    public required string WhisperDeviceLabel { get; init; }
    public required string WhisperDeviceGpu { get; init; }
    public required string WhisperDeviceNpu { get; init; }
    public required string WhisperDeviceCpu { get; init; }
    public required string LivePreview { get; init; }
    public required string MicrophoneLabel { get; init; }
    public required string MicrophoneSystemDefault { get; init; }
    public required string CleanupToggle { get; init; }
    public required string CleanupDeviceLabel { get; init; }
    public required string CleanupDeviceGpu { get; init; }
    public required string CleanupDeviceNpu { get; init; }
    public required string CleanupHelp { get; init; }
    public required string StartAtLogin { get; init; }
    public required string Save { get; init; }
    public required string Saved { get; init; }
    public required Func<string, string> SavedButStartupFailed { get; init; } // error
    public required string ChangesApplyNow { get; init; }

    // History
    public required string HistoryHeader { get; init; }
    public required string ClearHistory { get; init; }
    public required string CleanupLabel { get; init; }
    public required string CleanupOff { get; init; }
    public required string CleanupApplied { get; init; }
    public required string CleanupRejected { get; init; }
    public required string CleanupTimeout { get; init; }
    public required string CleanupError { get; init; }
    public required string CleanupNotReady { get; init; }

    // Errors shown in the FlowBar
    public required Func<string, string> MicSilent { get; init; } // device name
    public required string MicAccessDenied { get; init; }
    public required Func<string, string> RecordingStartFailed { get; init; }
    public required Func<string, string> RecordingStopFailed { get; init; }
    public required Func<string, string> WhisperPreparing { get; init; } // device
    public required string WhisperMissing { get; init; }
    public required Func<string, string> TranscriptionFailed { get; init; }
    public required string PasteBlocked { get; init; }

    /// <summary>Translates a stored cleanup status code (or a legacy Swedish value) for display.</summary>
    public string DescribeCleanup(string status) => status switch
    {
        Cleanup.CleanupStatus.Off or "av" or "" => CleanupOff,
        Cleanup.CleanupStatus.Applied or "använd" => CleanupApplied,
        Cleanup.CleanupStatus.Rejected or "avvisad" => CleanupRejected,
        Cleanup.CleanupStatus.Timeout => CleanupTimeout,
        Cleanup.CleanupStatus.Error or "fel" => CleanupError,
        Cleanup.CleanupStatus.NotReady or "inte redo" => CleanupNotReady,
        _ => status,
    };

    public static readonly UiStrings Swedish = new()
    {
        Code = "sv",
        TrayOpenSettings = "Inställningar",
        TrayExit = "Avsluta",
        Listening = "Lyssnar...",
        Notice = "Obs",
        WhisperSetupTitle = "Taligenkänning behöver laddas ner",
        WhisperSetupBody = (model, size) =>
            $"Taligenkänningsmodellen \"{model}\" och Intels OpenVINO-runtime ({size}) krävs för att diktera. " +
            "De laddas ner en gång och kontrolleras med SHA-256. Sedan körs allt lokalt, utan internet.",
        CleanupSetupTitle = "AI-städning behöver laddas ner",
        CleanupSetupBody = size => $"Experimentell AI-städning behöver Phi-3.5-modellen ({size}). Avbrutna nedladdningar fortsätter där de slutade.",
        Download = "Ladda ner",
        Cancel = "Avbryt",
        DownloadDone = "Klart.",
        DownloadCancelled = "Avbrutet – klicka igen för att fortsätta.",
        DownloadFailed = error => $"Misslyckades: {error}",
        DownloadProgress = (done, total) => $"{done} av {total}",
        SettingsHeader = "Inställningar",
        AppLanguageLabel = "Appens språk",
        FollowWindows = "Följ Windows",
        DictationLanguageLabel = "Språk du talar",
        DictationLanguageAuto = "Automatiskt",
        HotkeyLabel = "Diktera-tangent",
        KeyRightShift = "Höger Shift",
        KeyLeftShift = "Vänster Shift",
        KeyRightCtrl = "Höger Ctrl",
        KeyLeftCtrl = "Vänster Ctrl",
        KeyRightAlt = "Höger Alt",
        ModelLabel = "Taligenkänningsmodell",
        ModelTurbo = "large-v3-turbo (bäst kvalitet)",
        ModelSmall = "small (snabbast)",
        WhisperDeviceLabel = "Kör taligenkänning på",
        WhisperDeviceGpu = "GPU (rekommenderad)",
        WhisperDeviceNpu = "NPU (strömsnål, bäst med small)",
        WhisperDeviceCpu = "CPU (långsammast)",
        LivePreview = "Visa text medan jag pratar",
        MicrophoneLabel = "Mikrofon",
        MicrophoneSystemDefault = "Systemstandard",
        CleanupToggle = "Städa text med AI (experimentellt, Phi-3.5)",
        CleanupDeviceLabel = "Kör städning på",
        CleanupDeviceGpu = "GPU (snabbast)",
        CleanupDeviceNpu = "NPU (strömsnål)",
        CleanupHelp = "Svar som lägger till eller tappar ord avvisas automatiskt och då används den oredigerade texten. Historiken visar om städningen användes, avvisades eller tog för lång tid.",
        StartAtLogin = "Starta vid inloggning",
        Save = "Spara",
        Saved = "Sparat.",
        SavedButStartupFailed = error => $"Sparat, men 'starta vid inloggning' misslyckades: {error}",
        ChangesApplyNow = "Ändringar gäller direkt. Saknas en vald modell erbjuds nedladdning högst upp.",
        HistoryHeader = "Historik",
        ClearHistory = "Rensa historik",
        CleanupLabel = "städning",
        CleanupOff = "av",
        CleanupApplied = "använd",
        CleanupRejected = "avvisad",
        CleanupTimeout = "tog för lång tid",
        CleanupError = "fel",
        CleanupNotReady = "inte redo",
        MicSilent = device => $"Mikrofonen \"{device}\" gav inget ljud. Kontrollera mute-knappen eller välj en annan mikrofon i FlowWhispers inställningar.",
        MicAccessDenied = "Mikrofonåtkomst nekad. Tillåt skrivbordsappar i Windows-inställningar > Sekretess > Mikrofon.",
        RecordingStartFailed = error => $"Kunde inte starta inspelning: {error}",
        RecordingStopFailed = error => $"Kunde inte stoppa inspelning: {error}",
        WhisperPreparing = device => $"Taligenkänningen förbereds på {device} – första gången kan det ta en minut.",
        WhisperMissing = "Taligenkänning saknas – ladda ner den via FlowWhisper i systemfältet.",
        TranscriptionFailed = error => $"Transkribering misslyckades: {error}",
        PasteBlocked = "Kunde inte klistra in – programmet körs troligen som administratör. Texten ligger i urklipp, tryck Ctrl+V.",
    };

    public static readonly UiStrings English = new()
    {
        Code = "en",
        TrayOpenSettings = "Settings",
        TrayExit = "Exit",
        Listening = "Listening...",
        Notice = "Note",
        WhisperSetupTitle = "Speech recognition needs to be downloaded",
        WhisperSetupBody = (model, size) =>
            $"The speech model \"{model}\" and Intel's OpenVINO runtime ({size}) are needed to dictate. " +
            "They are downloaded once and verified with SHA-256. After that everything runs locally, offline.",
        CleanupSetupTitle = "AI cleanup needs to be downloaded",
        CleanupSetupBody = size => $"Experimental AI cleanup needs the Phi-3.5 model ({size}). Interrupted downloads resume where they left off.",
        Download = "Download",
        Cancel = "Cancel",
        DownloadDone = "Done.",
        DownloadCancelled = "Cancelled – click again to resume.",
        DownloadFailed = error => $"Failed: {error}",
        DownloadProgress = (done, total) => $"{done} of {total}",
        SettingsHeader = "Settings",
        AppLanguageLabel = "App language",
        FollowWindows = "Follow Windows",
        DictationLanguageLabel = "Language you speak",
        DictationLanguageAuto = "Automatic",
        HotkeyLabel = "Dictation key",
        KeyRightShift = "Right Shift",
        KeyLeftShift = "Left Shift",
        KeyRightCtrl = "Right Ctrl",
        KeyLeftCtrl = "Left Ctrl",
        KeyRightAlt = "Right Alt",
        ModelLabel = "Speech model",
        ModelTurbo = "large-v3-turbo (best quality)",
        ModelSmall = "small (fastest)",
        WhisperDeviceLabel = "Run speech recognition on",
        WhisperDeviceGpu = "GPU (recommended)",
        WhisperDeviceNpu = "NPU (low power, best with small)",
        WhisperDeviceCpu = "CPU (slowest)",
        LivePreview = "Show text while I speak",
        MicrophoneLabel = "Microphone",
        MicrophoneSystemDefault = "System default",
        CleanupToggle = "Clean up text with AI (experimental, Phi-3.5)",
        CleanupDeviceLabel = "Run cleanup on",
        CleanupDeviceGpu = "GPU (fastest)",
        CleanupDeviceNpu = "NPU (low power)",
        CleanupHelp = "Results that add or drop words are rejected automatically and the unedited text is used instead. History shows whether cleanup was used, rejected or took too long.",
        StartAtLogin = "Start when I sign in",
        Save = "Save",
        Saved = "Saved.",
        SavedButStartupFailed = error => $"Saved, but 'start when I sign in' failed: {error}",
        ChangesApplyNow = "Changes apply immediately. If a selected model is missing, a download is offered at the top.",
        HistoryHeader = "History",
        ClearHistory = "Clear history",
        CleanupLabel = "cleanup",
        CleanupOff = "off",
        CleanupApplied = "used",
        CleanupRejected = "rejected",
        CleanupTimeout = "took too long",
        CleanupError = "error",
        CleanupNotReady = "not ready",
        MicSilent = device => $"The microphone \"{device}\" gave no sound. Check its mute button or choose another microphone in FlowWhisper settings.",
        MicAccessDenied = "Microphone access denied. Allow desktop apps in Windows Settings > Privacy > Microphone.",
        RecordingStartFailed = error => $"Couldn't start recording: {error}",
        RecordingStopFailed = error => $"Couldn't stop recording: {error}",
        WhisperPreparing = device => $"Speech recognition is being prepared on the {device} – the first time can take a minute.",
        WhisperMissing = "Speech recognition is missing – download it from FlowWhisper in the system tray.",
        TranscriptionFailed = error => $"Transcription failed: {error}",
        PasteBlocked = "Couldn't paste – the app is probably running as administrator. The text is on the clipboard, press Ctrl+V.",
    };
}
