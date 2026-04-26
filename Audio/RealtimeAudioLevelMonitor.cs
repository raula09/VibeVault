using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace VibeVault;

internal sealed class RealtimeAudioLevelMonitor : IDisposable
{
    private static readonly Regex RmsRegex =
        new(@"lavfi\.astats\.Overall\.RMS_level=([-\d.]+)", RegexOptions.Compiled);

    private readonly object _gate = new();
    private Process? _process;
    private double _latestLevel;
    private DateTime _latestAtUtc = DateTime.MinValue;
    private readonly DataReceivedEventHandler _onData;

    public RealtimeAudioLevelMonitor()
    {
        _onData = (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data)) return;
            var m = RmsRegex.Match(e.Data);
            if (!m.Success) return;
            if (!double.TryParse(m.Groups[1].Value, out var db)) return;
            if (double.IsNaN(db) || double.IsInfinity(db)) return;

            var clamped = Math.Clamp(db, -60.0, 0.0);
            var normalized = (clamped + 60.0) / 60.0;
            lock (_gate)
            {
                _latestLevel = normalized;
                _latestAtUtc = DateTime.UtcNow;
            }
        };
    }

    public bool IsRunning
    {
        get
        {
            lock (_gate)
                return _process is { HasExited: false };
        }
    }

    public bool Start(string filePath, int startSeconds)
    {
        if (!File.Exists(filePath)) return false;
        var ffmpeg = ResolveFfmpegCommand();
        if (ffmpeg is null) return false;

        Stop();

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
        psi.ArgumentList.Add("-nostats");
        psi.ArgumentList.Add("-loglevel");
        psi.ArgumentList.Add("info");
        psi.ArgumentList.Add("-re");
        if (startSeconds > 0)
        {
            psi.ArgumentList.Add("-ss");
            psi.ArgumentList.Add(startSeconds.ToString());
        }
        psi.ArgumentList.Add("-i");
        psi.ArgumentList.Add(filePath);
        psi.ArgumentList.Add("-vn");
        psi.ArgumentList.Add("-af");
        psi.ArgumentList.Add("astats=metadata=1:reset=1,ametadata=print:key=lavfi.astats.Overall.RMS_level");
        psi.ArgumentList.Add("-f");
        psi.ArgumentList.Add("null");
        psi.ArgumentList.Add("-");

        try
        {
            var process = Process.Start(psi);
            if (process is null) return false;

            process.OutputDataReceived += _onData;
            process.ErrorDataReceived += _onData;
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            lock (_gate)
            {
                _process = process;
                _latestLevel = 0;
                _latestAtUtc = DateTime.UtcNow;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void Stop()
    {
        Process? process;
        lock (_gate)
        {
            process = _process;
            _process = null;
        }

        if (process is null) return;

        try
        {
            process.OutputDataReceived -= _onData;
            process.ErrorDataReceived -= _onData;
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(500);
            }
        }
        catch
        {
        }
        finally
        {
            process.Dispose();
        }
    }

    public double GetLatestLevel(double fallback)
    {
        lock (_gate)
        {
            var age = DateTime.UtcNow - _latestAtUtc;
            if (age > TimeSpan.FromMilliseconds(750))
                return fallback * 0.35;
            return _latestLevel;
        }
    }

    public void Dispose() => Stop();

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
