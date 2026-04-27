using NAudio.Wave;
using System.Runtime.InteropServices;

namespace VibeVault;

internal sealed class WindowsNaudioAudioPlayer : IAudioPlayer
{
    private readonly object _sync = new();
    private IWavePlayer? _output;
    private AudioFileReader? _reader;
    private bool _disposed;

    public bool IsAvailable => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
    public string BackendName => "naudio";

    public bool IsTrackRunning
    {
        get
        {
            lock (_sync)
                return _output?.PlaybackState == PlaybackState.Playing;
        }
    }

    public bool Play(string filePath, int startSeconds = 0, int volumePercent = 70)
    {
        if (!IsAvailable || !File.Exists(filePath)) return false;

        lock (_sync)
        {
            StopCore();

            try
            {
                _reader = new AudioFileReader(filePath)
                {
                    Volume = Math.Clamp(volumePercent, 0, 100) / 100f
                };

                if (startSeconds > 0)
                    _reader.CurrentTime = TimeSpan.FromSeconds(Math.Max(0, startSeconds));

                _output = new WaveOutEvent();
                _output.Init(_reader);
                _output.Play();
                return true;
            }
            catch
            {
                StopCore();
                return false;
            }
        }
    }

    public bool TrySetVolume(int volumePercent)
    {
        lock (_sync)
        {
            if (_reader is null) return false;
            _reader.Volume = Math.Clamp(volumePercent, 0, 100) / 100f;
            return true;
        }
    }

    public bool Pause()
    {
        lock (_sync)
        {
            if (_output is null || _output.PlaybackState != PlaybackState.Playing) return false;
            _output.Pause();
            return true;
        }
    }

    public bool Resume()
    {
        lock (_sync)
        {
            if (_output is null || _output.PlaybackState != PlaybackState.Paused) return false;
            _output.Play();
            return true;
        }
    }

    public void Stop()
    {
        lock (_sync)
            StopCore();
    }

    public void Dispose()
    {
        if (_disposed) return;
        Stop();
        _disposed = true;
    }

    private void StopCore()
    {
        try
        {
            _output?.Stop();
        }
        catch
        {
        }

        _output?.Dispose();
        _reader?.Dispose();
        _output = null;
        _reader = null;
    }
}
