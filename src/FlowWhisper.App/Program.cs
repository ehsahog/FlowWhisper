using System.Runtime.InteropServices;
using FlowWhisper.Core.Startup;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Velopack;

namespace FlowWhisper_App;

/// <summary>
/// Replaces the XAML-generated Main (DISABLE_XAML_GENERATED_MAIN) so the Velopack
/// installer's hooks run first: on install/uninstall/update Velopack starts the exe with
/// special arguments, handles them here and exits before any window is created.
/// </summary>
public static partial class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build()
            // Don't leave a "start at login" entry pointing at a deleted exe.
            .OnBeforeUninstallFastCallback(_ => StartupManager.SetEnabled(false))
            .Run();

        XamlCheckProcessRequirements();
        WinRT.ComWrappersSupport.InitializeComWrappers();
        Application.Start(p =>
        {
            var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
            SynchronizationContext.SetSynchronizationContext(context);
            _ = new App();
        });
    }

    [LibraryImport("Microsoft.ui.xaml.dll")]
    private static partial void XamlCheckProcessRequirements();
}
