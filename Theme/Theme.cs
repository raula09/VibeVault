using Tessera.Styles;

namespace VibeVault;
internal static class VibeVaultTheme
{
    public static TesseraTheme DefaultTheme { get; } = new()
    {
        Text = new TesseraThemeTextTokens
        {
            Primary   = Foreground(0xEDE3F6),  
            Secondary = Foreground(0xC2A8D8), 
            Muted     = Foreground(0x7A5A99),   
            Inverse   = ForegroundBackground(0x1A0E2A, 0xEDE3F6)
        },
        Surface = new TesseraThemeSurfaceTokens
        {
            Base    = Background(0x100818),  
            Panel   = Background(0x180E26),   
            Overlay = Background(0x221533)    
        },
        Border = new TesseraThemeBorderTokens
        {
            Default = Foreground(0x3D2260),   
            Strong  = Foreground(0x7344A0),  
            Focused = Foreground(0xF3BE5A),   
            Error   = Foreground(0xF28D74)
        },
        State = new TesseraThemeStateTokens
        {
            Success = Foreground(0x9FD9A3),
            Warning = Foreground(0xF3BE5A),
            Error   = Foreground(0xF28D74),
            Info    = Foreground(0xC4A7FF)
        },
        Accent = new TesseraThemeAccentTokens
        {
            Primary   = Foreground(0xC47EFF),
            Secondary = Foreground(0xF3BE5A)
        },
        Selection = new TesseraThemeSelectionTokens
        {
            Foreground = Foreground(0x1A0E2A),
            Background = Background(0xC47EFF)
        },
        Focus = new TesseraThemeFocusTokens
        {
            Ring   = Foreground(0xF3BE5A).WithBold(),
            Title  = Foreground(0xF3BE5A).WithBold(),
            Border = Foreground(0xF3BE5A).WithBold(),
            Marker = "✦"
        }
    };


    public static TesseraStyle Foreground(int color)
    {
        var (r, g, b) = Split(color);
        return TesseraStyle.Empty.WithForeground(AnsiColor.Rgb(r, g, b));
    }

    public static TesseraStyle Background(int color)
    {
        var (r, g, b) = Split(color);
        return TesseraStyle.Empty.WithBackground(AnsiColor.Rgb(r, g, b));
    }

    public static TesseraStyle ForegroundBackground(int fg, int bg)
    {
        var (fr, fg2, fb) = Split(fg);
        var (br, bg2, bb) = Split(bg);
        return TesseraStyle.Empty
            .WithForeground(AnsiColor.Rgb(fr, fg2, fb))
            .WithBackground(AnsiColor.Rgb(br, bg2, bb));
    }

    public static TesseraStyle Chip(int fg, int bg, bool bold = true)
    {
        var style = ForegroundBackground(fg, bg);
        return bold ? style.WithBold() : style;
    }

    private static (byte R, byte G, byte B) Split(int color) =>
        ((byte)((color >> 16) & 0xFF), (byte)((color >> 8) & 0xFF), (byte)(color & 0xFF));
}
