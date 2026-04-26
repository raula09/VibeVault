using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace VibeVault;

internal static class AudioLoudnessAnalyzer
{
    private static readonly Regex RmsRegex =
        new(@"lavfi\.astats\.Overall\.RMS_level=([-\d.]+)", RegexOptions.Compiled);

    public static async Task<double[]?> AnalyzeAsync(string filePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(filePath)) return null;
        var ffmpeg = ResolveFfmpegCommand();
        if (ffmpeg is null) return null;

        var psi = new ProcessStartInfo
        {
            FileName = ffmpeg,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        psi.ArgumentList.Add("-nostdin");
        psi.ArgumentList.Add("-v");
        psi.ArgumentList.Add("error");
        psi.ArgumentList.Add("-nostats");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(filePath);
        psi.ArgumentList.Add("-af");
        psi.ArgumentList.Add("astats=metadata=1:reset=1,ametadata=print:key=lavfi.astats.Overall.RMS_level");
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("null");
        psi.ArgumentList.Add("-");

        try
        {
            using var proc = Process.Start(psi);
            if (proc is null) return null;

            using var reg = cancellationToken.Register(() =>
            {
                try
                {
                    if (!proc.HasExited) proc.Kill(entireProcessTree: true);
                }
                catch
                {
                }
            });

            var stdOutTask = proc.StandardOutput.ReadToEndAsync(cancellationToken);
            var stdErrTask = proc.StandardError.ReadToEndAsync(cancellationToken);
            await proc.WaitForExitAsync(cancellationToken);
            var output = (await stdOutTask) + "\n" + (await stdErrTask);

            var raw = ParseRmsLevels(output);
            if (raw.Count == 0) return null;
            return Smooth(raw);
        }
        catch
        {
            return null;
        }
    }

    private static List<double> ParseRmsLevels(string text)
    {
        var levels = new List<double>(256);
        foreach (Match match in RmsRegex.Matches(text))
        {
            if (!double.TryParse(match.Groups[1].Value, out var db)) continue;
            if (double.IsNaN(db) || double.IsInfinity(db)) continue;

            var clamped = Math.Clamp(db, -60.0, 0.0);
            var normalized = (clamped + 60.0) / 60.0;
            levels.Add(normalized);
        }

        return levels;
    }

    private static double[] Smooth(IReadOnlyList<double> source)
    {
        var smoothed = new double[source.Count];
        for (var i = 0; i < source.Count; i++)
        {
            var a = source[Math.Max(0, i - 1)];
            var b = source[i];
            var c = source[Math.Min(source.Count - 1, i + 1)];
            smoothed[i] = ((a * 0.2) + (b * 0.6) + (c * 0.2));
        }

        return smoothed;
    }

    private static string? ResolveFfmpegCommand()
    {
        if (CommandExists("ffmpeg")) return "ffmpeg";
        if (CommandExists("avconv")) return "avconv";
        return null;
    }

    private static bool CommandExists(string command)
    {
        var pathValue = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathValue)) return false;

        var suffixes = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? (Environment.GetEnvironmentVariable("PATHEXT")?.Split(';', StringSplitOptions.RemoveEmptyEntries)
                ?? [".exe", ".cmd", ".bat"])
            : [string.Empty];

        foreach (var dir in pathValue.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            foreach (var suffix in suffixes)
            {
                var fullPath = Path.Combine(dir, command + suffix);
                if (File.Exists(fullPath)) return true;
            }
        }

        return false;
    }
}
