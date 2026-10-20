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
    private const string InstanceMutexName = @"Local\FlowWhisper.SingleInstance";
    private const string ShowSettingsEventName = @"Local\FlowWhisper.ShowSettings";

    /// <summary>Held for the process lifetime; its existence marks the running instance.</summary>
    private static Mutex? _instanceMutex;

    /// <summary>Signalled by a second launch to ask the running instance to open Settings.</summary>
    public static EventWaitHandle ShowSettingsRequested { get; } =
        new(false, EventResetMode.AutoReset, ShowSettingsEventName);

    [STAThread]
    private static void Main(string[] args)
    {
        VelopackApp.Build()
            // Don't leave a "start at login" entry pointing at a deleted exe.
            .OnBeforeUninstallFastCallback(_ => StartupManager.SetEnabled(false))
            .Run();

        // Two instances would both hook the dictation key, record and paste at the same
        // time. Launching again (Start menu, installer) instead shows the running one.
        _instanceMutex = new Mutex(initiallyOwned: true, InstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            ShowSettingsRequested.Set();
            return;
        }

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
