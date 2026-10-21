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

    // Splash
    public required string SplashLoading { get; init; }
    public required Func<string, string> SplashReady { get; init; } // key name
    public required string SplashStillLoading { get; init; }

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
    public required string ShiftKeyHint { get; init; }
    public required string OpenKeyboardSettings { get; init; }
    public required string ModelLabel { get; init; }
    public required string ModelTurbo { get; init; }
    public required string ModelSmall { get; init; }
    public required string WhisperDeviceLabel { get; init; }
    public required string WhisperDeviceGpu { get; init; }
    public required string WhisperDeviceNpu { get; init; }
    public required string WhisperDeviceCpu { get; init; }
    public required string LivePreview { get; init; }
    public required string GlassTransparencyLabel { get; init; }
    public required string GlassPreview { get; init; }
    public required string MicrophoneLabel { get; init; }
    public required string MicrophoneSystemDefault { get; init; }
    public required string CleanupToggle { get; init; }
    public required string CleanupDeviceLabel { get; init; }
    public required string CleanupDeviceGpu { get; init; }
    public required string CleanupDeviceNpu { get; init; }
    public required string CleanupHelp { get; init; }
    public required string CorrectionLabel { get; init; }
    public required string CorrectionOff { get; init; }
    public required string CorrectionRules { get; init; }
    public required string CorrectionAi { get; init; }
    public required string CorrectionHybrid { get; init; }
    public required string CorrectionHelp { get; init; }
    public required string CorrectionHistoryLabel { get; init; }
    public required string CorrectionByRules { get; init; }
    public required string CorrectionByAi { get; init; }
    public required string CorrectionRejected { get; init; }
    public required string CorrectionSkipped { get; init; }
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
    public required string ReadyToPaste { get; init; }
    public required string PasteInRemote { get; init; }

    /// <summary>Translates a stored correction status code for history, or null when nothing happened.</summary>
    public string? DescribeCorrection(string status) => status switch
    {
        Corrections.CorrectionStatus.Rules => CorrectionByRules,
        Corrections.CorrectionStatus.Ai => CorrectionByAi,
        Corrections.CorrectionStatus.AiRejected => CorrectionRejected,
        Corrections.CorrectionStatus.AiNotReady or Corrections.CorrectionStatus.AiTimeout or Corrections.CorrectionStatus.AiError => CorrectionSkipped,
        _ => null,
    };

    /// <summary>Display name of a hotkey as stored in settings, e.g. "RightCtrl".</summary>
    public string KeyName(string hotkey) => hotkey switch
    {
        "RightShift" => KeyRightShift,
        "LeftShift" => KeyLeftShift,
        "RightCtrl" => KeyRightCtrl,
        "LeftCtrl" => KeyLeftCtrl,
        "RightAlt" => KeyRightAlt,
        _ => hotkey,
    };

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
        SplashLoading = "Laddar taligenkänningen…",
        SplashReady = key => $"Redo – håll {key} och prata.",
        SplashStillLoading = "Taligenkänningen laddas klart i bakgrunden.",
        Listening = "Lyssnar...",
        Notice = "Obs",
        WhisperSetupTitle = "Taligenkänning behöver laddas ner",
        WhisperSetupBody = (model, size) =>
            $"Taligenkänningsmodellen \"{model}\" och Intels OpenVINO-runtime ({size}) krävs för att diktera. " +
            "De laddas ner en gång och kontrolleras med SHA-256. Sedan körs allt lokalt, utan internet.",
        CleanupSetupTitle = "AI-modellen behöver laddas ner",
        CleanupSetupBody = size => $"AI-städning och AI-rättelser behöver Phi-3.5-modellen ({size}). Den körs lokalt. Avbrutna nedladdningar fortsätter där de slutade.",
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
        ShiftKeyHint = "Håller du Shift länge (höger Shift i 8 sekunder) eller trycker den fem gånger frågar Windows om Tangentfilter eller Tröga tangenter. Välj Höger Ctrl i stället, eller stäng av kortkommandona i Windows.",
        OpenKeyboardSettings = "Öppna Windows tangentbordsinställningar",
        ModelLabel = "Taligenkänningsmodell",
        ModelTurbo = "large-v3-turbo (bäst kvalitet)",
        ModelSmall = "small (snabbast)",
        WhisperDeviceLabel = "Kör taligenkänning på",
        WhisperDeviceGpu = "GPU (rekommenderad)",
        WhisperDeviceNpu = "NPU (strömsnål, bäst med small)",
        WhisperDeviceCpu = "CPU (långsammast)",
        LivePreview = "Visa text medan jag pratar",
        GlassTransparencyLabel = "Rutans genomskinlighet",
        GlassPreview = "Så här genomskinlig blir rutan.",
        MicrophoneLabel = "Mikrofon",
        MicrophoneSystemDefault = "Systemstandard",
        CleanupToggle = "Städa text med AI (experimentellt, Phi-3.5)",
        CleanupDeviceLabel = "Kör AI-modellen på",
        CleanupDeviceGpu = "GPU (snabbast)",
        CleanupDeviceNpu = "NPU (strömsnål)",
        CleanupHelp = "Svar som lägger till eller tappar ord avvisas automatiskt och då används den oredigerade texten. Historiken visar om städningen användes, avvisades eller tog för lång tid.",
        CorrectionLabel = "Förstå rättelser",
        CorrectionOff = "Av",
        CorrectionRules = "Regler (snabbt)",
        CorrectionAi = "AI på allt (långsammare)",
        CorrectionHybrid = "Regler + AI vid behov",
        CorrectionHelp = "Experimentellt. Säg till exempel \"kl 12, nej förresten kl 13\" så skrivs bara \"kl 13\". Regler klarar tider, tal, veckodagar och enstaka ord. AI-lägena använder Phi-3.5-modellen och godtar bara svar där den tog bort det du tog tillbaka.",
        CorrectionHistoryLabel = "rättelse",
        CorrectionByRules = "regler",
        CorrectionByAi = "AI",
        CorrectionRejected = "AI-svar avvisat",
        CorrectionSkipped = "AI hoppades över",
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
        ReadyToPaste = "Klar att klistra in",
        PasteInRemote = "Texten ligger i urklippet – tryck Ctrl+V. Fjärrskrivbordet tar inte emot automatisk inklistring.",
    };

    public static readonly UiStrings English = new()
    {
        Code = "en",
        TrayOpenSettings = "Settings",
        TrayExit = "Exit",
        SplashLoading = "Loading speech recognition…",
        SplashReady = key => $"Ready – hold {key} and speak.",
        SplashStillLoading = "Speech recognition is finishing loading in the background.",
        Listening = "Listening...",
        Notice = "Note",
        WhisperSetupTitle = "Speech recognition needs to be downloaded",
        WhisperSetupBody = (model, size) =>
            $"The speech model \"{model}\" and Intel's OpenVINO runtime ({size}) are needed to dictate. " +
            "They are downloaded once and verified with SHA-256. After that everything runs locally, offline.",
        CleanupSetupTitle = "The AI model needs to be downloaded",
        CleanupSetupBody = size => $"AI cleanup and AI corrections need the Phi-3.5 model ({size}). It runs locally. Interrupted downloads resume where they left off.",
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
        ShiftKeyHint = "Holding Shift (Right Shift for 8 seconds) or pressing it five times makes Windows offer Filter Keys or Sticky Keys. Choose Right Ctrl instead, or turn off those shortcuts in Windows.",
        OpenKeyboardSettings = "Open Windows keyboard settings",
        ModelLabel = "Speech model",
        ModelTurbo = "large-v3-turbo (best quality)",
        ModelSmall = "small (fastest)",
        WhisperDeviceLabel = "Run speech recognition on",
        WhisperDeviceGpu = "GPU (recommended)",
        WhisperDeviceNpu = "NPU (low power, best with small)",
        WhisperDeviceCpu = "CPU (slowest)",
        LivePreview = "Show text while I speak",
        GlassTransparencyLabel = "Bar transparency",
        GlassPreview = "This is how see-through the bar will be.",
        MicrophoneLabel = "Microphone",
        MicrophoneSystemDefault = "System default",
        CleanupToggle = "Clean up text with AI (experimental, Phi-3.5)",
        CleanupDeviceLabel = "Run the AI model on",
        CleanupDeviceGpu = "GPU (fastest)",
        CleanupDeviceNpu = "NPU (low power)",
        CleanupHelp = "Results that add or drop words are rejected automatically and the unedited text is used instead. History shows whether cleanup was used, rejected or took too long.",
        CorrectionLabel = "Understand corrections",
        CorrectionOff = "Off",
        CorrectionRules = "Rules (fast)",
        CorrectionAi = "AI on everything (slower)",
        CorrectionHybrid = "Rules + AI when needed",
        CorrectionHelp = "Experimental. Say for example \"at 12, no wait, at 1\" and only \"at 1\" is typed. Rules handle times, numbers, weekdays and single words. The AI modes use the Phi-3.5 model and only accept answers where it removed what you took back.",
        CorrectionHistoryLabel = "correction",
        CorrectionByRules = "rules",
        CorrectionByAi = "AI",
        CorrectionRejected = "AI answer rejected",
        CorrectionSkipped = "AI skipped",
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
        ReadyToPaste = "Ready to paste",
        PasteInRemote = "The text is on the clipboard – press Ctrl+V. The remote desktop doesn't accept automatic pasting.",
    };
}
