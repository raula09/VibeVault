using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VibeVault;

internal sealed class AlbumArtFrame
{
    public required int Width { get; init; }
    public required int Height { get; init; }
    public required byte[] Rgb24 { get; init; }
}

internal static class AlbumArtExtractor
{
    public static AlbumArtFrame? TryExtract(string filePath, int targetSize = 96)
    {
        if (!File.Exists(filePath)) return null;
        var ffmpeg = ResolveFfmpegCommand();
        if (ffmpeg is null) return null;

        var size = Math.Clamp(targetSize, 48, 192);
        var expectedBytes = size * size * 3;

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
        psi.ArgumentList.Add("-hide_banner");
        psi.ArgumentList.Add("-loglevel");
        psi.ArgumentList.Add("error");
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(filePath);
        psi.ArgumentList.Add("-map");
        psi.ArgumentList.Add("0:v:0");
        psi.ArgumentList.Add("-frames:v");
        psi.ArgumentList.Add("1");
        psi.ArgumentList.Add("-vf");
        psi.ArgumentList.Add($"scale={size}:{size}:force_original_aspect_ratio=decrease,pad={size}:{size}:(ow-iw)/2:(oh-ih)/2:black");
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("rawvideo");
        psi.ArgumentList.Add("-pix_fmt");
        psi.ArgumentList.Add("rgb24");
        psi.ArgumentList.Add("-");

        try
        {
            using var process = Process.Start(psi);
            if (process is null) return null;

            using var mem = new MemoryStream(expectedBytes);
            process.StandardOutput.BaseStream.CopyTo(mem);
            _ = process.StandardError.ReadToEnd();
            process.WaitForExit(2000);

            if (process.ExitCode != 0) return null;

            var bytes = mem.ToArray();
            if (bytes.Length < expectedBytes) return null;

            if (bytes.Length > expectedBytes)
                Array.Resize(ref bytes, expectedBytes);

            return new AlbumArtFrame
            {
                Width = size,
                Height = size,
                Rgb24 = bytes
            };
        }
        catch
        {
            return null;
        }
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
