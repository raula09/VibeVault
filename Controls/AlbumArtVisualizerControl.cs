using System.Text;
using Tessera;
using Tessera.Components.Primitives;
using Tessera.Controls;
using Tessera.Styles;

namespace VibeVault;

internal enum VisualRenderMode
{
    Ascii,
    Image
}

internal sealed class AlbumArtVisualizerControl : Control
{
    private const string AsciiPalette = " .'`^\",:;Il!i~+_-?][}{1)(|/tfjrxnuvczXYUJCLQ0OZmwqpdbkhao*#MW&8%B@$";
    private const string AsciiEdgePalette = " .,:;irsXA253hMHGS#9B&@";
    private const string UnicodePalette = " ·░▒▓█";

    public string Title { get; set; } = "Cover Visual · v exits";
    public BorderStyle Border { get; set; } = BorderStyle.Rounded;
    public Thickness Padding { get; set; } = Thickness.All(0);

    public AlbumArtFrame? CoverArt { get; set; }
    public string TrackTitle { get; set; } = "—";
    public string ArtistAlbumLine { get; set; } = "—";
    public string TimingLine { get; set; } = "00:00 / 00:00";
    public string MetaLine { get; set; } = "volume 0";
    public double Loudness { get; set; }
    public long AnimationFrame { get; set; }
    public VisualRenderMode RenderMode { get; set; } = VisualRenderMode.Ascii;
    public string EmptyMessage { get; set; } = "No embedded cover art in this MP3";
    public string FocusMarker { get; set; } = "✦";
    public bool UseAsciiGlyphs { get; set; }

    public TesseraStyle TitleStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FocusedTitleStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle BorderStyleText { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FocusedBorderStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle InfoStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle HintStyle { get; set; } = TesseraStyle.Empty;

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

        var infoRows = Math.Clamp(content.Height / 4, 6, 8);
        var artHeight = Math.Max(1, content.Height - infoRows);
        var (ax, ay, aw, ah) = ComputeArtViewport(content.X, content.Y, content.Width, artHeight);
        RenderArt(canvas, ax, ay, aw, ah);
        RenderInfo(canvas, content.X, content.Y + artHeight, content.Width, content.Bottom - (content.Y + artHeight));
    }

    private void RenderArt(Canvas canvas, int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0) return;
        if (CoverArt is null)
        {
            var msg = Fit(EmptyMessage, width);
            var mx = x + Math.Max(0, (width - msg.Length) / 2);
            var my = y + (height / 2);
            canvas.WriteText(mx, my, Styled(HintStyle.IsEmpty ? InfoStyle : HintStyle, msg), width - (mx - x));
            return;
        }

        if (RenderMode == VisualRenderMode.Image)
        {
            RenderImageArt(canvas, x, y, width, height, CoverArt, AnimationFrame, Loudness);
            return;
        }

