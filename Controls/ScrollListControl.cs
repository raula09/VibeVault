using Tessera;
using Tessera.Components.Primitives;
using Tessera.Controls;
using Tessera.Styles;

namespace VibeVault;

internal sealed class ScrollListControl : Control
{
    public record ListItem(string Label, string? RightMeta = null);

    private IReadOnlyList<ListItem> _items = Array.Empty<ListItem>();

    public string        Title            { get; set; } = string.Empty;
    public BorderStyle   Border           { get; set; } = BorderStyle.Rounded;
    public Thickness     Padding          { get; set; } = Thickness.All(1);
    public int           SelectedIndex    { get; set; }
    public int           CurrentIndex     { get; set; } = -1;
    public string        FocusMarker      { get; set; } = "✦";
    public string        EmptyMessage     { get; set; } = "— empty —";
    public string        CurrentPrefix    { get; set; } = "●";
    public string        SelectedPrefix   { get; set; } = "◆";
    public string        ItemPrefix       { get; set; } = "·";

    public TesseraStyle TitleStyle          { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FocusedTitleStyle   { get; set; } = TesseraStyle.Empty;
    public TesseraStyle ItemStyle           { get; set; } = TesseraStyle.Empty;
    public TesseraStyle SelectedItemStyle   { get; set; } = TesseraStyle.Empty;
    public TesseraStyle CurrentItemStyle    { get; set; } = TesseraStyle.Empty;
    public TesseraStyle MetaStyle           { get; set; } = TesseraStyle.Empty;
    public TesseraStyle MutedStyle          { get; set; } = TesseraStyle.Empty;
    public TesseraStyle BorderStyleText     { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FocusedBorderStyle  { get; set; } = TesseraStyle.Empty;

    public void SetItems(IReadOnlyList<ListItem> items) =>
        _items = items ?? Array.Empty<ListItem>();

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

        if (_items.Count == 0)
        {
            canvas.WriteText(content.X, content.Y, Styled(MutedStyle, EmptyMessage), content.Width);
            return;
        }

        var start = 0;
        if (_items.Count > content.Height)
            start = Math.Clamp(SelectedIndex - content.Height / 2, 0, _items.Count - content.Height);

        for (var row = 0; row < content.Height && start + row < _items.Count; row++)
        {
            var idx  = start + row;
            var item = _items[idx];

            var prefix = idx == CurrentIndex ? CurrentPrefix : idx == SelectedIndex ? SelectedPrefix : ItemPrefix;

            var style = idx == SelectedIndex  ? SelectedItemStyle
                      : idx == CurrentIndex   ? CurrentItemStyle
                      : ItemStyle;

            var label = $"{prefix} {item.Label}";
            var maxLabel = content.Width - (item.RightMeta?.Length ?? 0) - 2;
            var labelWidth = Math.Max(0, maxLabel);
            if (idx == SelectedIndex && TrySplitEmojiPrefix(label, out var emojiPrefix, out var remainder))
            {
                // Keep icon cells unstyled on selected rows; some terminals hide emoji under ANSI-styled backgrounds.
                var emojiWidth = Math.Min(labelWidth, emojiPrefix.Length);
                if (emojiWidth > 0)
                    canvas.WriteText(content.X, content.Y + row, emojiPrefix, emojiWidth);
                var restWidth = Math.Max(0, labelWidth - emojiWidth);
                if (restWidth > 0)
                    canvas.WriteText(content.X + emojiWidth, content.Y + row, Styled(style, remainder), restWidth);
            }
            else
            {
                canvas.WriteText(content.X, content.Y + row, Styled(style, label), labelWidth);
            }

            if (item.RightMeta is not null)
            {
                var rx = Math.Max(content.X, content.Right - item.RightMeta.Length);
                canvas.WriteText(rx, content.Y + row, Styled(MetaStyle, item.RightMeta),
                    content.Right - rx);
            }
        }
    }

    private static string Styled(TesseraStyle s, string t) =>
        s.IsEmpty || string.IsNullOrEmpty(t) ? t : s.Render(t);

    private static bool TrySplitEmojiPrefix(string label, out string emojiPrefix, out string remainder)
    {
        emojiPrefix = string.Empty;
        remainder = label;
        if (string.IsNullOrEmpty(label)) return false;

        var firstSpace = label.IndexOf(' ');
        if (firstSpace < 0 || firstSpace + 1 >= label.Length) return false;
        var secondSpace = label.IndexOf(' ', firstSpace + 1);
        if (secondSpace < 0) return false;

        var iconToken = label[(firstSpace + 1)..secondSpace];
        if (!ContainsBrowserIcon(iconToken)) return false;

        emojiPrefix = label[..secondSpace];
        remainder = label[secondSpace..];
        return true;
    }

    private static bool ContainsBrowserIcon(string token) =>
        token.Contains("📁", StringComparison.Ordinal) ||
        token.Contains("🎵", StringComparison.Ordinal) ||
        token.Contains("📄", StringComparison.Ordinal) ||
        token.Contains("⬆", StringComparison.Ordinal);

    private static void ClearContent(Canvas canvas, Rect content)
    {
        var blank = new string(' ', content.Width);
        for (var row = 0; row < content.Height; row++)
            canvas.WriteText(content.X, content.Y + row, blank, content.Width);
    }
}
