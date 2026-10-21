using FlowWhisper.Core.Assets;
using FlowWhisper.Core.Audio;
using FlowWhisper.Core.History;
using FlowWhisper.Core.Hotkey;
using FlowWhisper.Core.Localization;
using FlowWhisper.Core.Settings;
using FlowWhisper.Core.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace FlowWhisper_App;

public sealed partial class MainPage : Page
{
    private AppSettings _settings = new();
    private CancellationTokenSource? _whisperDownload;
    private CancellationTokenSource? _cleanupDownload;
    private List<HistoryEntry> _history = [];

    /// <summary>True while controls are filled from settings, so change handlers don't react.</summary>
    private bool _settingControls;

    private static AssetInstaller Installer => App.Services.GetRequiredService<AssetInstaller>();
    private static UiStrings S => App.Strings;

    public MainPage()
    {
        InitializeComponent();
        App.StringsChanged += () => DispatcherQueue.TryEnqueue(ApplyTexts);
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        var settingsStore = App.Services.GetRequiredService<ISettingsStore>();
        _settings = await settingsStore.LoadAsync();

        if (!AssetManifest.IsKnownWhisperModel(_settings.WhisperModel))
        {
            _settings.WhisperModel = AssetManifest.DefaultWhisperModel;
        }

        ApplyTexts();
        ApplySettingsToControls();

        await LoadHistoryAsync();
    }

    /// <summary>
    /// Sets every text on the page from the current UI language. Combo boxes are rebuilt
    /// in place and keep their selection (matched by tag), so this can run at any time.
    /// </summary>
    private void ApplyTexts()
    {
        WhisperSetupTitle.Text = S.WhisperSetupTitle;
        CleanupSetupTitle.Text = S.CleanupSetupTitle;
        if (_whisperDownload is null)
        {
            WhisperDownloadButton.Content = S.Download;
        }

        if (_cleanupDownload is null)
        {
            CleanupDownloadButton.Content = S.Download;
        }

        SettingsHeader.Text = S.SettingsHeader;
        AppLanguageLabel.Text = S.AppLanguageLabel;
        DictationLanguageLabel.Text = S.DictationLanguageLabel;
        HotkeyLabel.Text = S.HotkeyLabel;
        ModelLabel.Text = S.ModelLabel;
        WhisperDeviceLabel.Text = S.WhisperDeviceLabel;
        MicrophoneLabel.Text = S.MicrophoneLabel;
        CleanupDeviceLabel.Text = S.CleanupDeviceLabel;
        CleanupHelpText.Text = S.CleanupHelp;
        GlassTransparencyLabel.Text = S.GlassTransparencyLabel;
        LivePreviewToggle.Header = S.LivePreview;
        CleanupToggle.Header = S.CleanupToggle;
        StartAtLoginToggle.Header = S.StartAtLogin;
        SaveButton.Content = S.Save;
        SaveStatusText.Text = "";
        ChangesApplyText.Text = S.ChangesApplyNow;
        HistoryHeader.Text = S.HistoryHeader;
        ClearHistoryButton.Content = S.ClearHistory;

        // Language names are written in their own language so they're recognizable
        // whatever the UI language is.
        SetItems(AppLanguageComboBox,
            ("", S.FollowWindows),
            ("sv", "Svenska"),
            ("en", "English"));

        SetItems(DictationLanguageComboBox,
            [("", $"{S.FollowWindows} ({DescribeWindowsDictationLanguage()})"),
             ("auto", S.DictationLanguageAuto),
             .. LanguageResolver.DictationLanguages]);

        SetItems(HotkeyComboBox,
            ("RightCtrl", S.KeyRightCtrl),
            ("LeftCtrl", S.KeyLeftCtrl),
            ("RightShift", S.KeyRightShift),
            ("LeftShift", S.KeyLeftShift),
            ("RightAlt", S.KeyRightAlt));
        ShiftHintText.Text = S.ShiftKeyHint;
        KeyboardSettingsLink.Content = S.OpenKeyboardSettings;

        SetItems(ModelComboBox,
            ("turbo", S.ModelTurbo),
            ("small", S.ModelSmall));

        SetItems(WhisperDeviceComboBox,
            ("GPU", S.WhisperDeviceGpu),
            ("NPU", S.WhisperDeviceNpu),
            ("CPU", S.WhisperDeviceCpu));

        SetItems(CleanupDeviceComboBox,
            ("GPU", S.CleanupDeviceGpu),
            ("NPU", S.CleanupDeviceNpu));

        PopulateMicrophones();
        RefreshSetupCards();
        ShowHistory();
    }

    private static string DescribeWindowsDictationLanguage()
    {
        var code = LanguageResolver.ResolveDictation("", System.Globalization.CultureInfo.CurrentUICulture);
        return LanguageResolver.DictationLanguages.FirstOrDefault(l => l.Code == code).NativeName
            ?? S.DictationLanguageAuto;
    }

