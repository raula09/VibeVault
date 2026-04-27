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
    private const string Palette = " .'`^\",:;Il!i~+_-?][}{1)(|/tfjrxnuvczXYUJCLQ0OZmwqpdbkhao*#MW&8%B@$";

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

        var infoRows = Math.Min(5, Math.Max(3, content.Height / 5));
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
            RenderImageArt(canvas, x, y, width, height, CoverArt);
            return;
        }

        RenderAsciiArt(canvas, x, y, width, height, CoverArt, AnimationFrame, Loudness);
    }

    private static void RenderAsciiArt(
        Canvas canvas,
        int x,
        int y,
        int width,
        int height,
        AlbumArtFrame frame,
        long animationFrame,
        double loudness)
    {
        var t = animationFrame * 0.095;
        var intensity = 0.95 + (Math.Clamp(loudness, 0, 1) * 0.7);
        var amp = 0.012 * intensity;

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

                var sx = (int)Math.Round(Math.Clamp(fx + dx, 0, 1) * (frame.Width - 1));
                var sy = (int)Math.Round(Math.Clamp(fy + dy, 0, 1) * (frame.Height - 1));

                var idx = ((sy * frame.Width) + sx) * 3;
                var (r, g, b) = Enhance(frame.Rgb24[idx], frame.Rgb24[idx + 1], frame.Rgb24[idx + 2], intensity);

                var lum = ((0.2126 * r) + (0.7152 * g) + (0.0722 * b)) / 255.0;
                var glyphIndex = Math.Clamp((int)Math.Round(lum * (Palette.Length - 1)), 0, Palette.Length - 1);
                var glyph = Palette[glyphIndex];

                line.Append($"\u001b[38;2;{r};{g};{b}m{glyph}");
            }

            line.Append("\u001b[0m");
            canvas.WriteText(x, y + row, line.ToString(), width);
        }
    }

    private static void RenderImageArt(Canvas canvas, int x, int y, int width, int height, AlbumArtFrame frame)
    {
        for (var row = 0; row < height; row++)
        {
            var topYNorm = ((row * 2) / (double)Math.Max(1, (height * 2) - 1));
            var botYNorm = (((row * 2) + 1) / (double)Math.Max(1, (height * 2) - 1));
            var topSy = (int)Math.Round(topYNorm * (frame.Height - 1));
            var botSy = (int)Math.Round(botYNorm * (frame.Height - 1));

            var line = new StringBuilder(width * 40);
            for (var col = 0; col < width; col++)
            {
                var fx = col / (double)Math.Max(1, width - 1);
                var sx = (int)Math.Round(fx * (frame.Width - 1));

                var topIdx = ((topSy * frame.Width) + sx) * 3;
                var botIdx = ((botSy * frame.Width) + sx) * 3;

                var (tr, tg, tb) = Enhance(frame.Rgb24[topIdx], frame.Rgb24[topIdx + 1], frame.Rgb24[topIdx + 2], 1.0);
                var (br, bg, bb) = Enhance(frame.Rgb24[botIdx], frame.Rgb24[botIdx + 1], frame.Rgb24[botIdx + 2], 1.0);

                line.Append($"\u001b[38;2;{tr};{tg};{tb}m\u001b[48;2;{br};{bg};{bb}m▀");
            }

            line.Append("\u001b[0m");
            canvas.WriteText(x, y + row, line.ToString(), width);
        }
    }

    private void RenderInfo(Canvas canvas, int x, int y, int width, int height)
    {
        if (height <= 0 || width <= 0) return;

        var rows = new[]
        {
            Fit(TrackTitle, width),
            Fit(ArtistAlbumLine, width),
            Fit(TimingLine, width),
            Fit(MetaLine, width),
            Fit(
                UseAsciiGlyphs
                    ? $"i switch render ({RenderMode}) | v exit | space play/pause | n/p next-prev"
                    : $"i switch render ({RenderMode}) · v exit · space play/pause · n/p next-prev",
                width)
        };

        for (var i = 0; i < height && i < rows.Length; i++)
        {
            var style = i == rows.Length - 1 && !HintStyle.IsEmpty ? HintStyle : InfoStyle;
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

        var targetWidth = Math.Clamp((int)Math.Round(width * 0.62), 24, Math.Min(width, 100));
        var targetHeight = Math.Clamp((int)Math.Round(height * 0.74), 10, height);
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
}
