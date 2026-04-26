using System.Diagnostics;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace VibeVault;

internal interface IAudioPlayer : IDisposable
{
    bool IsAvailable { get; }
    string BackendName { get; }
    bool IsTrackRunning { get; }
    bool Play(string filePath, int startSeconds = 0, int volumePercent = 70);
    bool TrySetVolume(int volumePercent);
    bool Pause();
    bool Resume();
    void Stop();
}

internal sealed class ExternalAudioPlayer : IAudioPlayer
{
    private readonly Backend? _backend;
    private Process? _process;
    private bool _paused;
    private string? _mpvIpcPath;
    private readonly DataReceivedEventHandler _discardOutput = static (_, _) => { };

    public ExternalAudioPlayer()
    {
        _backend = ResolveBackend();
    }

    public bool IsAvailable => _backend is not null;
    public string BackendName => _backend?.Name ?? "none";
    public bool IsTrackRunning => _process is { HasExited: false };

    public bool Play(string filePath, int startSeconds = 0, int volumePercent = 70)
    {
        if (!File.Exists(filePath) || _backend is null) return false;

        Stop();

        var safeVolume = Math.Clamp(volumePercent, 0, 100);
        var psi = BuildStartInfo(_backend, filePath, Math.Max(0, startSeconds), safeVolume, out _mpvIpcPath);
        try
        {
            _process = Process.Start(psi);
            if (_process is null) return false;

            _process.OutputDataReceived += _discardOutput;
            _process.ErrorDataReceived += _discardOutput;
            _process.BeginOutputReadLine();
            _process.BeginErrorReadLine();

            _paused = false;
            return true;
        }
        catch
        {
            _process = null;
            return false;
        }
    }

    public bool TrySetVolume(int volumePercent)
    {
        if (_process is null || _process.HasExited || _backend is null) return false;
        var safeVolume = Math.Clamp(volumePercent, 0, 100);

        return _backend.Name switch
        {
            "mpv" => TrySetMpvVolume(safeVolume),
            _ => false
        };
    }

    public bool Pause()
    {
        if (_process is null || _process.HasExited || _paused) return false;
        if (!TrySignal(_process.Id, "STOP")) return false;
        _paused = true;
        return true;
    }

    public bool Resume()
    {
        if (_process is null || _process.HasExited || !_paused) return false;
        if (!TrySignal(_process.Id, "CONT")) return false;
        _paused = false;
        return true;
    }

    public void Stop()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(true);
                _process.WaitForExit(1000);
            }
        }
        catch
        {
        }
        finally
        {
            if (_process is not null)
            {
                _process.OutputDataReceived -= _discardOutput;
                _process.ErrorDataReceived -= _discardOutput;
                _process.Dispose();
            }
            _process = null;
            _paused = false;
            TryDeleteMpvIpcSocket();
        }
    }

    public void Dispose() => Stop();

    private static ProcessStartInfo BuildStartInfo(
        Backend backend,
        string filePath,
        int startSeconds,
        int volumePercent,
        out string? mpvIpcPath)
    {
        mpvIpcPath = null;
        var psi = new ProcessStartInfo
        {
            FileName = backend.Command,
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        foreach (var arg in backend.FixedArgs)
            psi.ArgumentList.Add(arg);

        if (startSeconds > 0)
        {
            switch (backend.Name)
            {
                case "ffplay":
                    psi.ArgumentList.Add("-ss");
                    psi.ArgumentList.Add(startSeconds.ToString());
                    break;
                case "mpv":
                    psi.ArgumentList.Add($"--start={startSeconds}");
                    break;
                case "cvlc":
                case "vlc":
                    psi.ArgumentList.Add($"--start-time={startSeconds}");
                    break;
            }
        }
        switch (backend.Name)
        {
            case "ffplay":
                psi.ArgumentList.Add("-volume");
                psi.ArgumentList.Add(volumePercent.ToString());
                break;
            case "mpv":
                psi.ArgumentList.Add($"--volume={volumePercent}");
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    mpvIpcPath = Path.Combine(Path.GetTempPath(), $"vibevault-mpv-{Guid.NewGuid():N}.sock");
                    psi.ArgumentList.Add($"--input-ipc-server={mpvIpcPath}");
                }
                break;
            case "cvlc":
            case "vlc":
                psi.ArgumentList.Add($"--volume={Math.Clamp(volumePercent * 2, 0, 200)}");
                break;
            case "mpg123":
                
                var scale = Math.Clamp((int)Math.Round(32768 * (volumePercent / 100.0)), 0, 32768);
                psi.ArgumentList.Add("-f");
                psi.ArgumentList.Add(scale.ToString());
                break;
        }

        if (backend.Name == "mpv")
            psi.ArgumentList.Add("--");

        psi.ArgumentList.Add(filePath);

        return psi;
    }

    private bool TrySetMpvVolume(int volumePercent)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;
        if (string.IsNullOrWhiteSpace(_mpvIpcPath)) return false;

        var payload = Encoding.UTF8.GetBytes(
            $"{{\"command\":[\"set_property\",\"volume\",{volumePercent}]}}\n");
        for (var attempt = 0; attempt < 6; attempt++)
        {
            try
            {
                using var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                socket.Connect(new UnixDomainSocketEndPoint(_mpvIpcPath));
                socket.Send(payload);
                return true;
            }
            catch
            {
                Thread.Sleep(25);
            }
        }

        return false;
    }

    private void TryDeleteMpvIpcSocket()
    {
        if (string.IsNullOrWhiteSpace(_mpvIpcPath))
        {
            _mpvIpcPath = null;
            return;
        }

        try
        {
            if (File.Exists(_mpvIpcPath))
                File.Delete(_mpvIpcPath);
        }
        catch
        {
        }
        finally
        {
            _mpvIpcPath = null;
        }
    }

    private static bool TrySignal(int processId, string signal)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return false;

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "kill",
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            psi.ArgumentList.Add($"-{signal}");
            psi.ArgumentList.Add(processId.ToString());

            using var kill = Process.Start(psi);
            if (kill is null) return false;
            kill.WaitForExit(500);
            return kill.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static Backend? ResolveBackend()
    {
        var candidates = new[]
        {
            new Backend("mpv", "mpv", ["--no-video", "--really-quiet", "--input-terminal=no"]),
            new Backend("ffplay", "ffplay", ["-nodisp", "-autoexit", "-loglevel", "quiet", "-nostdin"]),
            new Backend("mpg123", "mpg123", ["-q"]),
            new Backend("cvlc", "cvlc", ["--intf", "dummy", "--play-and-exit", "--no-video", "--quiet"]),
            new Backend("vlc", "vlc", ["--intf", "dummy", "--play-and-exit", "--no-video", "--quiet"])
        };

        foreach (var candidate in candidates)
        {
            if (CommandExists(candidate.Command))
                return candidate;
        }

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

    private sealed record Backend(string Name, string Command, string[] FixedArgs);
}