    private static void SetItems(ComboBox comboBox, params (string Tag, string Text)[] items)
    {
        var selectedTag = (comboBox.SelectedItem as ComboBoxItem)?.Tag as string;
        comboBox.Items.Clear();
        foreach (var (tag, text) in items)
        {
            comboBox.Items.Add(new ComboBoxItem { Tag = tag, Content = text });
        }

        if (selectedTag is not null)
        {
            SelectComboBoxByTag(comboBox, selectedTag);
        }
    }

    private void RefreshSetupCards()
    {
        var model = _settings.WhisperModel;
        var whisperMissing = !Installer.IsWhisperInstalled(model);
        WhisperSetupCard.Visibility = whisperMissing || _whisperDownload is not null ? Visibility.Visible : Visibility.Collapsed;
        WhisperSetupText.Text = S.WhisperSetupBody(model, FormatMegabytes(Installer.WhisperDownloadSize(model)));

        var cleanupMissing = _settings.CleanupEnabled && !Installer.IsCleanupInstalled();
        CleanupSetupCard.Visibility = cleanupMissing || _cleanupDownload is not null ? Visibility.Visible : Visibility.Collapsed;
        CleanupSetupText.Text = S.CleanupSetupBody(FormatMegabytes(Installer.CleanupDownloadSize));
    }

    private async void OnWhisperDownloadClick(object sender, RoutedEventArgs e)
    {
        var model = _settings.WhisperModel;
        await RunDownloadAsync(
            () => ref _whisperDownload, WhisperDownloadButton, WhisperProgressBar, WhisperProgressText,
            (progress, ct) => Installer.InstallWhisperAsync(model, progress, ct));
    }

    private async void OnCleanupDownloadClick(object sender, RoutedEventArgs e) =>
        await RunDownloadAsync(
            () => ref _cleanupDownload, CleanupDownloadButton, CleanupProgressBar, CleanupProgressText,
            Installer.InstallCleanupAsync);

    private delegate ref CancellationTokenSource? CtsSlot();

    /// <summary>
    /// Shared start/cancel/progress handling for both download cards. Clicking
    /// the button while a download runs cancels it; the partial file is kept so
    /// the next click resumes.
    /// </summary>
    private async Task RunDownloadAsync(CtsSlot slot, Button button, ProgressBar bar, TextBlock status,
        Func<IProgress<InstallProgress>, CancellationToken, Task> install)
    {
        if (slot() is { } running)
        {
            running.Cancel();
            return;
        }

        using var cts = new CancellationTokenSource();
        slot() = cts;
        button.Content = S.Cancel;
        bar.Visibility = Visibility.Visible;
        bar.Value = 0;

        var lastPercent = -1;
        var progress = new SyncProgress(p =>
        {
            var percent = (int)(p.BytesDone * 100 / Math.Max(1, p.BytesTotal));
            if (Interlocked.Exchange(ref lastPercent, percent) == percent)
            {
                return;
            }

            DispatcherQueue.TryEnqueue(() =>
            {
                bar.Value = (double)p.BytesDone / p.BytesTotal;
                status.Text = S.DownloadProgress(FormatMegabytes(p.BytesDone), FormatMegabytes(p.BytesTotal));
            });
        });

        try
        {
            await Task.Run(() => install(progress, cts.Token));
            status.Text = S.DownloadDone;
            ((App)Application.Current).OnAssetsInstalled();
        }
        catch (OperationCanceledException)
        {
            status.Text = S.DownloadCancelled;
        }
        catch (Exception ex)
        {
            status.Text = S.DownloadFailed(ex.Message);
        }
        finally
        {
            slot() = null;
            button.Content = S.Download;
            bar.Visibility = Visibility.Collapsed;
            RefreshSetupCards();
        }
    }

    private static string FormatMegabytes(long bytes) => $"{bytes / (1024.0 * 1024.0):N0} MB";

    private sealed class SyncProgress(Action<InstallProgress> report) : IProgress<InstallProgress>
    {
        public void Report(InstallProgress value) => report(value);
    }

    private void PopulateMicrophones()
    {
        var selectedId = (MicComboBox.SelectedItem as AudioDeviceInfo)?.Id ?? _settings.PreferredMicrophoneId;
        var devices = new List<AudioDeviceInfo> { new("", S.MicrophoneSystemDefault) };
        try
        {
            devices.AddRange(AudioDeviceProvider.GetCaptureDevices());
        }
        catch
        {
            // Device enumeration can fail transiently; the default option above still works.
        }

        MicComboBox.ItemsSource = devices;
        MicComboBox.SelectedIndex = Math.Max(0, devices.FindIndex(d => d.Id == selectedId));
    }

