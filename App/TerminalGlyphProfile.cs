using System.Text;
using Tessera;

namespace VibeVault;

internal sealed class TerminalGlyphProfile
{
    public bool UseAscii { get; }
    public BorderStyle BorderStyle => UseAscii ? BorderStyle.Ascii : BorderStyle.Rounded;
    public string FocusMarker => UseAscii ? "*" : "✦";

    private TerminalGlyphProfile(bool useAscii)
    {
        UseAscii = useAscii;
    }

    public static TerminalGlyphProfile Detect()
    {
        var forceAscii = ReadBoolEnv("VIBEVAULT_ASCII");
        if (forceAscii.HasValue) return new TerminalGlyphProfile(forceAscii.Value);

        var forceUnicode = ReadBoolEnv("VIBEVAULT_UNICODE");
        if (forceUnicode.HasValue) return new TerminalGlyphProfile(!forceUnicode.Value);

        // Default to Unicode/Tessera across hosts (including classic Windows console)
        // so borders and visual glyphs match Linux. Users can still force ASCII via
        // VIBEVAULT_ASCII=1 when running in a font/host that cannot render glyphs.
        return new TerminalGlyphProfile(useAscii: false);
    }

    public string Normalize(string? text)
    {
        if (!UseAscii || string.IsNullOrEmpty(text)) return text ?? string.Empty;

        var normalized = text
            .Replace("▌▌", "||", StringComparison.Ordinal)
            .Replace("⇌", "<->", StringComparison.Ordinal)
            .Replace("▶", ">", StringComparison.Ordinal)
            .Replace("⬆", "^", StringComparison.Ordinal)
            .Replace("📁", "[DIR]", StringComparison.Ordinal)
            .Replace("🎵", "[MP3]", StringComparison.Ordinal)
            .Replace("📄", "[FILE]", StringComparison.Ordinal)
            .Replace("…", "...", StringComparison.Ordinal);

        var builder = new StringBuilder(normalized.Length + 16);
        foreach (var ch in normalized)
        {
            builder.Append(ch switch
            {
                '✦' => '*',
                '·' => '|',
                '—' => '-',
                '–' => '-',
                '→' => '>',
                '◌' => 'o',
                '▁' => '.',
                '▂' => ':',
                '▃' => '-',
                '▄' => '=',
                '▅' => '+',
                '▆' => '*',
                '▇' => '#',
                '█' => '#',
                '◆' => '*',
                '◇' => 'o',
                '●' => '*',
                '▮' => '#',
                '▯' => '-',
                '─' => '-',
                '┄' => '-',
                _ => ch
            });
        }

        return builder.ToString();
    }

    private static bool? ReadBoolEnv(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrWhiteSpace(value)) return null;

        return value.Trim().ToLowerInvariant() switch
        {
            "1" or "true" or "yes" or "on" => true,
            "0" or "false" or "no" or "off" => false,
            _ => null
        };
    }
}
