using Tessera;
using Tessera.Components.Primitives;
using Tessera.Styles;

namespace VibeVault;

internal static class ControlCanvasHelpers
{
    public static void ClearContent(Canvas canvas, Rect content)
    {
        string blank = new string(' ', content.Width);
        for (int row = 0; row < content.Height; row++)
            canvas.WriteText(content.X, content.Y + row, blank, content.Width);
    }

    public static string ApplyStyle(TesseraStyle style, string text) =>
        style.IsEmpty || string.IsNullOrEmpty(text) ? text : style.Render(text);
}
