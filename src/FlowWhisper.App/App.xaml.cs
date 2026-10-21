using System.Globalization;
using FlowWhisper.Core;
using FlowWhisper.Core.Assets;
using FlowWhisper.Core.Localization;
using FlowWhisper.Core.Audio;
using FlowWhisper.Core.Cleanup;
using FlowWhisper.Core.History;
using FlowWhisper.Core.Hotkey;
using FlowWhisper.Core.Injection;
using FlowWhisper.Core.Settings;
using FlowWhisper.Core.Transcription;
using FlowWhisper.Data;
using H.NotifyIcon;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FlowWhisper_App;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    private MainWindow? _mainWindow;
    private SplashWindow? _splash;
    private static readonly TimeSpan SplashMaxTime = TimeSpan.FromSeconds(10);
    private TaskbarIcon? _trayIcon;
    private FlowBarWindow? _flowBar;
    private IHotkeyService? _hotkeyService;
    private IAudioCaptureService? _audioCaptureService;
    private IWhisperTranscriptionService? _transcriptionService;
    private ICleanupService? _cleanupService;
    private ITextInjectionService? _textInjectionService;
    private IHistoryStore? _historyStore;
    private AppSettings _settings = new();
    private DispatcherQueue? _uiDispatcher;
    private CancellationTokenSource? _previewCts;
    private int _barSession;

    // A preview pass can't be interrupted, so on release the final pass may wait for
    // one in-flight preview. With OpenVINO that's ~1 s at most (vs 10–70 s measured with
    // whisper.cpp on CPU), and the user can turn previews off in Settings.
    private static readonly TimeSpan PreviewInterval = TimeSpan.FromMilliseconds(900);
    private static readonly TimeSpan MinPreviewAudio = TimeSpan.FromSeconds(1);

    // Measured: a working laptop mic's room-noise peak is ~0.02; the dead USB mic
    // produced 0.0000–0.00003. 0.0015 (≈ -56 dBFS) separates them with margin.
    private const float SilentRecordingPeak = 0.0015f;

    public static IServiceProvider Services { get; private set; } = null!;

    /// <summary>Windows' display language, captured before anything could change the thread culture.</summary>
    private static readonly CultureInfo WindowsUiCulture = CultureInfo.CurrentUICulture;

    /// <summary>Texts in the current UI language.</summary>
    public static UiStrings Strings { get; private set; } = LanguageResolver.ResolveUi("", WindowsUiCulture);

    /// <summary>Raised when the UI language changes, so open windows can re-apply their texts.</summary>
    public static event Action? StringsChanged;

    private string DictationLanguage => LanguageResolver.ResolveDictation(_settings.Language, WindowsUiCulture);

    public App()
    {
        InitializeComponent();
        Services = BuildServiceProvider();
    }

    private static IServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ISettingsStore>(_ =>
        {
            return new JsonSettingsStore(System.IO.Path.Combine(AppPaths.Data, "settings.json"));
        });
        services.AddSingleton<IHotkeyService, HotkeyService>();
        services.AddSingleton<IAudioCaptureService, AudioCaptureService>();
        services.AddSingleton<IHistoryStore>(_ =>
        {
            return new SqliteHistoryStore(System.IO.Path.Combine(AppPaths.Data, "history.db"));
        });
        services.AddSingleton(_ =>
        {
            // Multi-GB downloads: the default 100s timeout would abort them; cancellation
            // comes from the user's "Avbryt" button instead.
            var http = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("FlowWhisper/0.1");
            return new AssetInstaller(new AssetDownloader(http));
        });
        return services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _uiDispatcher = DispatcherQueue.GetForCurrentThread();

        var settingsStore = Services.GetRequiredService<ISettingsStore>();
        // Run on the thread pool: blocking the UI thread on an await that wants to
        // resume on the UI thread deadlocks the app at startup.
        _settings = System.Threading.Tasks.Task.Run(settingsStore.LoadAsync).GetAwaiter().GetResult();
        if (!AssetManifest.IsKnownWhisperModel(_settings.WhisperModel))
        {
            _settings.WhisperModel = AssetManifest.DefaultWhisperModel;
        }

        // Before any window is built, so they all start in the right language.
        Strings = LanguageResolver.ResolveUi(_settings.UiLanguage, WindowsUiCulture);

        _splash = new SplashWindow();
        _splash.SetTransparency(_settings.GlassTransparency);
        _splash.ShowSplash(Strings.SplashLoading);

        // At low priority so the splash gets painted first: the work below keeps this
        // thread busy for a second or two, which otherwise showed the splash as a white box.
        _uiDispatcher.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, ContinueLaunch);
    }

    private void ContinueLaunch()
    {
        var uiDispatcher = _uiDispatcher!;

        _trayIcon = (TaskbarIcon)Resources["TrayIcon"];
        var menu = (MenuFlyout)_trayIcon.ContextFlyout!;
        // H.NotifyIcon turns the flyout into a native Win32 menu and invokes each item's
        // Command; Click handlers never fire in that mode.
        ((MenuFlyoutItem)menu.Items[0]).Command = new CommunityToolkit.Mvvm.Input.RelayCommand(OpenSettingsWindow);
        ((MenuFlyoutItem)menu.Items[2]).Command = new CommunityToolkit.Mvvm.Input.RelayCommand(ExitApp);
        ApplyTrayTexts();
        _trayIcon.ForceCreate();

        // Keep the process alive via a hidden main window; the app has no
        // visible UI until the user opens it from the tray menu.
        _mainWindow = new MainWindow();
        _mainWindow.AppWindow.Hide();

        // The hidden main window lives as long as the app, so it can own the clipboard
        // data used for paste-based injection.
        _textInjectionService = new TextInjectionService(WinRT.Interop.WindowNative.GetWindowHandle(_mainWindow));

        _flowBar = new FlowBarWindow();
        _flowBar.SetTransparency(_settings.GlassTransparency);

        _historyStore = Services.GetRequiredService<IHistoryStore>();

        _audioCaptureService = Services.GetRequiredService<IAudioCaptureService>();
        _audioCaptureService.PreferredDeviceId = _settings.PreferredMicrophoneId;
        _audioCaptureService.VolumeChanged += OnVolumeChanged;

        // The hook must be installed on this (UI) thread: WH_KEYBOARD_LL only
        // delivers callbacks to a thread running a Win32 message loop.
        _hotkeyService = Services.GetRequiredService<IHotkeyService>();
        _hotkeyService.PushToTalkStarted += OnPushToTalkStarted;
        _hotkeyService.PushToTalkStopped += OnPushToTalkStopped;
        _hotkeyService.Start(_settings.PushToTalkHotkey);

        var installer = Services.GetRequiredService<AssetInstaller>();
        if (installer.IsWhisperInstalled(_settings.WhisperModel))
        {
            ReloadTranscriptionService(loaded => uiDispatcher.TryEnqueue(() => FinishSplash(loaded)));

            // A first load on a new device compiles the model for minutes; don't keep the
            // splash up that long. The FlowBar explains if the user dictates before it's done.
            var timeout = uiDispatcher.CreateTimer();
            timeout.Interval = SplashMaxTime;
            timeout.IsRepeating = false;
            timeout.Tick += (_, _) =>
            {
                if (Interlocked.Exchange(ref _splash, null) is { } splash)
                {
                    splash.Finish(Strings.SplashStillLoading, TimeSpan.FromSeconds(3));
                }
            };
            timeout.Start();
        }
        else
        {
            // First run: nothing works until the speech model is downloaded, so show
            // the window with the download card instead of sitting silently in the tray.
            Interlocked.Exchange(ref _splash, null)?.Close();
            _mainWindow.AppWindow.Show();
            _mainWindow.Activate();
        }

        if (_settings.CleanupEnabled)
        {
            LoadCleanupServiceInBackground(_settings.CleanupDevice);
        }

        // A second launch exits right away and signals this; show Settings so the click
        // visibly did something.
        new Thread(() =>
        {
            while (Program.ShowSettingsRequested.WaitOne())
            {
                uiDispatcher.TryEnqueue(OpenSettingsWindow);
            }
        })
        { IsBackground = true, Name = "ShowSettingsListener" }.Start();
    }

    /// <summary>
    /// Called by the Settings page after a download finishes, so the newly
    /// installed models start working without restarting the app.
    /// </summary>
    public void OnAssetsInstalled()
    {
        if (_transcriptionService is null)
        {
            ReloadTranscriptionService();
        }

        if (_settings.CleanupEnabled && _cleanupService is null)
        {
            LoadCleanupServiceInBackground(_settings.CleanupDevice);
        }
    }

    private void FinishSplash(bool loaded)
    {
        if (Interlocked.Exchange(ref _splash, null) is not { } splash)
        {
            return;
        }

        if (loaded)
        {
            splash.Finish(Strings.SplashReady(Strings.KeyName(_settings.PushToTalkHotkey)), TimeSpan.FromSeconds(1.8));
        }
        else
        {
            splash.Close(); // The load error is logged; dictating shows what's wrong.
        }
    }

    /// <param name="loaded">Called from a background thread when loading ends; true on success.</param>
    private void ReloadTranscriptionService(Action<bool>? loaded = null)
    {
        var model = _settings.WhisperModel;
        var device = _settings.WhisperDevice;
        var old = Interlocked.Exchange(ref _transcriptionService, null);

        // The first load per model/device compiles it (measured 25–74 s on GPU/NPU, 9 min
        // for turbo on NPU); later loads hit the cache. Either way, keep it off the UI thread.
        _ = System.Threading.Tasks.Task.Run(() =>
        {
            (old as IDisposable)?.Dispose();
            if (!Services.GetRequiredService<AssetInstaller>().IsWhisperInstalled(model))
            {
                loaded?.Invoke(false);
                return;
            }

            try
            {
                var service = new OpenVinoWhisperService(
                    WhisperModelLocator.GetModelDirectory(model),
                    device,
                    CleanupModelLocator.GetRuntimeDirectory(),
                    CleanupModelLocator.GetCacheDirectory());

                // The user may have switched model or device while this was compiling.
                if (_settings.WhisperModel == model && _settings.WhisperDevice == device)
                {
                    Interlocked.Exchange(ref _transcriptionService, service);
                }
                else
                {
                    service.Dispose();
                }

                loaded?.Invoke(true);
            }
            catch (Exception ex)
            {
                LogError($"Couldn't load Whisper ({model} on {device}): {ex.Message}");
                loaded?.Invoke(false);
            }
        });
    }

    /// <summary>
    /// Applies a settings change made from the Settings page to this already-
    /// running instance. Everything applies immediately, including switching
    /// Whisper model or device.
    /// </summary>
    public void ApplySettingsChange(AppSettings updated)
    {
        var strings = LanguageResolver.ResolveUi(updated.UiLanguage, WindowsUiCulture);
        if (strings != Strings)
        {
            Strings = strings;
            ApplyTrayTexts();
            StringsChanged?.Invoke();
        }

        _flowBar?.SetTransparency(updated.GlassTransparency);

        var whisperChanged = updated.WhisperModel != _settings.WhisperModel
            || updated.WhisperDevice != _settings.WhisperDevice;
        _settings = updated;

        if (whisperChanged)
        {
            ReloadTranscriptionService();
        }

        if (_audioCaptureService is not null)
        {
            _audioCaptureService.PreferredDeviceId = updated.PreferredMicrophoneId;
        }

        if (!updated.CleanupEnabled)
        {
            // Frees the ~2 GB model from GPU/NPU memory when the toggle is off.
            ReplaceCleanupService(null);
        }
        else if ((_cleanupService as CleanupService)?.Device != updated.CleanupDevice)
        {
            LoadCleanupServiceInBackground(updated.CleanupDevice);
        }
    }

    private void LoadCleanupServiceInBackground(string device)
    {
        ReplaceCleanupService(null);
        if (!Services.GetRequiredService<AssetInstaller>().IsCleanupInstalled())
        {
            // The Settings page offers the download; OnAssetsInstalled() retries after it.
            return;
        }

        // First load on a device compiles the model (~25–50s); later loads hit
        // the cache (~4s on GPU). Either way, keep it off the UI thread.
        _ = System.Threading.Tasks.Task.Run(() =>
        {
            try
            {
                var service = new CleanupService(
                    CleanupModelLocator.GetModelDirectory(),
                    device,
                    CleanupModelLocator.GetRuntimeDirectory(),
                    CleanupModelLocator.GetCacheDirectory());

                // The user may have switched device or turned cleanup off while this was loading.
                if (_settings.CleanupEnabled && _settings.CleanupDevice == device)
                {
                    ReplaceCleanupService(service);
                }
                else
                {
                    service.Dispose();
                }
            }
            catch (Exception ex)
            {
                LogError($"Couldn't load the cleanup model on {device}: {ex.Message}");
            }
        });
    }

    private void ReplaceCleanupService(ICleanupService? replacement)
    {
        var old = Interlocked.Exchange(ref _cleanupService, replacement);
        (old as IDisposable)?.Dispose();
    }

    private void OnPushToTalkStarted()
    {
        // A new dictation invalidates any pending "hide the bar" from the previous one.
        Interlocked.Increment(ref _barSession);
        try
        {
            _flowBar!.SetStatus(Strings.Listening);
            _flowBar.SetTranscript(string.Empty);
            _flowBar.ResetWaveform();
            _flowBar.ShowBar();
            _audioCaptureService!.StartRecording();
            StartLivePreview();
        }
        catch (Exception ex)
        {
            ShowTransientError(ex is UnauthorizedAccessException or System.Runtime.InteropServices.COMException { HResult: unchecked((int)0x80070005) }
                ? Strings.MicAccessDenied
                : Strings.RecordingStartFailed(ex.Message));
        }
    }

    /// <summary>
    /// While the key is held, re-transcribes everything recorded so far about
    /// once a second so the FlowBar shows text as it's spoken. Purely a
    /// preview: the text that gets typed comes from the final pass on release.
    /// </summary>
    private void StartLivePreview()
    {
        var service = _transcriptionService;
        if (!_settings.LivePreviewEnabled || service is null || _audioCaptureService is null)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        Interlocked.Exchange(ref _previewCts, cts)?.Cancel();
        var capture = _audioCaptureService;
        var language = DictationLanguage;

        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                while (!cts.IsCancellationRequested)
                {
                    await System.Threading.Tasks.Task.Delay(PreviewInterval, cts.Token);
                    if (capture.RecordedDuration < MinPreviewAudio)
                    {
                        continue;
                    }

                    await using var snapshot = capture.CreateSnapshot();
                    var text = await service.TranscribeAsync(snapshot, language, cts.Token);
                    if (!cts.IsCancellationRequested && text.Length > 0)
                    {
                        _uiDispatcher!.TryEnqueue(() => _flowBar!.SetTranscript(text));
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException)
            {
                // Model was swapped mid-dictation; the final pass will report properly.
            }
            catch (Exception ex)
            {
                LogError($"Live preview failed: {ex.Message}");
            }
        });
    }

    private void OnPushToTalkStopped()
    {
        Interlocked.Exchange(ref _previewCts, null)?.Cancel();

        string wavPath;
        try
        {
            wavPath = _audioCaptureService!.StopRecording();
        }
        catch (Exception ex)
        {
            ShowTransientError(Strings.RecordingStopFailed(ex.Message));
            return;
        }

        // A dead/muted mic delivers exact digital silence; Whisper then invents text
        // (observed: one made-up sentence repeated seven times). Catch it here instead.
        if (_audioCaptureService.PeakLevel < SilentRecordingPeak)
        {
            TryDelete(wavPath);
            ShowTransientError(Strings.MicSilent(_audioCaptureService.DeviceName));
            return;
        }

        var transcriptionService = _transcriptionService;
        if (transcriptionService is null)
        {
            ShowTransientError(Services.GetRequiredService<AssetInstaller>().IsWhisperInstalled(_settings.WhisperModel)
                ? Strings.WhisperPreparing(_settings.WhisperDevice)
                : Strings.WhisperMissing);
            return;
        }

        // The bar's job (confirming audio is coming in) ends when the key is released; the
        // text appearing in the target app is the confirmation from here on. It only
        // reappears if something goes wrong.
        _flowBar!.HideBar();

        // Whisper inference can take well over a second; this handler runs on
        // the UI thread (it's invoked from the keyboard hook), so the actual
        // transcription must happen on a background thread instead of
        // blocking here.
        _ = System.Threading.Tasks.Task.Run(async () =>
        {
            try
            {
                // Whisper turns room noise into made-up subtitle credits; if nobody spoke,
                // do nothing at all rather than type invented text into the user's app.
                float[] samples;
                await using (var wav = File.OpenRead(wavPath))
                {
                    samples = WavFile.ReadPcm16MonoAsFloat(wav);
                }

                if (!SpeechDetector.ContainsSpeech(samples))
                {
                    return;
                }

                var rawText = await transcriptionService.TranscribeAsync(wavPath, DictationLanguage);

                if (string.IsNullOrWhiteSpace(rawText))
                {
                    return;
                }

                var (finalText, cleanupStatus) = await CleanupOrFallbackAsync(rawText);

                // async void is fine here: everything inside is caught, and the injection's
                // waits must resume on the UI thread that owns the clipboard window.
                _uiDispatcher!.TryEnqueue(async () =>
                {
                    string method;
                    try
                    {
                        method = await _textInjectionService!.InjectTextAsync(finalText);
                        if (method == TextInjectionService.RemoteManualPaste)
                        {
                            ShowNotice(Strings.ReadyToPaste, Strings.PasteInRemote, TimeSpan.FromSeconds(5));
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        method = "Blocked";
                        ShowTransientError(Strings.PasteBlocked);
                    }
                    catch (Exception ex)
                    {
                        method = "Failed";
                        ShowTransientError(Strings.TranscriptionFailed(ex.Message));
                    }

                    _ = _historyStore?.AddAsync(new HistoryEntry
                    {
                        TimestampUtc = DateTime.UtcNow,
                        Text = finalText,
                        InjectionMethod = method,
                        Cleanup = cleanupStatus,
                    });
                });
            }
            catch (Exception ex)
            {
                _uiDispatcher!.TryEnqueue(() => ShowTransientError(Strings.TranscriptionFailed(ex.Message)));
            }
            finally
            {
                // The text lives on in history; keeping raw voice recordings on disk
                // indefinitely isn't needed and isn't something users would expect.
                TryDelete(wavPath);
            }
        });
    }

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>
    /// Runs the optional LLM cleanup pass with a timeout. Cleanup must never
    /// hold up or corrupt the dictation, so slowness, failure, or an unfaithful
    /// result all fall back to the raw transcript. The returned status is
    /// recorded in history so the experiment can be judged in real use.
    /// </summary>
    private async Task<(string Text, string Status)> CleanupOrFallbackAsync(string rawText)
    {
        if (!_settings.CleanupEnabled)
        {
            return (rawText, CleanupStatus.Off);
        }

        var service = _cleanupService;
        if (service is null)
        {
            return (rawText, CleanupStatus.NotReady);
        }

        try
        {
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(8));
            var result = await service.CleanupAsync(rawText, cts.Token);
            return result.Outcome == CleanupOutcome.Applied
                ? (result.Text, CleanupStatus.Applied)
                : (rawText, CleanupStatus.Rejected);
        }
        catch (OperationCanceledException)
        {
            return (rawText, CleanupStatus.Timeout);
        }
        catch (Exception ex)
        {
            LogError($"Cleanup failed, using raw text: {ex.Message}");
            return (rawText, CleanupStatus.Error);
        }
    }

    /// <summary>Brings the bar back with an informational message (not an error, so not logged).</summary>
    private void ShowNotice(string status, string message, TimeSpan duration)
    {
        Interlocked.Increment(ref _barSession);
        _flowBar!.SetStatus(status);
        _flowBar.SetTranscript(message);
        _flowBar.ResetWaveform();
        _flowBar.ShowBar();
        HideFlowBarAfterDelay(duration);
    }

    private void ShowTransientError(string message)
    {
        // The status line is one short line; the transcript row wraps, so the message goes there.
        // The bar is normally hidden by now (it closes on key release), so bring it back.
        _flowBar!.SetStatus(Strings.Notice);
        _flowBar.SetTranscript(message);
        _flowBar.ResetWaveform();
        _flowBar.ShowBar();
        HideFlowBarAfterDelay();
        LogError(message);
    }

    private static void LogError(string message)
    {
        try
        {
            var dir = AppPaths.Logs;
            System.IO.Directory.CreateDirectory(dir);
            System.IO.File.AppendAllText(
                System.IO.Path.Combine(dir, "error.log"),
                $"{DateTime.Now:O} {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never itself crash the app.
        }
    }

    /// <summary>
    /// Hides the bar after the final text has been readable for a moment —
    /// unless a new dictation has started since, which owns the bar now.
    /// </summary>
    private void HideFlowBarAfterDelay(TimeSpan? after = null)
    {
        var session = Volatile.Read(ref _barSession);
        _ = System.Threading.Tasks.Task.Delay(after ?? TimeSpan.FromMilliseconds(2500)).ContinueWith(_ => _uiDispatcher!.TryEnqueue(() =>
        {
            if (Volatile.Read(ref _barSession) == session)
            {
                _flowBar!.HideBar();
            }
        }));
    }

    /// <summary>Shows the FlowBar at a transparency the user is trying out in Settings (not saved yet).</summary>
    public void PreviewGlass(int percent)
    {
        if (_flowBar is null)
        {
            return;
        }

        // Every slider step restarts the hide timer, so the bar stays up while dragging.
        Interlocked.Increment(ref _barSession);
        _flowBar.SetTransparency(percent);
        _flowBar.SetStatus("FlowWhisper");
        _flowBar.SetTranscript(Strings.GlassPreview);
        _flowBar.ResetWaveform();
        _flowBar.ShowBar();
        HideFlowBarAfterDelay();
    }

    private void OnVolumeChanged(float level)
    {
        // VolumeChanged fires from NAudio's capture thread; UI updates must
        // be marshalled back onto the WinUI dispatcher.
        _uiDispatcher?.TryEnqueue(() => _flowBar?.PushLevel(level));
    }

    private void ApplyTrayTexts()
    {
        var menu = (MenuFlyout)_trayIcon!.ContextFlyout!;
        ((MenuFlyoutItem)menu.Items[0]).Text = Strings.TrayOpenSettings;
        ((MenuFlyoutItem)menu.Items[2]).Text = Strings.TrayExit;
    }

    private void OpenSettingsWindow()
    {
        _mainWindow ??= new MainWindow();
        _mainWindow.AppWindow.Show();
        _mainWindow.Activate();
    }

    private void ExitApp()
    {
        _hotkeyService?.Dispose();
        _audioCaptureService?.Dispose();
        (_transcriptionService as IDisposable)?.Dispose();
        (_cleanupService as IDisposable)?.Dispose();
        (_historyStore as IDisposable)?.Dispose();
        _trayIcon?.Dispose();
        _flowBar?.Close();
        if (_mainWindow is not null)
        {
            _mainWindow.AllowClose = true;
            _mainWindow.Close();
        }
        Environment.Exit(0);
    }
}
