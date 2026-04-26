using Tessera;
using Tessera.Components.Primitives;
using Tessera.Controls;
using Tessera.Styles;

namespace VibeVault;

internal sealed class NowPlayingControl : Control
{
    public string HeaderTitle   { get; set; } = "VibeVault ✦ Now Playing";
    public string TrackTitle    { get; set; } = "— no track loaded —";
    public string ArtistLine    { get; set; } = string.Empty;
    public string AlbumLine     { get; set; } = string.Empty;
    public string ProgressLine  { get; set; } = string.Empty;
    public string RemainingLine { get; set; } = string.Empty;
    public string StatusChip    { get; set; } = string.Empty;
    public string ShuffleChip   { get; set; } = string.Empty;
    public string PlaylistChip  { get; set; } = string.Empty;
    public BorderStyle Border   { get; set; } = BorderStyle.Rounded;
    public Thickness   Padding  { get; set; } = Thickness.All(1);

    public TesseraStyle TitleStyle    { get; set; } = TesseraStyle.Empty;
    public TesseraStyle TrackStyle    { get; set; } = TesseraStyle.Empty;
    public TesseraStyle ArtistStyle   { get; set; } = TesseraStyle.Empty;
    public TesseraStyle AlbumStyle    { get; set; } = TesseraStyle.Empty;
    public TesseraStyle ChipStyle     { get; set; } = TesseraStyle.Empty;
    public TesseraStyle ProgressStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle MutedStyle    { get; set; } = TesseraStyle.Empty;
    public TesseraStyle BorderStyleText { get; set; } = TesseraStyle.Empty;

    public override void Render(Canvas canvas, Rect rect)
    {
        var clipped = Rect.Intersect(rect, canvas.Bounds);
        if (clipped.IsEmpty) return;

        canvas.DrawBox(clipped, Styled(TitleStyle, HeaderTitle), Border, BorderStyleText);
        var content = clipped.Inset(1, 1).Inset(Padding);
        if (content.IsEmpty) return;
        ClearContent(canvas, content);

        ClearContent(canvas, content);

        Line(canvas, content, 0, Styled(TrackStyle, TrackTitle));
        Line(canvas, content, 1, Styled(ArtistStyle, ArtistLine));
        Line(canvas, content, 2, Styled(AlbumStyle, AlbumLine));

        var chips = $"{Styled(ChipStyle, $"[{StatusChip}]")}  " +
                    $"{Styled(ChipStyle, $"[{ShuffleChip}]")}  " +
                    $"{Styled(ChipStyle, $"[{PlaylistChip}]")}";
        Line(canvas, content, 3, chips);

        Line(canvas, content, 4,
            $"{Styled(ProgressStyle, ProgressLine)}  {Styled(MutedStyle, RemainingLine)}");
    }

    private static void Line(Canvas canvas, Rect content, int row, string text)
    {
        if (row >= content.Height) return;
        canvas.WriteText(content.X, content.Y + row, text, content.Width);
    }

    private static void ClearContent(Canvas canvas, Rect content)
    {
        var blank = new string(' ', content.Width);
        for (var row = 0; row < content.Height; row++)
            canvas.WriteText(content.X, content.Y + row, blank, content.Width);
    }

    private static string Styled(TesseraStyle style, string text) =>
        style.IsEmpty || string.IsNullOrEmpty(text) ? text : style.Render(text);
}
