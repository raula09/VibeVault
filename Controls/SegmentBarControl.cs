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
            ? Styled(FocusedTitleStyle, $"{Title} {FocusMarker}")
            : Styled(TitleStyle, Title);
        var border = IsFocused ? BorderStyleText.Merge(FocusedBorderStyle) : BorderStyleText;
        canvas.DrawBox(clipped, titleText, Border, border);

        var content = clipped.Inset(1, 1).Inset(Padding);
        if (content.IsEmpty) return;
        ClearContent(canvas, content);
        if (_segments.Count == 0) return;

        ClearContent(canvas, content);

        var x = content.X;
        var y = content.Y;
        foreach (var segment in _segments)
        {
            if (x >= content.Right) break;
            var text = Styled(segment.Style, segment.Text);
            canvas.WriteText(x, y, text, content.Right - x);
            x += segment.Text.Length + 1;
        }
    }

    private static string Styled(TesseraStyle style, string text) =>
        style.IsEmpty || string.IsNullOrEmpty(text) ? text : style.Render(text);

    private static void ClearContent(Canvas canvas, Rect content)
    {
        var blank = new string(' ', content.Width);
        for (var row = 0; row < content.Height; row++)
            canvas.WriteText(content.X, content.Y + row, blank, content.Width);
    }
}
