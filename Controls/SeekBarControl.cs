using Tessera;
using Tessera.Components.Primitives;
using Tessera.Controls;
using Tessera.Styles;

namespace VibeVault;

internal sealed class SeekBarControl : Control
{
    private Rect _lastBarRect;

    public string Title { get; set; } = "Timeline";
    public BorderStyle Border { get; set; } = BorderStyle.Rounded;
    public Thickness Padding { get; set; } = Thickness.All(0);

    public int CurrentSeconds { get; set; }
    public int TotalSeconds { get; set; }
    public bool IsPlaying { get; set; }
    public string LeftTime { get; set; } = "00:00";
    public string RightTime { get; set; } = "00:00";
    public int VolumePercent { get; set; } = 70;

    public TesseraStyle TitleStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FocusedTitleStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle BorderStyleText { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FocusedBorderStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FillStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle TrackStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle LabelStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle KnobStyle { get; set; } = TesseraStyle.Empty;
    public string FocusMarker { get; set; } = "✦";
    public bool UseAsciiGlyphs { get; set; }

    public bool TryGetRatioFromPoint(int x, int y, out double ratio)
    {
        ratio = 0;
        if (_lastBarRect.IsEmpty) return false;
        if (Math.Abs(y - _lastBarRect.Y) > 1) return false;
        if (x < _lastBarRect.X || x >= _lastBarRect.Right) return false;

        ratio = (x - _lastBarRect.X) / (double)Math.Max(1, _lastBarRect.Width - 1);
        ratio = Math.Clamp(ratio, 0, 1);
        return true;
    }

    public override void Render(Canvas canvas, Rect rect)
    {
        _lastBarRect = default;
        var clipped = Rect.Intersect(rect, canvas.Bounds);
        if (clipped.IsEmpty) return;

        var title = IsFocused
            ? Styled(FocusedTitleStyle, $"{Title} {FocusMarker}")
            : Styled(TitleStyle, Title);
        var border = IsFocused ? BorderStyleText.Merge(FocusedBorderStyle) : BorderStyleText;
        canvas.DrawBox(clipped, title, Border, border);

        var content = clipped.Inset(1, 1).Inset(Padding);
        if (content.IsEmpty) return;
        ClearContent(canvas, content);

        ClearContent(canvas, content);

        var ratio = TotalSeconds > 0 ? Math.Clamp(CurrentSeconds / (double)TotalSeconds, 0, 1) : 0;
        var status = IsPlaying
            ? (UseAsciiGlyphs ? "> LIVE" : "▶ LIVE")
            : (UseAsciiGlyphs ? "|| HOLD" : "▌▌ HOLD");
        var vol = Math.Clamp(VolumePercent, 0, 100);
        var percent = (int)Math.Round(ratio * 100);
        var leftChip = $"[{status}]";
        var middle = $"{LeftTime} / {RightTime}  {percent:000}%";
        var rightChip = $"VOL {BuildVolumeMeter(vol, 6)} {vol:000}%";

        if (content.Width < 46)
        {
            var compact = $"{leftChip} {LeftTime}/{RightTime} {percent:000}%";
            canvas.WriteText(content.X, content.Y, Styled(LabelStyle, Fit(compact, content.Width)), content.Width);
        }
        else
        {
            canvas.WriteText(content.X, content.Y, Styled(KnobStyle.IsEmpty ? LabelStyle : KnobStyle, leftChip), content.Width);
            var middleX = content.X + Math.Max(0, (content.Width - middle.Length) / 2);
            canvas.WriteText(middleX, content.Y, Styled(LabelStyle, middle), Math.Max(0, content.Right - middleX));

            var rightX = Math.Max(content.X, content.Right - rightChip.Length);
            if (rightX > content.X + leftChip.Length + 1)
                canvas.WriteText(rightX, content.Y, Styled(TrackStyle.IsEmpty ? LabelStyle : TrackStyle, rightChip), content.Right - rightX);
        }

        if (content.Height < 2) return;

        _lastBarRect = new Rect(content.X, content.Y + 1, content.Width, 1);
        var width = _lastBarRect.Width;
        var fill = (int)Math.Round(ratio * Math.Max(0, width - 1));

        if (width <= 0) return;
        var head = Math.Clamp(fill, 0, width - 1);
        var leftText = BuildRail(Math.Clamp(head, 0, width), true);
        var rightText = BuildRail(Math.Clamp(width - head - 1, 0, width), false);
        var knob = IsPlaying
            ? (UseAsciiGlyphs ? "*" : "◆")
            : (UseAsciiGlyphs ? "o" : "◇");

        if (!string.IsNullOrEmpty(leftText))
            canvas.WriteText(_lastBarRect.X, _lastBarRect.Y, Styled(FillStyle, leftText), leftText.Length);
        canvas.WriteText(_lastBarRect.X + head, _lastBarRect.Y, Styled(KnobStyle.IsEmpty ? LabelStyle : KnobStyle, knob), 1);
        if (!string.IsNullOrEmpty(rightText))
            canvas.WriteText(_lastBarRect.X + head + 1, _lastBarRect.Y, Styled(TrackStyle, rightText), rightText.Length);
    }

    private string BuildVolumeMeter(int volumePercent, int bars)
    {
        var fill = (int)Math.Round((Math.Clamp(volumePercent, 0, 100) / 100.0) * bars);
        return "[" + new string(UseAsciiGlyphs ? '#' : '▮', Math.Clamp(fill, 0, bars))
            + new string(UseAsciiGlyphs ? '-' : '▯', Math.Clamp(bars - fill, 0, bars))
            + "]";
    }

    private string BuildRail(int length, bool filled)
    {
        if (length <= 0) return string.Empty;
        var pattern = UseAsciiGlyphs ? (filled ? "##" : "--") : (filled ? "█▓" : "─┄");
        var chars = new char[length];
        for (var i = 0; i < length; i++)
            chars[i] = pattern[i % pattern.Length];
        return new string(chars);
    }

    private static string Fit(string text, int width)
    {
        if (width <= 0) return string.Empty;
        if (text.Length <= width) return text;
        return width == 1 ? "." : text[..(width - 1)] + ".";
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
