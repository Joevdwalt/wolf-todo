using Spectre.Console;
using Spectre.Console.Rendering;

namespace Wolf.Controls.Rendering;

/// <summary>Places renderables within a canvas without changing the underlying content flow.</summary>
public sealed class OverlayLayout(int width, int height, IEnumerable<ControlLayer> layers) : IRenderable
{
    public Measurement Measure(RenderOptions options, int maxWidth) => new(Math.Min(width, maxWidth), Math.Min(width, maxWidth));
    public IEnumerable<Segment> Render(RenderOptions options, int maxWidth) =>
        new LayeredRenderable(width, height, layers).Render(options, maxWidth);

    public static ControlLayer Place(IRenderable content, int canvasWidth, int canvasHeight,
        OverlayAlignment alignment, int? width = null, int? height = null,
        int top = 0, int bottom = 0, int left = 0, int right = 0, int zIndex = 1)
    {
        var w = Math.Clamp(width ?? canvasWidth - left - right, 1, Math.Max(1, canvasWidth - left - right));
        var h = Math.Clamp(height ?? canvasHeight - top - bottom, 1, Math.Max(1, canvasHeight - top - bottom));
        var x = alignment switch { OverlayAlignment.TopRight or OverlayAlignment.Right or OverlayAlignment.BottomRight => canvasWidth - right - w,
            OverlayAlignment.Top or OverlayAlignment.Center or OverlayAlignment.Bottom => (canvasWidth - w) / 2,
            _ => left };
        var y = alignment switch { OverlayAlignment.BottomLeft or OverlayAlignment.Bottom or OverlayAlignment.BottomRight => canvasHeight - bottom - h,
            OverlayAlignment.Left or OverlayAlignment.Center or OverlayAlignment.Right => (canvasHeight - h) / 2,
            _ => top };
        return new ControlLayer(new HeightClipRenderable(content, h), Math.Max(0, x), Math.Max(0, y), zIndex, w);
    }
}
