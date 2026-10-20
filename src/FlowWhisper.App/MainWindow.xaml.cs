using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace FlowWhisper_App;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    /// <summary>Set when the app exits; until then closing the window just hides it.</summary>
    public bool AllowClose { get; set; }

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        // The close button only hides the window: the app keeps living in the tray, and
        // this window's HWND is the clipboard owner for pasting, so it must stay alive.
        AppWindow.Closing += (sender, args) =>
        {
            if (!AllowClose)
            {
                args.Cancel = true;
                sender.Hide();
            }
        };

        // An absolute file URI: ms-appx:/// paths didn't resolve in the unpackaged app.
        AppTitleBar.IconSource = new ImageIconSource
        {
            ImageSource = new BitmapImage(new Uri(Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.png"))),
        };

        // Navigate the root frame to the main page on startup.
        RootFrame.Navigate(typeof(MainPage));
    }
}
