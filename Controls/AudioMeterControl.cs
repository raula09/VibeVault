using Tessera;
using Tessera.Components.Primitives;
using Tessera.Controls;
using Tessera.Styles;

namespace VibeVault;

internal sealed class AudioMeterControl : Control
{
    private static readonly char[] LevelChars = "▁▂▃▄▅▆▇█".ToCharArray();
    private readonly List<double> _smoothed = [];

    public string Title { get; set; } = "Audio Meter";
    public BorderStyle Border { get; set; } = BorderStyle.Rounded;
    public Thickness Padding { get; set; } = Thickness.All(0);
    public string FocusMarker { get; set; } = "✦";
    public string Levels { get; set; } = string.Empty;
    public double OverallLevel { get; set; }
    public bool UseAsciiGlyphs { get; set; }

    public TesseraStyle TitleStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FocusedTitleStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle BorderStyleText { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FocusedBorderStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle TopBarStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle BottomBarStyle { get; set; } = TesseraStyle.Empty;

    public override void Render(Canvas canvas, Rect rect)
    {
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
        if (content.Height < 3) return;

        ClearContent(canvas, content);
        var baselineY = content.Bottom - 1;
        var barHeight = Math.Max(2, content.Height - 1);
        var bars = Math.Max(1, Math.Min((content.Width + 1) / 2, 96));
        var levels = BuildLevels(bars);

        var usedWidth = (bars * 2) - 1;
        var startX = content.X + Math.Max(0, (content.Width - usedWidth) / 2);

        canvas.WriteText(
            content.X,
            baselineY,
            Styled(BottomBarStyle.IsEmpty ? TopBarStyle : BottomBarStyle, new string(UseAsciiGlyphs ? '-' : '─', content.Width)),
            content.Width);

        for (var i = 0; i < bars; i++)
        {
            var x = startX + (i * 2);
            if (x >= content.Right) break;

            var level = levels[i];
            var normalized = level / 7.0;
            var loudnessBoost = 0.22 + (OverallLevel * 0.95);
            var cells = Math.Clamp((int)Math.Round((normalized * loudnessBoost) * barHeight), 0, barHeight);
            if (cells == 0) continue;
            for (var h = 0; h < cells; h++)
            {
                var y = baselineY - 1 - h;
                if (y < content.Y) break;
                canvas.WriteText(x, y, Styled(TopBarStyle, UseAsciiGlyphs ? "#" : "█"), 1);
            }
        }
    }

    private int[] BuildLevels(int bars)
    {
        var raw = ParseRawLevels();
        EnsureSmoothCapacity(bars);

        var result = new int[bars];
        for (var i = 0; i < bars; i++)
        {
            var sampleIndex = (int)Math.Round((i / (double)Math.Max(1, bars - 1)) * Math.Max(0, raw.Count - 1));
            var target = raw.Count == 0 ? 0.0 : raw[Math.Clamp(sampleIndex, 0, raw.Count - 1)];
            target = Math.Pow(Math.Clamp(target / 7.0, 0, 1), 0.82) * 7.0;
            target = Math.Clamp(target * (0.30 + (OverallLevel * 0.80)), 0, 7);
            var previous = _smoothed[i];
            var blended = target >= previous
                ? previous + ((target - previous) * 0.74)
                : previous + ((target - previous) * 0.42);

            _smoothed[i] = blended;
            result[i] = (int)Math.Round(Math.Clamp(blended, 0, 7));
        }

        return result;
    }

    private List<int> ParseRawLevels()
    {
        var parsed = new List<int>(Levels.Length);
        foreach (var ch in Levels)
        {
            var idx = Array.IndexOf(LevelChars, ch);
            if (idx >= 0) parsed.Add(idx);
        }

        if (parsed.Count == 0)
            parsed.Add(0);

        return parsed;
    }

    private void EnsureSmoothCapacity(int bars)
    {
        if (_smoothed.Count == bars) return;
        if (_smoothed.Count > bars)
        {
            _smoothed.RemoveRange(bars, _smoothed.Count - bars);
            return;
        }

        while (_smoothed.Count < bars)
            _smoothed.Add(0);
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
