using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace FlowWhisper_App;

/// <summary>
/// A clear, thin acrylic that always renders in its "active" look. Implemented as a
/// SystemBackdrop (rather than attaching a DesktopAcrylicController to the window
/// directly) so WinUI knows the window has a backdrop and leaves the area behind the
/// XAML content transparent; attached directly, WinUI painted an opaque background over it.
/// </summary>
public sealed partial class GlassBackdrop(bool dark) : SystemBackdrop
{
    private DesktopAcrylicController? _controller;
    private double _transparency;

    /// <summary>0 = the default frosted, tinted glass; 1 = as see-through as acrylic gets.</summary>
    public double Transparency
    {
        get => _transparency;
        set
        {
            _transparency = Math.Clamp(value, 0, 1);
            ApplyTransparency();
        }
    }

    protected override void OnTargetConnected(ICompositionSupportsSystemBackdrop connectedTarget, XamlRoot xamlRoot)
    {
        base.OnTargetConnected(connectedTarget, xamlRoot);
        if (!DesktopAcrylicController.IsSupported())
        {
            return;
        }

        _controller = new DesktopAcrylicController
        {
            Kind = DesktopAcrylicKind.Thin,
            // A cool blue tint so the glass has its own color over white apps; the XAML
            // layers on top add the sheen, rim and depth.
            TintColor = dark ? Color.FromArgb(255, 24, 30, 46) : Color.FromArgb(255, 214, 226, 248),
            // Shown when Windows disables translucency (Energy saver, transparency effects off).
            FallbackColor = dark ? Color.FromArgb(255, 36, 42, 58) : Color.FromArgb(255, 222, 231, 247),
        };
        ApplyTransparency();
        _controller.AddSystemBackdropTarget(connectedTarget);

        // Not GetDefaultSystemBackdropConfiguration(): that one tracks window activation, and
        // this window is never activated (it must not steal focus), so acrylic would always
        // show its flat inactive fallback.
        _controller.SetSystemBackdropConfiguration(new SystemBackdropConfiguration
        {
            IsInputActive = true,
            Theme = dark ? SystemBackdropTheme.Dark : SystemBackdropTheme.Light,
        });
    }

    private void ApplyTransparency()
    {
        if (_controller is null)
        {
            return;
        }

        // Luminosity is what makes acrylic look milky; at 0 only the blur and a hint of tint remain.
        var keep = (float)(1 - _transparency);
        _controller.TintOpacity = 0.18f * keep;
        _controller.LuminosityOpacity = (dark ? 0.30f : 0.35f) * keep;
    }

    protected override void OnTargetDisconnected(ICompositionSupportsSystemBackdrop disconnectedTarget)
    {
        base.OnTargetDisconnected(disconnectedTarget);
        _controller?.RemoveSystemBackdropTarget(disconnectedTarget);
        _controller?.Dispose();
        _controller = null;
    }
}