        RenderAsciiArt(canvas, x, y, width, height, CoverArt, AnimationFrame, Loudness, UseAsciiGlyphs);
    }

    private static void RenderAsciiArt(
        Canvas canvas,
        int x,
        int y,
        int width,
        int height,
        AlbumArtFrame frame,
        long animationFrame,
        double loudness,
        bool useAsciiGlyphs)
    {
        var palette = useAsciiGlyphs ? AsciiPalette : UnicodePalette;
        var t = animationFrame * 0.095;
        var intensity = 0.95 + (Math.Clamp(loudness, 0, 1) * 0.7);
        var amp = 0.01 * intensity;

        for (var row = 0; row < height; row++)
        {
            var fy = row / (double)Math.Max(1, height - 1);
            var line = new StringBuilder(width * 20);
            for (var col = 0; col < width; col++)
            {
                var fx = col / (double)Math.Max(1, width - 1);

                var dx =
                    (Math.Sin((fy * 15.0) + (t * 1.3)) * amp) +
                    (Math.Sin((fx * 8.0) + (t * 0.47)) * (amp * 0.55));
                var dy =
                    (Math.Cos((fx * 13.0) + (t * 1.1)) * (amp * 0.85)) +
                    (Math.Sin((fy * 7.0) + (t * 0.63)) * (amp * 0.45));

                var nx = Math.Clamp(fx + dx, 0, 1);
                var ny = Math.Clamp(fy + dy, 0, 1);

                var (r0, g0, b0) = SampleEnhancedColor(frame, nx - 0.0025, ny - 0.0025, intensity);
                var (r1, g1, b1) = SampleEnhancedColor(frame, nx + 0.0025, ny - 0.0025, intensity);
                var (r2, g2, b2) = SampleEnhancedColor(frame, nx - 0.0025, ny + 0.0025, intensity);
                var (r3, g3, b3) = SampleEnhancedColor(frame, nx + 0.0025, ny + 0.0025, intensity);
                var r = (r0 + r1 + r2 + r3) >> 2;
                var g = (g0 + g1 + g2 + g3) >> 2;
                var b = (b0 + b1 + b2 + b3) >> 2;

                var lum = ((0.2126 * r) + (0.7152 * g) + (0.0722 * b)) / 255.0;
                var vignetteX = (fx - 0.5) * (fx - 0.5);
                var vignetteY = (fy - 0.5) * (fy - 0.5);
                var vignette = 1.0 - Math.Clamp((vignetteX * 1.5) + (vignetteY * 1.9), 0, 0.72);
                var scanline = 0.92 + (0.08 * Math.Sin((row * 0.9) + (t * 2.2)));
                lum = Math.Clamp(lum * vignette * scanline, 0, 1);

                var edge = EstimateEdge(frame, nx, ny);
                var dither = BayerDither(row, col) * 0.08;
                var tone = Math.Clamp(lum + dither + (edge * 0.26), 0, 1);

                var glyphSet = useAsciiGlyphs && edge > 0.22 ? AsciiEdgePalette : palette;
                var glyphIndex = Math.Clamp((int)Math.Round(tone * (glyphSet.Length - 1)), 0, glyphSet.Length - 1);
                var glyph = glyphSet[glyphIndex];

                line.Append($"\u001b[38;2;{r};{g};{b}m{glyph}");
            }

            line.Append("\u001b[0m");
            canvas.WriteText(x, y + row, line.ToString(), width);
        }
    }

    private static void RenderImageArt(
        Canvas canvas,
        int x,
        int y,
        int width,
        int height,
        AlbumArtFrame frame,
        long animationFrame,
        double loudness)
    {
        var t = animationFrame * 0.03;
        var pulse = 1.0 + (Math.Clamp(loudness, 0, 1) * 0.025);
        var wobbleX = 0.006 * Math.Sin(t * 2.1);
        var wobbleY = 0.006 * Math.Cos(t * 1.7);

        for (var row = 0; row < height; row++)
        {
            var topYNorm = ((row * 2) / (double)Math.Max(1, (height * 2) - 1));
            var botYNorm = (((row * 2) + 1) / (double)Math.Max(1, (height * 2) - 1));
            var topNy = Math.Clamp(((topYNorm - 0.5) / pulse) + 0.5 + wobbleY, 0, 1);
            var botNy = Math.Clamp(((botYNorm - 0.5) / pulse) + 0.5 + wobbleY, 0, 1);

            var line = new StringBuilder(width * 40);
            for (var col = 0; col < width; col++)
            {
                var fx = col / (double)Math.Max(1, width - 1);
                var nx = Math.Clamp(((fx - 0.5) / pulse) + 0.5 + wobbleX, 0, 1);

                var (tr, tg, tb) = SampleEnhancedColor(frame, nx, topNy, 1.02);
                var (br, bg, bb) = SampleEnhancedColor(frame, nx, botNy, 1.02);

                line.Append($"\u001b[38;2;{tr};{tg};{tb}m\u001b[48;2;{br};{bg};{bb}m▀");
            }

            line.Append("\u001b[0m");
            canvas.WriteText(x, y + row, line.ToString(), width);
        }
    }

    private void RenderInfo(Canvas canvas, int x, int y, int width, int height)
    {
        if (height <= 0 || width <= 0) return;

        var energy = Math.Clamp(Loudness, 0, 1);
        var rows = new List<string>
        {
            Fit(TrackTitle, width),
            Fit(ArtistAlbumLine, width),
            Fit(TimingLine, width),
            Fit(MetaLine, width),
            Fit(BuildEnergyLine(energy, width), width),
            Fit(
                UseAsciiGlyphs
                    ? $"i switch render ({RenderMode}) | v exit | space play/pause | n/p next-prev"
                    : $"i switch render ({RenderMode}) · v exit · space play/pause · n/p next-prev",
                width)
        };

        for (var i = 0; i < height && i < rows.Count; i++)
        {
            var style = i == rows.Count - 1 && !HintStyle.IsEmpty ? HintStyle : InfoStyle;
            canvas.WriteText(x, y + i, Styled(style, rows[i]), width);
        }
    }

    private static string Fit(string text, int width)
    {
        if (width <= 0) return string.Empty;
        if (string.IsNullOrEmpty(text)) return string.Empty.PadRight(width);
        if (text.Length == width) return text;
        if (text.Length > width) return width == 1 ? "." : text[..(width - 1)] + ".";
        return text.PadRight(width);
    }

    private static void ClearContent(Canvas canvas, Rect content)
    {
        var blank = new string(' ', content.Width);
        for (var row = 0; row < content.Height; row++)
            canvas.WriteText(content.X, content.Y + row, blank, content.Width);
    }

    private static string Styled(TesseraStyle style, string text) =>
        style.IsEmpty || string.IsNullOrEmpty(text) ? text : style.Render(text);

    private static (int X, int Y, int Width, int Height) ComputeArtViewport(int x, int y, int width, int height)
    {
        if (width <= 0 || height <= 0) return (x, y, Math.Max(1, width), Math.Max(1, height));

        var targetWidth = Math.Clamp((int)Math.Round(width * 0.7), 24, Math.Max(24, width - 4));
        var targetHeight = Math.Clamp((int)Math.Round(height * 0.8), 10, Math.Max(10, height - 1));
        var aspectLimitedHeight = Math.Clamp((int)Math.Round(targetWidth * 0.56), 8, height);
        targetHeight = Math.Min(targetHeight, aspectLimitedHeight);

        var offsetX = (width - targetWidth) / 2;
        var offsetY = (height - targetHeight) / 2;
        return (x + offsetX, y + offsetY, targetWidth, targetHeight);
    }

    private static (int R, int G, int B) Enhance(byte r, byte g, byte b, double intensity)
    {
        var rf = r / 255.0;
        var gf = g / 255.0;
        var bf = b / 255.0;

        var gray = (rf + gf + bf) / 3.0;
        var saturation = 1.55 + (0.15 * (intensity - 1.0));
        rf = gray + ((rf - gray) * saturation);
        gf = gray + ((gf - gray) * saturation);
        bf = gray + ((bf - gray) * saturation);

        var contrast = 1.32;
        rf = ((rf - 0.5) * contrast) + 0.5;
        gf = ((gf - 0.5) * contrast) + 0.5;
        bf = ((bf - 0.5) * contrast) + 0.5;

        var brightness = 1.2 + (0.2 * (intensity - 1.0));
        rf *= brightness;
        gf *= brightness;
        bf *= brightness;
        rf = Math.Pow(Math.Clamp(rf, 0, 1), 0.82);
        gf = Math.Pow(Math.Clamp(gf, 0, 1), 0.82);
        bf = Math.Pow(Math.Clamp(bf, 0, 1), 0.82);

        return (
            (int)Math.Round(Math.Clamp(rf, 0, 1) * 255),
            (int)Math.Round(Math.Clamp(gf, 0, 1) * 255),
            (int)Math.Round(Math.Clamp(bf, 0, 1) * 255));
    }

    private string BuildEnergyLine(double loudness, int width)
    {
        var barWidth = Math.Clamp(width - 11, 6, 40);
        var fill = (int)Math.Round(loudness * barWidth);
        var fullGlyph = UseAsciiGlyphs ? '#' : '█';
        var emptyGlyph = UseAsciiGlyphs ? '-' : '░';
        var label = $"Energy [{new string(fullGlyph, fill)}{new string(emptyGlyph, Math.Max(0, barWidth - fill))}]";
        return label;
    }

    private static double EstimateEdge(AlbumArtFrame frame, int sx, int sy)
    {
        var lx = Math.Max(0, sx - 1);
        var rx = Math.Min(frame.Width - 1, sx + 1);
        var uy = Math.Max(0, sy - 1);
        var dy = Math.Min(frame.Height - 1, sy + 1);

        var left = SampleLuma(frame, lx, sy);
        var right = SampleLuma(frame, rx, sy);
        var up = SampleLuma(frame, sx, uy);
        var down = SampleLuma(frame, sx, dy);

        return Math.Clamp((Math.Abs(right - left) + Math.Abs(down - up)) * 0.5, 0, 1);
    }

    private static double EstimateEdge(AlbumArtFrame frame, double nx, double ny)
    {
        var stepX = 1.0 / Math.Max(1, frame.Width - 1);
        var stepY = 1.0 / Math.Max(1, frame.Height - 1);
        var left = SampleLuma(frame, Math.Clamp(nx - stepX, 0, 1), ny);
        var right = SampleLuma(frame, Math.Clamp(nx + stepX, 0, 1), ny);
        var up = SampleLuma(frame, nx, Math.Clamp(ny - stepY, 0, 1));
        var down = SampleLuma(frame, nx, Math.Clamp(ny + stepY, 0, 1));
        return Math.Clamp((Math.Abs(right - left) + Math.Abs(down - up)) * 0.5, 0, 1);
    }

    private static double SampleLuma(AlbumArtFrame frame, int sx, int sy)
    {
        var idx = ((sy * frame.Width) + sx) * 3;
        var r = frame.Rgb24[idx] / 255.0;
        var g = frame.Rgb24[idx + 1] / 255.0;
        var b = frame.Rgb24[idx + 2] / 255.0;
        return (0.2126 * r) + (0.7152 * g) + (0.0722 * b);
    }

    private static double SampleLuma(AlbumArtFrame frame, double nx, double ny)
    {
        var (r, g, b) = SampleRgbBilinear(frame, nx, ny);
        return ((0.2126 * r) + (0.7152 * g) + (0.0722 * b)) / 255.0;
    }

    private static (int R, int G, int B) SampleEnhancedColor(AlbumArtFrame frame, double nx, double ny, double intensity)
    {
        var (r, g, b) = SampleRgbBilinear(frame, nx, ny);
        return Enhance((byte)r, (byte)g, (byte)b, intensity);
    }

    private static (int R, int G, int B) SampleRgbBilinear(AlbumArtFrame frame, double nx, double ny)
    {
        var x = Math.Clamp(nx, 0, 1) * (frame.Width - 1);
        var y = Math.Clamp(ny, 0, 1) * (frame.Height - 1);
        var x0 = (int)Math.Floor(x);
        var y0 = (int)Math.Floor(y);
        var x1 = Math.Min(frame.Width - 1, x0 + 1);
        var y1 = Math.Min(frame.Height - 1, y0 + 1);
        var tx = x - x0;
        var ty = y - y0;

        var i00 = ((y0 * frame.Width) + x0) * 3;
        var i10 = ((y0 * frame.Width) + x1) * 3;
        var i01 = ((y1 * frame.Width) + x0) * 3;
        var i11 = ((y1 * frame.Width) + x1) * 3;

        static double Lerp(double a, double b, double t) => a + ((b - a) * t);

        var r0 = Lerp(frame.Rgb24[i00], frame.Rgb24[i10], tx);
        var g0 = Lerp(frame.Rgb24[i00 + 1], frame.Rgb24[i10 + 1], tx);
        var b0 = Lerp(frame.Rgb24[i00 + 2], frame.Rgb24[i10 + 2], tx);
        var r1 = Lerp(frame.Rgb24[i01], frame.Rgb24[i11], tx);
        var g1 = Lerp(frame.Rgb24[i01 + 1], frame.Rgb24[i11 + 1], tx);
        var b1 = Lerp(frame.Rgb24[i01 + 2], frame.Rgb24[i11 + 2], tx);

        var r = (int)Math.Round(Lerp(r0, r1, ty));
        var g = (int)Math.Round(Lerp(g0, g1, ty));
        var b = (int)Math.Round(Lerp(b0, b1, ty));
        return (Math.Clamp(r, 0, 255), Math.Clamp(g, 0, 255), Math.Clamp(b, 0, 255));
    }

    private static double BayerDither(int row, int col)
    {
        return ((row & 3), (col & 3)) switch
        {
            (0, 0) => -0.46875, (0, 1) => 0.03125,  (0, 2) => -0.34375, (0, 3) => 0.15625,
            (1, 0) => 0.28125,  (1, 1) => -0.21875, (1, 2) => 0.40625,  (1, 3) => -0.09375,
            (2, 0) => -0.28125, (2, 1) => 0.21875,  (2, 2) => -0.40625, (2, 3) => 0.09375,
            (3, 0) => 0.46875,  (3, 1) => -0.03125, (3, 2) => 0.34375,  _ => -0.15625
        };
    }
}
