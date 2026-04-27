using System.Text;
using Tessera;

namespace VibeVault;

internal sealed class TerminalGlyphProfile
{
    public bool UseAscii { get; }
    public bool UseLegacyUnicodeGlyphs { get; }
    public BorderStyle BorderStyle => UseAscii ? BorderStyle.Ascii : BorderStyle.Rounded;
    public string FocusMarker => UseAscii ? "*" : UseLegacyUnicodeGlyphs ? "◆" : "✦";

    private TerminalGlyphProfile(bool useAscii, bool useLegacyUnicodeGlyphs = false)
    {
        UseAscii = useAscii;
        UseLegacyUnicodeGlyphs = useLegacyUnicodeGlyphs;
    }

    public static TerminalGlyphProfile Detect()
    {
        var forceAscii = ReadBoolEnv("VIBEVAULT_ASCII");
        if (forceAscii.HasValue) return new TerminalGlyphProfile(forceAscii.Value, useLegacyUnicodeGlyphs: false);

        var forceUnicode = ReadBoolEnv("VIBEVAULT_UNICODE");
        if (forceUnicode.HasValue) return new TerminalGlyphProfile(!forceUnicode.Value, useLegacyUnicodeGlyphs: false);

        if (!OperatingSystem.IsWindows()) return new TerminalGlyphProfile(useAscii: false);

        var termProgram = Environment.GetEnvironmentVariable("TERM_PROGRAM");
        var term = Environment.GetEnvironmentVariable("TERM");
        var inWindowsTerminal = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("WT_SESSION"));
        var inVsCodeTerminal = string.Equals(termProgram, "vscode", StringComparison.OrdinalIgnoreCase);
        var inWezTerm = string.Equals(termProgram, "wezterm", StringComparison.OrdinalIgnoreCase);
        var inConEmu = string.Equals(
            Environment.GetEnvironmentVariable("ConEmuANSI"),
            "ON",
            StringComparison.OrdinalIgnoreCase);
        var termLooksModern = !string.IsNullOrWhiteSpace(term) && (
            term.Contains("xterm", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("wezterm", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("msys", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("cygwin", StringComparison.OrdinalIgnoreCase) ||
            term.Contains("mintty", StringComparison.OrdinalIgnoreCase));

        var fullUnicodeHost = inWindowsTerminal || inVsCodeTerminal || inWezTerm || inConEmu || termLooksModern;

        // Keep Tessera Unicode visuals on Windows; only downgrade to a safer
        // Unicode subset in classic hosts where some glyphs render as '?'.
        return new TerminalGlyphProfile(useAscii: false, useLegacyUnicodeGlyphs: !fullUnicodeHost);
    }

    public string Normalize(string? text)
    {
        if (string.IsNullOrEmpty(text)) return text ?? string.Empty;
        if (!UseAscii && !UseLegacyUnicodeGlyphs) return text;

        var normalized = UseAscii
            ? text
                .Replace("▌▌", "||", StringComparison.Ordinal)
                .Replace("⇌", "<->", StringComparison.Ordinal)
                .Replace("▶", ">", StringComparison.Ordinal)
                .Replace("⬆", "^", StringComparison.Ordinal)
                .Replace("📁", "[DIR]", StringComparison.Ordinal)
                .Replace("🎵", "[MP3]", StringComparison.Ordinal)
                .Replace("📄", "[FILE]", StringComparison.Ordinal)
                .Replace("…", "...", StringComparison.Ordinal)
            : text
                .Replace("✦", "◆", StringComparison.Ordinal)
                .Replace("▶", "►", StringComparison.Ordinal)
                .Replace("⇌", "↔", StringComparison.Ordinal);

        var builder = new StringBuilder(normalized.Length + 16);
        foreach (var ch in normalized)
        {
            builder.Append(ch switch
            {
                '✦' when UseAscii => '*',
                '·' when UseAscii => '|',
                '—' when UseAscii => '-',
                '–' when UseAscii => '-',
                '→' when UseAscii => '>',
                '◌' when UseAscii => 'o',
                '▁' when UseAscii => '.',
                '▂' when UseAscii => ':',
                '▃' when UseAscii => '-',
                '▄' when UseAscii => '=',
                '▅' when UseAscii => '+',
                '▆' when UseAscii => '*',
                '▇' when UseAscii => '#',
                '█' when UseAscii => '#',
                '◆' when UseAscii => '*',
                '◇' when UseAscii => 'o',
                '●' when UseAscii => '*',
                '▮' when UseAscii => '#',
                '▯' when UseAscii => '-',
                '─' when UseAscii => '-',
                '┄' when UseAscii => '-',

                '◌' when UseLegacyUnicodeGlyphs => '○',
                '▁' when UseLegacyUnicodeGlyphs => '░',
                '▂' when UseLegacyUnicodeGlyphs => '░',
                '▃' when UseLegacyUnicodeGlyphs => '▒',
                '▄' when UseLegacyUnicodeGlyphs => '▒',
                '▅' when UseLegacyUnicodeGlyphs => '▓',
                '▆' when UseLegacyUnicodeGlyphs => '▓',
                '▇' when UseLegacyUnicodeGlyphs => '█',
                '◇' when UseLegacyUnicodeGlyphs => '○',
                '▯' when UseLegacyUnicodeGlyphs => '□',
                '┄' when UseLegacyUnicodeGlyphs => '─',
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
