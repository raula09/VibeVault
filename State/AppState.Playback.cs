using System.Text;

namespace VibeVault;

internal sealed partial class VibeVaultState
{
    public void Tick()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastTickUtc).TotalSeconds;
        if (elapsed < 0 || elapsed > 2) elapsed = 0;
        _lastTickUtc = now;

        _pulseTick++;
        if (!IsPlaying || NowPlaying is null) return;

        if (!_audio.IsTrackRunning)
        {
            _levelMonitor.Stop();
            PlayNext();
            return;
        }

        _positionRemainderSeconds += elapsed;
        if (_positionRemainderSeconds >= 1.0)
        {
            var step = (int)_positionRemainderSeconds;
            _positionSeconds += step;
            _positionRemainderSeconds -= step;
        }

        if (_positionSeconds >= NowPlaying.DurationSeconds)
            PlayNext();
    }

    public void TogglePlayPause()
    {
        if (NowPlaying is null)
        {
            PlaySelected();
            return;
        }

        if (IsPlaying)
        {
            if (_audio.Pause())
            {
                IsPlaying = false;
                _positionRemainderSeconds = 0;
                _levelMonitor.Stop();
                SetStatus($"paused   {NowPlaying.Title}");
            }
            else
            {
                _audio.Stop();
                IsPlaying = false;
                SetStatus($"stopped  {NowPlaying.Title}");
            }

            return;
        }

        if (_audio.Resume())
        {
            IsPlaying = true;
            _lastTickUtc = DateTime.UtcNow;
            StartLiveLevelMonitor(_positionSeconds);
            SetStatus($"playing  {NowPlaying.Title}");
            return;
        }

        if (_audio.Play(NowPlaying.FilePath, _positionSeconds, _volumePercent))
        {
            IsPlaying = true;
            _positionRemainderSeconds = 0;
            _lastTickUtc = DateTime.UtcNow;
            StartLiveLevelMonitor(_positionSeconds);
            SetStatus($"playing  {NowPlaying.Title}");
            return;
        }

        IsPlaying = false;
        SetStatus("cannot play track (missing audio backend?)");
    }

    public void PlayNext()
    {
        if (TryDequeueNextTrack(out var queuedTrack))
        {
            if (NowPlaying is not null)
                PushQueueHistory(NowPlaying.Id);

            _queueFromPlaylist = false;
            PlayTrack(queuedTrack);
            return;
        }

        var list = ActivePlaylistTrackList();
        if (list.Count == 0) return;

        var idx = list.FindIndex(t => t.Id == NowPlaying?.Id);
        var next = _shuffleOn
            ? Random.Shared.Next(list.Count)
            : (idx + 1) % list.Count;

        PlayTrack(list[next]);
    }

    public void PlayPrevious()
    {
        if (TryPopQueueHistory(out var previousTrack))
        {
            if (NowPlaying is not null)
                _manualQueueTrackIds.Insert(0, NowPlaying.Id);

            _queueFromPlaylist = false;
            PlayTrack(previousTrack);
            return;
        }

        var list = ActivePlaylistTrackList();
        if (list.Count == 0) return;

        var idx = list.FindIndex(t => t.Id == NowPlaying?.Id);
        var prev = (idx - 1 + list.Count) % list.Count;
        PlayTrack(list[prev]);
    }

    public void ToggleShuffle()
    {
        _shuffleOn = !_shuffleOn;
        SetStatus(_shuffleOn ? "shuffle on" : "shuffle off");
    }

    public void AdjustVolume(int deltaPercent)
    {
        var next = Math.Clamp(_volumePercent + deltaPercent, 0, 100);
        if (next == _volumePercent) return;

        _volumePercent = next;

        if (IsPlaying)
        {
            var liveApplied = _audio.TrySetVolume(_volumePercent);
            SetStatus(liveApplied
                ? $"volume  {_volumePercent}"
                : $"volume  {_volumePercent}  (live not supported by {_audio.BackendName})");
            return;
        }

        SetStatus($"volume  {_volumePercent}");
    }

    public void SeekBy(int deltaSeconds)
    {
        SeekToSeconds(_positionSeconds + deltaSeconds);
    }

    public void SeekToRatio(double ratio)
    {
        if (NowPlaying is null || NowPlaying.DurationSeconds <= 0) return;

        var target = (int)Math.Round(Math.Clamp(ratio, 0, 1) * NowPlaying.DurationSeconds);
        SeekToSeconds(target);
    }

    private void PlaySelected()
    {
        if (_library.Count == 0) return;

        _queueFromPlaylist = false;
        PlayTrack(_library[_librarySelected]);
    }

    private void PlayTrack(LibraryTrack track)
    {
        NowPlaying = track;
        _positionSeconds = 0;
        _positionRemainderSeconds = 0;
        _lastTickUtc = DateTime.UtcNow;
        BeginLoudnessAnalysis(track);

        IsPlaying = _audio.Play(track.FilePath, 0, _volumePercent);
        if (IsPlaying)
            StartLiveLevelMonitor(0);

        SetStatus(IsPlaying
            ? $"playing  {track.Artist}  –  {track.Title}"
            : $"cannot play  {track.Title}  (backend: {_audio.BackendName})");
    }

    private List<LibraryTrack> ActivePlaylistTrackList()
    {
        if (!_queueFromPlaylist || _activePlaylistId is null || _playlistTracks.Count == 0)
            return _library;

        return _playlistTracks;
    }

    private bool TryDequeueNextTrack(out LibraryTrack track)
    {
        while (_manualQueueTrackIds.Count > 0)
        {
            var id = _manualQueueTrackIds[0];
            _manualQueueTrackIds.RemoveAt(0);
            var next = _library.FirstOrDefault(t => t.Id == id);
            if (next is null) continue;

            track = next;
            return true;
        }

        track = default!;
        return false;
    }

    private bool TryPopQueueHistory(out LibraryTrack track)
    {
        while (_manualQueueHistoryIds.Count > 0)
        {
            var last = _manualQueueHistoryIds[^1];
            _manualQueueHistoryIds.RemoveAt(_manualQueueHistoryIds.Count - 1);
            var previous = _library.FirstOrDefault(t => t.Id == last);
            if (previous is null) continue;

            track = previous;
            return true;
        }

        track = default!;
        return false;
    }

    private void PushQueueHistory(string trackId)
    {
        _manualQueueHistoryIds.Add(trackId);
        if (_manualQueueHistoryIds.Count > 100)
            _manualQueueHistoryIds.RemoveAt(0);
    }

    private string BuildVisualizerLine()
    {
        const int bars = 96;
        const string levels = "▁▂▃▄▅▆▇█";
        StringBuilder sb = new StringBuilder(bars);

        var envelope = _currentLoudnessEnvelope;
        var duration = Math.Max(1.0, NowPlaying?.DurationSeconds ?? 1);
        var precisePosition = Math.Clamp(_positionSeconds + _positionRemainderSeconds, 0.0, duration);

        if (envelope is not { Length: > 0 })
        {
            EnsureFallbackBandState(bars);
            var loudness = Math.Clamp(GetCurrentLoudnessLevel(), 0.0, 1.0);
            for (var i = 0; i < bars; i++)
            {
                var t = i / (double)Math.Max(1, bars - 1);
                var lowBandBias = Math.Pow(1.0 - t, 0.72);

                var waveA = (Math.Sin((_pulseTick * _fallbackBandRate[i]) + _fallbackBandPhase[i]) + 1.0) * 0.5;
                var waveB = (Math.Sin((_pulseTick * (_fallbackBandRate[i] * 1.65)) + (_fallbackBandPhase[i] * 1.27)) + 1.0) * 0.5;
                var waveC = (Math.Sin((_pulseTick * 0.045) + (t * 14.0)) + 1.0) * 0.5;
                var motion = (waveA * 0.56) + (waveB * 0.29) + (waveC * 0.15);

                var floor = 0.03 + (loudness * 0.05);
                var tonal = 0.52 + (lowBandBias * 0.48);
                var target = Math.Clamp((motion * loudness * tonal) + floor, 0.0, 1.0);

                var previous = _fallbackBandState[i];
                var blend = target >= previous ? 0.90 : 0.58;
                var value = previous + ((target - previous) * blend);
                _fallbackBandState[i] = value;

                var idx = Math.Clamp((int)Math.Round(value * (levels.Length - 1)), 0, levels.Length - 1);
                sb.Append(levels[idx]);
            }

            return sb.ToString();
        }

        const double windowSeconds = 4.0;

        for (var i = 0; i < bars; i++)
        {
            var t = i / (double)Math.Max(1, bars - 1);
            var sampleTime = precisePosition + ((t - 0.5) * windowSeconds);
            sampleTime = Math.Clamp(sampleTime, 0.0, duration);

            var raw = SampleEnvelopeAtTime(envelope, sampleTime, duration);
            var shaped = Math.Pow(Math.Clamp(raw, 0.0, 1.0), 0.80);
            var idx = Math.Clamp((int)Math.Round(shaped * (levels.Length - 1)), 0, levels.Length - 1);
            sb.Append(levels[idx]);
        }

        return sb.ToString();
    }

    private static double SampleEnvelopeAtTime(double[] envelope, double timeSeconds, double durationSeconds)
    {
        if (envelope.Length == 0 || durationSeconds <= 0) return 0;

        var ratio = Math.Clamp(timeSeconds / durationSeconds, 0.0, 1.0);
        var center = ratio * (envelope.Length - 1);
        var centerIndex = (int)Math.Round(center);
        const int radius = 2;
        var sum = 0.0;
        var count = 0;

        for (var i = centerIndex - radius; i <= centerIndex + radius; i++)
        {
            var idx = Math.Clamp(i, 0, envelope.Length - 1);
            sum += envelope[idx];
            count++;
        }

        return count == 0 ? 0 : sum / count;
    }

    private void EnsureFallbackBandState(int bars)
    {
        if (_fallbackBandState.Count == bars) return;

        if (_fallbackBandState.Count > bars)
        {
            _fallbackBandState.RemoveRange(bars, _fallbackBandState.Count - bars);
            _fallbackBandPhase.RemoveRange(bars, _fallbackBandPhase.Count - bars);
            _fallbackBandRate.RemoveRange(bars, _fallbackBandRate.Count - bars);
            return;
        }

        Random rng = new Random(1979 + bars);
        while (_fallbackBandState.Count < bars)
        {
            _fallbackBandState.Add(0);
            _fallbackBandPhase.Add(rng.NextDouble() * Math.PI * 2.0);
            _fallbackBandRate.Add(0.055 + (rng.NextDouble() * 0.11));
        }
    }

    private void BeginLoudnessAnalysis(LibraryTrack track)
    {
        _analysisGeneration++;
        var generation = _analysisGeneration;

        if (_loudnessCache.TryGetValue(track.Id, out var cached))
        {
            _currentLoudnessEnvelope = cached;
            return;
        }

        _currentLoudnessEnvelope = null;

        _ = Task.Run(async () =>
        {
            try
            {
                var analyzed = await AudioLoudnessAnalyzer.AnalyzeAsync(track.FilePath);
                if (analyzed is null || analyzed.Length == 0) return;

                _loudnessCache[track.Id] = analyzed;
                if (generation != _analysisGeneration) return;
                if (NowPlaying?.Id != track.Id) return;
                _currentLoudnessEnvelope = analyzed;
            }
            catch
            {
            }
        });
    }

    private double GetCurrentLoudnessLevel()
    {
        if (_liveLevelEnabled)
        {
            var fallback = _currentLoudnessEnvelope is { Length: > 0 }
                ? SampleEnvelopeByCurrentPosition(_currentLoudnessEnvelope, Math.Max(1, NowPlaying?.DurationSeconds ?? 1))
                : 0.06;

            return _levelMonitor.GetLatestLevel(fallback);
        }

        var envelope = _currentLoudnessEnvelope;
        if (envelope is null || envelope.Length == 0)
        {
            if (!IsPlaying) return 0.05;

            var fallback = (Math.Sin(_pulseTick * 0.3) + 1.0) * 0.5;
            return (fallback * 0.25) + 0.15;
        }

        if (envelope.Length == 1) return envelope[0];

        var duration = Math.Max(1, NowPlaying?.DurationSeconds ?? 1);
        return SampleEnvelopeByCurrentPosition(envelope, duration);
    }

    private void StartLiveLevelMonitor(int startSeconds)
    {
        if (NowPlaying is null)
        {
            _liveLevelEnabled = false;
            _levelMonitor.Stop();
            return;
        }

        _liveLevelEnabled = _levelMonitor.Start(NowPlaying.FilePath, Math.Max(0, startSeconds));
    }

    private double SampleEnvelopeByCurrentPosition(double[] envelope, int duration)
    {
        if (envelope.Length == 0) return 0;

        var precisePosition = Math.Clamp(_positionSeconds + _positionRemainderSeconds, 0, duration);
        var ratio = Math.Clamp(precisePosition / duration, 0.0, 1.0);
        var idx = (int)Math.Round(ratio * (envelope.Length - 1));
        return envelope[Math.Clamp(idx, 0, envelope.Length - 1)];
    }

    private void SeekToSeconds(int target)
    {
        if (NowPlaying is null) return;

        _positionSeconds = Math.Clamp(target, 0, NowPlaying.DurationSeconds);
        _positionRemainderSeconds = 0;
        _lastTickUtc = DateTime.UtcNow;
        _audio.Stop();

        if (IsPlaying)
        {
            IsPlaying = _audio.Play(NowPlaying.FilePath, _positionSeconds, _volumePercent);
            if (IsPlaying)
                StartLiveLevelMonitor(_positionSeconds);
            else
            {
                _liveLevelEnabled = false;
                _levelMonitor.Stop();
            }

            SetStatus(IsPlaying
                ? $"seek  {LibraryTrack.FormatTime(_positionSeconds)}"
                : $"seek failed  {NowPlaying.Title}");
            return;
        }

        SetStatus($"seek set  {LibraryTrack.FormatTime(_positionSeconds)}");
    }
}
