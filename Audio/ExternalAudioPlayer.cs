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
    private const string MpvBackend = "mpv";
    private const string FfplayBackend = "ffplay";
    private const string CvlcBackend = "cvlc";
    private const string VlcBackend = "vlc";
    private const string Mpg123Backend = "mpg123";
    private const int StopWaitTimeoutMs = 1000;
    private const int SignalWaitTimeoutMs = 500;
    private const int MpvVolumeRetryCount = 6;
    private const int MpvVolumeRetryDelayMs = 25;

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
        int safeVolume = Math.Clamp(volumePercent, 0, 100);

        return _backend.Name switch
        {
            MpvBackend => TrySetMpvVolume(safeVolume),
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
                _process.WaitForExit(StopWaitTimeoutMs);
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
            AppendStartOffsetArguments(psi, backend.Name, startSeconds);
        AppendVolumeArguments(psi, backend.Name, volumePercent, out mpvIpcPath);

        if (backend.Name == MpvBackend)
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
        for (int attempt = 0; attempt < MpvVolumeRetryCount; attempt++)
        {
            try
            {
                using Socket socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                socket.Connect(new UnixDomainSocketEndPoint(_mpvIpcPath));
                socket.Send(payload);
                return true;
            }
            catch
            {
                Thread.Sleep(MpvVolumeRetryDelayMs);
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
            kill.WaitForExit(SignalWaitTimeoutMs);
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
            new Backend(MpvBackend, MpvBackend, ["--no-video", "--really-quiet", "--input-terminal=no"]),
            new Backend(FfplayBackend, FfplayBackend, ["-nodisp", "-autoexit", "-loglevel", "quiet", "-nostdin"]),
            new Backend(Mpg123Backend, Mpg123Backend, ["-q"]),
            new Backend(CvlcBackend, CvlcBackend, ["--intf", "dummy", "--play-and-exit", "--no-video", "--quiet"]),
            new Backend(VlcBackend, VlcBackend, ["--intf", "dummy", "--play-and-exit", "--no-video", "--quiet"])
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

    private static void AppendStartOffsetArguments(ProcessStartInfo psi, string backendName, int startSeconds)
    {
        switch (backendName)
        {
            case FfplayBackend:
                psi.ArgumentList.Add("-ss");
                psi.ArgumentList.Add(startSeconds.ToString());
                break;
            case MpvBackend:
                psi.ArgumentList.Add($"--start={startSeconds}");
                break;
            case CvlcBackend:
            case VlcBackend:
                psi.ArgumentList.Add($"--start-time={startSeconds}");
                break;
        }
    }

    private static void AppendVolumeArguments(
        ProcessStartInfo psi,
        string backendName,
        int volumePercent,
        out string? mpvIpcPath)
    {
        mpvIpcPath = null;

        switch (backendName)
        {
            case FfplayBackend:
                psi.ArgumentList.Add("-volume");
                psi.ArgumentList.Add(volumePercent.ToString());
                return;
            case MpvBackend:
                psi.ArgumentList.Add($"--volume={volumePercent}");
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    mpvIpcPath = Path.Combine(Path.GetTempPath(), $"vibevault-mpv-{Guid.NewGuid():N}.sock");
                    psi.ArgumentList.Add($"--input-ipc-server={mpvIpcPath}");
                }
                return;
            case CvlcBackend:
            case VlcBackend:
                psi.ArgumentList.Add($"--volume={Math.Clamp(volumePercent * 2, 0, 200)}");
                return;
            case Mpg123Backend:
                int scale = Math.Clamp((int)Math.Round(32768 * (volumePercent / 100.0)), 0, 32768);
                psi.ArgumentList.Add("-f");
                psi.ArgumentList.Add(scale.ToString());
                return;
        }
    }
}