    private void ApplySettingsToControls()
    {
        SelectComboBoxByTag(AppLanguageComboBox, _settings.UiLanguage);
        SelectComboBoxByTag(DictationLanguageComboBox, _settings.Language);
        SelectComboBoxByTag(HotkeyComboBox, _settings.PushToTalkHotkey);
        SelectComboBoxByTag(ModelComboBox, _settings.WhisperModel);
        SelectComboBoxByTag(WhisperDeviceComboBox, _settings.WhisperDevice);
        LivePreviewToggle.IsOn = _settings.LivePreviewEnabled;
        _settingControls = true;
        GlassTransparencySlider.Value = _settings.GlassTransparency;
        _settingControls = false;
        CleanupToggle.IsOn = _settings.CleanupEnabled;
        SelectComboBoxByTag(CleanupDeviceComboBox, _settings.CleanupDevice);
        StartAtLoginToggle.IsOn = _settings.StartAtLogin;
    }

    private static void SelectComboBoxByTag(ComboBox comboBox, string tag)
    {
        foreach (var item in comboBox.Items.OfType<ComboBoxItem>())
        {
            if (Equals(item.Tag, tag))
            {
                comboBox.SelectedItem = item;
                return;
            }
        }

        // Unknown value (e.g. a hand-edited settings file): fall back to the first option.
        comboBox.SelectedIndex = 0;
    }

    /// <summary>Shows the FlowBar at the new transparency right away, so the slider can be judged live.</summary>
    private void OnGlassTransparencyChanged(object sender, Microsoft.UI.Xaml.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (!_settingControls)
        {
            ((App)Application.Current).PreviewGlass((int)e.NewValue);
        }
    }

    private void OnHotkeySelectionChanged(object sender, SelectionChangedEventArgs e) =>
        ShiftHintPanel.Visibility = (HotkeyComboBox.SelectedItem as ComboBoxItem)?.Tag is string tag && tag.EndsWith("Shift")
            ? Visibility.Visible
            : Visibility.Collapsed;

    private async void OnKeyboardSettingsClick(object sender, RoutedEventArgs e) =>
        // Accessibility > Keyboard holds the Sticky Keys and Filter Keys shortcut switches.
        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:easeofaccess-keyboard"));

    private static string SelectedTag(ComboBox comboBox) => (string)((ComboBoxItem)comboBox.SelectedItem).Tag;

    private async Task LoadHistoryAsync()
    {
        var historyStore = App.Services.GetRequiredService<IHistoryStore>();
        _history = await historyStore.GetRecentAsync(50);
        ShowHistory();
    }

    private void ShowHistory() =>
        HistoryListView.ItemsSource = _history.Select(e => new HistoryDisplayItem(
            e.Text,
            $"{e.TimestampUtc.ToLocalTime():yyyy-MM-dd HH:mm} · {e.InjectionMethod} · {S.CleanupLabel}: {S.DescribeCleanup(e.Cleanup)}")).ToList();

    private async void OnSaveButtonClick(object sender, RoutedEventArgs e)
    {
        var newHotkey = SelectedTag(HotkeyComboBox);
        var hotkeyChanged = newHotkey != _settings.PushToTalkHotkey;

        _settings.UiLanguage = SelectedTag(AppLanguageComboBox);
        _settings.Language = SelectedTag(DictationLanguageComboBox);
        _settings.PushToTalkHotkey = newHotkey;
        _settings.WhisperModel = SelectedTag(ModelComboBox);
        _settings.WhisperDevice = SelectedTag(WhisperDeviceComboBox);
        _settings.LivePreviewEnabled = LivePreviewToggle.IsOn;
        _settings.GlassTransparency = (int)GlassTransparencySlider.Value;
        _settings.CleanupEnabled = CleanupToggle.IsOn;
        _settings.CleanupDevice = SelectedTag(CleanupDeviceComboBox);
        _settings.StartAtLogin = StartAtLoginToggle.IsOn;
        _settings.PreferredMicrophoneId = ((AudioDeviceInfo)MicComboBox.SelectedItem).Id;

        var settingsStore = App.Services.GetRequiredService<ISettingsStore>();
        await settingsStore.SaveAsync(_settings);

        if (hotkeyChanged)
        {
            App.Services.GetRequiredService<IHotkeyService>().ChangeKey(newHotkey);
        }

        // May switch the UI language, which re-runs ApplyTexts via App.StringsChanged.
        ((App)Application.Current).ApplySettingsChange(_settings);

        try
        {
            StartupManager.SetEnabled(_settings.StartAtLogin);
        }
        catch (Exception ex)
        {
            SetSaveStatusAfterTexts(S.SavedButStartupFailed(ex.Message));
            return;
        }

        SetSaveStatusAfterTexts(S.Saved);
    }

    /// <summary>ApplyTexts runs queued after a language switch and clears the status, so queue behind it.</summary>
    private void SetSaveStatusAfterTexts(string text) =>
        DispatcherQueue.TryEnqueue(() =>
        {
            SaveStatusText.Text = text;
            RefreshSetupCards();
        });

    private async void OnClearHistoryButtonClick(object sender, RoutedEventArgs e)
    {
        var historyStore = App.Services.GetRequiredService<IHistoryStore>();
        await historyStore.ClearAsync();
        _history = [];
        ShowHistory();
    }

    private sealed record HistoryDisplayItem(string Text, string Subtitle);
}
