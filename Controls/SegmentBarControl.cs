using Tessera;
using Tessera.Components.Primitives;
using Tessera.Controls;
using Tessera.Styles;

namespace VibeVault;

internal sealed class SegmentBarControl : Control
{
    public readonly record struct Segment(string Text, TesseraStyle Style);

    private IReadOnlyList<Segment> _segments = Array.Empty<Segment>();

    public string Title { get; set; } = string.Empty;
    public BorderStyle Border { get; set; } = BorderStyle.Rounded;
    public Thickness Padding { get; set; } = Thickness.All(1);

    public TesseraStyle TitleStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FocusedTitleStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle BorderStyleText { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FocusedBorderStyle { get; set; } = TesseraStyle.Empty;
    public string FocusMarker { get; set; } = "✦";

    public void SetSegments(IReadOnlyList<Segment> segments) =>
        _segments = segments ?? Array.Empty<Segment>();

    public override void Render(Canvas canvas, Rect rect)
    {
        var clipped = Rect.Intersect(rect, canvas.Bounds);
        if (clipped.IsEmpty) return;

        var titleText = IsFocused
            ? ControlCanvasHelpers.ApplyStyle(FocusedTitleStyle, $"{Title} {FocusMarker}")
            : ControlCanvasHelpers.ApplyStyle(TitleStyle, Title);
        var border = IsFocused ? BorderStyleText.Merge(FocusedBorderStyle) : BorderStyleText;
        canvas.DrawBox(clipped, titleText, Border, border);

        var content = clipped.Inset(1, 1).Inset(Padding);
        if (content.IsEmpty) return;
        ControlCanvasHelpers.ClearContent(canvas, content);
        if (_segments.Count == 0) return;

        var x = content.X;
        var y = content.Y;
        foreach (var segment in _segments)
        {
            if (x >= content.Right) break;
            var text = ControlCanvasHelpers.ApplyStyle(segment.Style, segment.Text);
            canvas.WriteText(x, y, text, content.Right - x);
            x += segment.Text.Length + 1;
        }
    }
}
