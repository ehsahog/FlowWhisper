using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Windows.Graphics;

namespace FlowWhisper_App;

/// <summary>
/// Borderless, always-on-top overlay shown while push-to-talk is held: a
/// rolling waveform confirms that audio is actually coming in, and the text
/// row shows what Whisper hears so far, then the final text.
/// </summary>
public sealed partial class FlowBarWindow : Window
{
    private const int WindowWidth = 520;
    private const int WindowHeight = 104;
    private const int BarCount = 44;
    private const double BarWidth = 3;
    private const double BarGap = 2;
    private const int MaxTranscriptChars = 150;

    private readonly Rectangle[] _bars = new Rectangle[BarCount];
    private readonly float[] _levels = new float[BarCount];
    private int _head;

    public FlowBarWindow()
    {
        InitializeComponent();
        ConfigureWindow();
        SystemBackdrop = new GlassBackdrop(dark: Application.Current.RequestedTheme == ApplicationTheme.Dark);
        CreateBars();
    }

    private void ConfigureWindow()
    {
        AppWindow.Resize(new SizeInt32(WindowWidth, WindowHeight));
        AppWindow.IsShownInSwitchers = false;

        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.SetBorderAndTitleBar(hasBorder: false, hasTitleBar: false);
            presenter.IsResizable = false;
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        // Borderless windows lose Windows 11's rounded corners unless asked for explicitly.
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var preference = DwmWindowCornerPreferenceRound;
        DwmSetWindowAttribute(hwnd, DwmwaWindowCornerPreference, ref preference, sizeof(int));

        var work = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        AppWindow.Move(new PointInt32(
            work.X + (work.Width - WindowWidth) / 2,
            work.Y + work.Height - WindowHeight - 48));
    }

    private void CreateBars()
    {
        var brush = (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
        for (var i = 0; i < BarCount; i++)
        {
            var bar = new Rectangle { Width = BarWidth, RadiusX = 1.5, RadiusY = 1.5, Fill = brush };
            Canvas.SetLeft(bar, i * (BarWidth + BarGap));
            WaveformCanvas.Children.Add(bar);
            _bars[i] = bar;
        }

        ResetWaveform();
    }

    /// <summary>Pushes one new level (0..1 peak) onto the right edge of the waveform.</summary>
    public void PushLevel(float level)
    {
        _levels[_head] = level;
        _head = (_head + 1) % BarCount;

        var height = WaveformCanvas.Height;
        for (var i = 0; i < BarCount; i++)
        {
            // sqrt lifts quiet speech (peaks ~0.05–0.3) into a clearly visible range.
            var value = Math.Clamp(Math.Sqrt(_levels[(_head + i) % BarCount]) * 1.4, 0.06, 1.0);
            var barHeight = value * height;
            _bars[i].Height = barHeight;
            Canvas.SetTop(_bars[i], (height - barHeight) / 2);
        }
    }

    public void ResetWaveform()
    {
        Array.Clear(_levels);
        PushLevel(0);
    }

    public void SetStatus(string text) => StatusText.Text = text;

    public void SetTransparency(int percent) => GlassStyle.Apply(this, TintLayers, percent);

    /// <summary>Shows the tail of the transcript so the newest words stay visible.</summary>
    public void SetTranscript(string text) =>
        TranscriptText.Text = text.Length <= MaxTranscriptChars ? text : "…" + text[^MaxTranscriptChars..];

    public void ShowBar()
    {
        // Show(false) = don't activate: the bar must never steal keyboard
        // focus from whatever app the user is dictating into, or SendInput
        // would end up typing into the FlowBar instead of the target app.
        AppWindow.Show(activateWindow: false);
    }

    public void HideBar() => AppWindow.Hide();

    private const int DwmwaWindowCornerPreference = 33;
    private const int DwmWindowCornerPreferenceRound = 2;

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);
}
