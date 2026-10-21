using Microsoft.UI.Xaml;

namespace FlowWhisper_App;

/// <summary>Applies the user's glass transparency setting to a window built from GlassBackdrop + tint layers.</summary>
internal static class GlassStyle
{
    /// <param name="percent">0 = the default frosted glass, 100 = as see-through as it gets.</param>
    public static void Apply(Window window, UIElement tintLayers, int percent)
    {
        var t = Math.Clamp(percent, 0, 100) / 100.0;
        if (window.SystemBackdrop is GlassBackdrop glass)
        {
            glass.Transparency = t;
        }

        // Keep a trace of the tint so the edge still reads as glass at 100 %.
        tintLayers.Opacity = 1 - 0.85 * t;
    }
}
