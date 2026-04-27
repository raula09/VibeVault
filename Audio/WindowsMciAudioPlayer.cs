using System.Runtime.InteropServices;
using System.Text;

namespace VibeVault;

internal sealed class WindowsMciAudioPlayer : IAudioPlayer
{
    private const string Alias = "vibevault_track";
    private readonly object _sync = new();
    private bool _isOpen;
    private bool _disposed;

    public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public string BackendName => "winmm";
    public bool IsTrackRunning
    {
        get
        {
            lock (_sync)
            {
                if (!_isOpen) return false;
                var mode = QueryStatus("mode");
                return mode.Equals("playing", StringComparison.OrdinalIgnoreCase)
                    || mode.Equals("seeking", StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    public bool Play(string filePath, int startSeconds = 0, int volumePercent = 70)
    {
        if (!IsAvailable || !File.Exists(filePath)) return false;

        lock (_sync)
        {
            StopCore();

            var safePath = filePath.Replace("\"", "\"\"");
            if (!Send($"open \"{safePath}\" type mpegvideo alias {Alias}")) return false;

            _isOpen = true;

            if (!Send($"set {Alias} time format milliseconds"))
            {
                StopCore();
                return false;
            }

            if (!TrySetVolumeInternal(Math.Clamp(volumePercent, 0, 100)))
            {
                StopCore();
                return false;
            }

            var startMs = Math.Max(0, startSeconds) * 1000;
            if (!Send($"play {Alias} from {startMs}"))
            {
                StopCore();
                return false;
            }

            return true;
        }
    }

    public bool TrySetVolume(int volumePercent)
    {
        if (!IsAvailable) return false;

        lock (_sync)
        {
            if (!_isOpen) return false;
            return TrySetVolumeInternal(Math.Clamp(volumePercent, 0, 100));
        }
    }

    public bool Pause()
    {
        if (!IsAvailable) return false;

        lock (_sync)
        {
            if (!_isOpen) return false;
            return Send($"pause {Alias}");
        }
    }

    public bool Resume()
    {
        if (!IsAvailable) return false;

        lock (_sync)
        {
            if (!_isOpen) return false;
            return Send($"resume {Alias}");
        }
    }

    public void Stop()
    {
        if (!IsAvailable) return;
        lock (_sync)
            StopCore();
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
    }

    private bool TrySetVolumeInternal(int volumePercent)
    {
        var mciVolume = (int)Math.Round((volumePercent / 100.0) * 1000);
        mciVolume = Math.Clamp(mciVolume, 0, 1000);
        return Send($"setaudio {Alias} volume to {mciVolume}");
    }

    private void StopCore()
    {
        if (!_isOpen) return;

        Send($"stop {Alias}");
        Send($"close {Alias}");
        _isOpen = false;
    }

    private static bool Send(string command)
    {
        return MciSendString(command, null, 0, IntPtr.Zero) == 0;
    }

    private static string QueryStatus(string key)
    {
        var buffer = new StringBuilder(128);
        _ = MciSendString($"status {Alias} {key}", buffer, buffer.Capacity, IntPtr.Zero);
        return buffer.ToString().Trim();
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int MciSendString(
        string command,
        StringBuilder? returnValue,
        int returnLength,
        IntPtr winHandle);
}
