using Tessera;
using Tessera.Components.Primitives;
using Tessera.Controls;
using Tessera.Styles;

namespace VibeVault;

internal sealed class CommandBoardControl : Control
{
    public readonly record struct CommandRow(string Group, string Commands);

    private IReadOnlyList<CommandRow> _rows = Array.Empty<CommandRow>();

    public string Title { get; set; } = "Controls";
    public BorderStyle Border { get; set; } = BorderStyle.Rounded;
    public Thickness Padding { get; set; } = Thickness.All(0);

    public TesseraStyle TitleStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FocusedTitleStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle GroupStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle CommandStyle { get; set; } = TesseraStyle.Empty;
    public TesseraStyle BorderStyleText { get; set; } = TesseraStyle.Empty;
    public TesseraStyle FocusedBorderStyle { get; set; } = TesseraStyle.Empty;
    public string FocusMarker { get; set; } = "✦";

    public void SetRows(IReadOnlyList<CommandRow> rows) => _rows = rows ?? Array.Empty<CommandRow>();

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
        if (_rows.Count == 0) return;

        ClearContent(canvas, content);

        var columns = content.Width >= 110 ? 2 : 1;
        var columnGap = columns == 2 ? 2 : 0;
        var columnWidth = Math.Max(1, (content.Width - columnGap) / columns);
        var rowsPerColumn = (int)Math.Ceiling(_rows.Count / (double)columns);

        for (var col = 0; col < columns; col++)
        {
            var startIndex = col * rowsPerColumn;
            var endIndex = Math.Min(_rows.Count, startIndex + rowsPerColumn);
            var x = content.X + col * (columnWidth + columnGap);
            var groupWidth = Math.Min(10, Math.Max(7, columnWidth / 4));

            for (var i = startIndex; i < endIndex; i++)
            {
                var row = i - startIndex;
                if (row >= content.Height) break;

                var item = _rows[i];
                var y = content.Y + row;
                var commandX = Math.Min(content.Right, x + groupWidth + 1);

                canvas.WriteText(x, y, Styled(GroupStyle, Fit(item.Group.ToUpperInvariant(), groupWidth)), groupWidth);
                canvas.WriteText(commandX, y,
                    Styled(CommandStyle, Fit(item.Commands, Math.Max(0, x + columnWidth - commandX))),
                    Math.Max(0, x + columnWidth - commandX));
            }
        }
    }

    private static string Fit(string text, int width)
    {
        if (width <= 0) return string.Empty;
        if (text.Length == width) return text;
        if (text.Length > width) return text[..Math.Max(0, width - 1)] + "…";
        return text.PadRight(width);
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
