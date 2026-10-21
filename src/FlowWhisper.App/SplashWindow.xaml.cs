using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Graphics;

namespace FlowWhisper_App;

/// <summary>
/// Shown while the app starts so it's visible that it did: the app otherwise lives only in
/// the tray. Closes itself once speech recognition is loaded, or after a while if loading
/// takes long (the first load on a new device compiles the model for minutes).
/// </summary>
public sealed partial class SplashWindow : Window
{
    private const int WindowWidth = 380;
    private const int WindowHeight = 220;

    public SplashWindow()
    {
        InitializeComponent();
        SystemBackdrop = new GlassBackdrop(dark: Application.Current.RequestedTheme == ApplicationTheme.Dark);
        AppIconImage.Source = new BitmapImage(new Uri(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png")));

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

        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        var preference = 2; // DWMWCP_ROUND
        DwmSetWindowAttribute(hwnd, 33 /* DWMWA_WINDOW_CORNER_PREFERENCE */, ref preference, sizeof(int));
        var noBorder = unchecked((int)0xFFFFFFFE); // DWMWA_COLOR_NONE: no Windows 11 window outline
        DwmSetWindowAttribute(hwnd, 34 /* DWMWA_BORDER_COLOR */, ref noBorder, sizeof(int));

        var work = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary).WorkArea;
        AppWindow.Move(new PointInt32(
            work.X + (work.Width - WindowWidth) / 2,
            work.Y + (work.Height - WindowHeight) / 2));
    }

    public void SetTransparency(int percent) => GlassStyle.Apply(this, TintLayers, percent);

    public void ShowSplash(string status)
    {
        StatusText.Text = status;
        AppWindow.Show(activateWindow: false);
    }

    /// <summary>
    /// Kept in a field: a DispatcherQueueTimer only referenced by a local can be garbage
    /// collected before it fires, which left the splash on screen for good (seen in real use,
    /// where loading the speech model triggers collections; never in short test runs).
    /// </summary>
    private Microsoft.UI.Dispatching.DispatcherQueueTimer? _closeTimer;

    /// <summary>Shows a final message, stops the progress bar and closes after a moment.</summary>
    public void Finish(string status, TimeSpan after)
    {
        StatusText.Text = status;
        LoadingBar.Visibility = Visibility.Collapsed;
        _closeTimer = DispatcherQueue.CreateTimer();
        _closeTimer.Interval = after;
        _closeTimer.IsRepeating = false;
        _closeTimer.Tick += (_, _) =>
        {
            _closeTimer?.Stop();
            Close();
        };
        _closeTimer.Start();
    }

    [LibraryImport("dwmapi.dll")]
    private static partial int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);
}
