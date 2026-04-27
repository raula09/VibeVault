using System.Collections.Concurrent;
using Tessera.Controls;
using System.Text;

namespace VibeVault;


internal enum AppView { Library, Playlists, Browser, Visualizer, NewPlaylist, AddToPlaylist, GoogleDriveImport }


internal sealed class VibeVaultState : IDisposable
{
    private readonly VibeVaultDb _db;
    private readonly IAudioPlayer _audio;
    private readonly RealtimeAudioLevelMonitor _levelMonitor = new();
    private readonly DateTime _startedAtUtc = DateTime.UtcNow;
    private readonly List<string> _eventLog = [];
    private readonly ConcurrentDictionary<string, double[]> _loudnessCache = new();

    private List<LibraryTrack> _library     = [];
    private List<Playlist>     _playlists   = [];
    private List<LibraryTrack> _playlistTracks = [];

    private int  _positionSeconds;
    private double _positionRemainderSeconds;
    private DateTime _lastTickUtc = DateTime.UtcNow;
    private int  _volumePercent = 70;
    private bool _shuffleOn;
    private int  _pulseTick;
    private int  _analysisGeneration;
    private bool _queueFromPlaylist;
    private double[]? _currentLoudnessEnvelope;
    private bool _liveLevelEnabled;
    private readonly List<double> _fallbackBandState = [];
    private readonly List<double> _fallbackBandPhase = [];
    private readonly List<double> _fallbackBandRate = [];
    private readonly List<string> _manualQueueTrackIds = [];
    private readonly List<string> _manualQueueHistoryIds = [];

    private int  _librarySelected;
    private readonly HashSet<int> _libraryMarked = [];
    private int _libraryRangeAnchor = -1;
    private int  _playlistTrackSelected;
    private int  _playlistPanelSelected;
    private int  _addToPlaylistSelected;
    private string? _activePlaylistId;

    private string _browserPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
    private List<string> _browserEntries = [];
    private int  _browserSelected;
    private readonly HashSet<int> _browserMarked = [];
    private int _browserRangeAnchor = -1;

    private string _newPlaylistName = string.Empty;
    private string _googleDriveFolderLink = string.Empty;
    private string _searchQuery = string.Empty;
    private readonly string _importCacheDir;

    public VibeVaultState(VibeVaultDb db, IAudioPlayer audio)
    {
        _db = db;
        _audio = audio;
        _importCacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VibeVault",
            "imports");
        Directory.CreateDirectory(_importCacheDir);
        Reload();
        if (!_audio.IsAvailable)
            SetStatus("no audio backend found (install ffplay/mpv/mpg123/vlc)");
        else
            SetStatus($"ready · backend {_audio.BackendName}");
    }


    public AppView View         { get; private set; } = AppView.Library;
    public bool    IsPlaying    { get; private set; }
    public bool    ShuffleOn    => _shuffleOn;
    public string  StatusLine   { get; private set; } = "welcome to vibevault ✦";
    public string  NewPlaylistName => _newPlaylistName;
    public string GoogleDriveFolderLink => _googleDriveFolderLink;
    public string SearchQuery => _searchQuery;
    public bool IsSearchActive { get; private set; }
    public TimeSpan Uptime => DateTime.UtcNow - _startedAtUtc;
    public string AudioBackend => _audio.BackendName;
    public string PulseGlyph => IsPlaying
        ? (_pulseTick % 4) switch
        {
            0 => "▁",
            1 => "▃",
            2 => "▆",
            _ => "█"
        }
        : "◌";
    public IReadOnlyList<string> RecentEvents => _eventLog;

    public IReadOnlyList<LibraryTrack> Library         => _library;
    public IReadOnlyList<Playlist>     Playlists        => _playlists;
    public IReadOnlyList<LibraryTrack> PlaylistTracks   => _playlistTracks;
    public IReadOnlyList<string>       BrowserEntries   => _browserEntries;

    public int LibrarySelectedIndex      => _librarySelected;
    public int PlaylistTrackSelectedIndex => _playlistTrackSelected;
    public int PlaylistPanelSelectedIndex => _playlistPanelSelected;
    public int AddToPlaylistSelectedIndex => _addToPlaylistSelected;
    public int BrowserSelectedIndex      => _browserSelected;
    public int BrowserMarkedCount => _browserMarked.Count;
    public int LibraryMarkedCount => _libraryMarked.Count;
    public int PendingQueueCount => _manualQueueTrackIds.Count;

    public string BrowserPath => _browserPath;
    public string AddToPlaylistPrompt
    {
        get
        {
            if (_library.Count == 0) return "Add Track To Playlist";

            var selection = BuildLibrarySelectionForPlaylistAddIndices();
            if (selection.Count <= 1)
            {
                var selectedIndex = Math.Clamp(_librarySelected, 0, _library.Count - 1);
                return $"Add \"{_library[selectedIndex].Title}\" To Playlist";
            }

            return $"Add {selection.Count} Tracks To Playlist";
        }
    }
    public string VisualizerLine => BuildVisualizerLine();
    public double CurrentLoudnessLevel => GetCurrentLoudnessLevel();

    public LibraryTrack? NowPlaying   { get; private set; }
    public Playlist?     ActivePlaylist => _playlists.FirstOrDefault(p => p.Id == _activePlaylistId);

    public double Progress => NowPlaying is { DurationSeconds: > 0 }
        ? _positionSeconds / (double)NowPlaying.DurationSeconds : 0;

    public string ProgressText =>
        $"{LibraryTrack.FormatTime(_positionSeconds)} / {LibraryTrack.FormatTime(NowPlaying?.DurationSeconds ?? 0)}";

    public string RemainingText =>
        $"-{LibraryTrack.FormatTime(Math.Max(0, (NowPlaying?.DurationSeconds ?? 0) - _positionSeconds))}";
    public int PositionSeconds => _positionSeconds;
    public int DurationSeconds => NowPlaying?.DurationSeconds ?? 0;
    public int VolumePercent => _volumePercent;
    public int VisibleLibraryCount => BuildVisibleLibraryIndices().Count;
    public int VisiblePlaylistTrackCount => BuildVisiblePlaylistTrackIndices().Count;

    public IReadOnlyList<StatItem> BuildNowPlayingStats() =>
        BuildNowPlayingStatsCore();

    public IReadOnlyList<StatItem> BuildLibraryStats() =>
        BuildLibraryStatsCore();

    public IReadOnlyList<LibraryTrack> BuildVisibleLibrary() =>
        BuildVisibleLibraryIndices().Select(i => _library[i]).ToArray();
    public IReadOnlyList<int> BuildVisibleLibrarySourceIndices() =>
        BuildVisibleLibraryIndices();

    public IReadOnlyList<LibraryTrack> BuildVisiblePlaylistTracks() =>
        BuildVisiblePlaylistTrackIndices().Select(i => _playlistTracks[i]).ToArray();

    public int BuildVisibleLibrarySelectedIndex() =>
        FindVisibleIndex(_librarySelected, BuildVisibleLibraryIndices());

    public int BuildVisibleLibraryCurrentIndex()
    {
        if (NowPlaying is null) return -1;
        var fullIndex = _library.FindIndex(t => t.Id == NowPlaying.Id);
        if (fullIndex < 0) return -1;
        return FindVisibleIndex(fullIndex, BuildVisibleLibraryIndices());
    }

    public int BuildVisiblePlaylistSelectedIndex() =>
        FindVisibleIndex(_playlistTrackSelected, BuildVisiblePlaylistTrackIndices());

    public int BuildVisiblePlaylistCurrentIndex()
    {
        if (NowPlaying is null) return -1;
        var fullIndex = _playlistTracks.FindIndex(t => t.Id == NowPlaying.Id);
        if (fullIndex < 0) return -1;
        return FindVisibleIndex(fullIndex, BuildVisiblePlaylistTrackIndices());
    }

    private IReadOnlyList<StatItem> BuildNowPlayingStatsCore() =>
    [
        new StatItem("MODE", IsPlaying ? "LIVE" : "PAUSED"),
        new StatItem("VOL", $"{_volumePercent}"),
        new StatItem("QUEUED", $"{_manualQueueTrackIds.Count}"),
        new StatItem("POSITION", ProgressText),
        new StatItem("REMAINING", RemainingText),
        new StatItem("QUEUE", BuildQueueStat()),
        new StatItem("SCOPE", ActivePlaylist?.Name ?? "Library"),
        new StatItem("BPM", NowPlaying?.Bpm > 0 ? $"{NowPlaying.Bpm}" : "—"),
        new StatItem("YEAR", NowPlaying?.Year > 0 ? $"{NowPlaying.Year}" : "—")
    ];

    private IReadOnlyList<StatItem> BuildLibraryStatsCore()
    {
        var totalSeconds = _library.Sum(t => t.DurationSeconds);
        var avgSeconds = _library.Count == 0 ? 0 : (int)Math.Round(totalSeconds / (double)_library.Count);
        var artistCount = _library.Select(t => t.Artist).Where(static a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var albumCount = _library.Select(t => t.Album).Where(static a => !string.IsNullOrWhiteSpace(a)).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        var bpmTagged = _library.Count(t => t.Bpm > 0);
        var yearTagged = _library.Count(t => t.Year > 0);
        var activeQueue = _queueFromPlaylist && _activePlaylistId is not null ? _playlistTracks.Count : _library.Count;

        return
        [
            new StatItem("TRACKS", $"{_library.Count}"),
            new StatItem("LISTS", $"{_playlists.Count}"),
            new StatItem("TOTAL TIME", FormatDurationDetailed(totalSeconds)),
            new StatItem("AVG TRACK", LibraryTrack.FormatTime(avgSeconds)),
            new StatItem("ARTISTS", $"{artistCount}"),
            new StatItem("ALBUMS", $"{albumCount}"),
            new StatItem("BPM TAGGED", $"{bpmTagged}/{_library.Count}"),
            new StatItem("YEAR TAGGED", $"{yearTagged}/{_library.Count}"),
            new StatItem("ACTIVE QUEUE", $"{activeQueue}"),
            new StatItem("SHUFFLE", _shuffleOn ? "ON" : "OFF")
        ];
    }

    private string BuildQueueStat()
    {
        var queue = _queueFromPlaylist && _activePlaylistId is not null ? _playlistTracks : _library;
        if (queue.Count == 0) return "—";
        if (NowPlaying is null) return $"0/{queue.Count}";

        var index = queue.FindIndex(t => t.Id == NowPlaying.Id);
        return index < 0 ? $"?/{queue.Count}" : $"{index + 1}/{queue.Count}";
    }

    private static string FormatDurationDetailed(int totalSeconds)
    {
        if (totalSeconds <= 0) return "00:00";

        var ts = TimeSpan.FromSeconds(totalSeconds);
        return ts.TotalHours >= 1
            ? $"{(int)ts.TotalHours:00}:{ts.Minutes:00}:{ts.Seconds:00}"
            : $"{ts.Minutes:00}:{ts.Seconds:00}";
    }

    private void AlignSelectionsToSearch()
    {
        AlignSelectionWithVisible(_library, BuildVisibleLibraryIndices(), ref _librarySelected);
        AlignSelectionWithVisible(_playlistTracks, BuildVisiblePlaylistTrackIndices(), ref _playlistTrackSelected);
    }

    private static void AlignSelectionWithVisible(
        List<LibraryTrack> source,
        List<int> visibleIndices,
        ref int selectedIndex)
    {
        if (source.Count == 0)
        {
            selectedIndex = 0;
            return;
        }

        if (visibleIndices.Count == 0)
        {
            selectedIndex = Math.Clamp(selectedIndex, 0, source.Count - 1);
            return;
        }

        if (!visibleIndices.Contains(selectedIndex))
            selectedIndex = visibleIndices[0];
    }

    private static int FindVisibleIndex(int fullIndex, List<int> visibleIndices) =>
        fullIndex < 0 ? -1 : visibleIndices.IndexOf(fullIndex);

    private List<int> BuildVisibleLibraryIndices()
    {
        var result = new List<int>(_library.Count);
        for (var i = 0; i < _library.Count; i++)
        {
            if (MatchesSearch(_library[i], _searchQuery))
                result.Add(i);
        }
        return result;
    }

    private List<int> BuildVisiblePlaylistTrackIndices()
    {
        var result = new List<int>(_playlistTracks.Count);
        for (var i = 0; i < _playlistTracks.Count; i++)
        {
            if (MatchesSearch(_playlistTracks[i], _searchQuery))
                result.Add(i);
        }
        return result;
    }

    private static bool MatchesSearch(LibraryTrack track, string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return true;

        var haystack = $"{track.Title} {track.Artist} {track.Album} {track.Year} {track.Bpm}";
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var term in terms)
        {
            if (!haystack.Contains(term, StringComparison.OrdinalIgnoreCase))
                return false;
        }

        return true;
    }


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
        int next;
        if (_shuffleOn)
            next = Random.Shared.Next(list.Count);
        else
            next = (idx + 1) % list.Count;
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
        var idx  = list.FindIndex(t => t.Id == NowPlaying?.Id);
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

    public void NotifyStatus(string message) => SetStatus(message);


    public void SwitchView(AppView view)
    {
        View = view;
        if (view == AppView.Browser)
            RefreshBrowser();
    }

    public void MoveLibrarySelection(int delta, bool extendSelection = false)
    {
        var visible = BuildVisibleLibraryIndices();
        if (visible.Count == 0)
        {
            _librarySelected = Math.Clamp(_librarySelected, 0, Math.Max(0, _library.Count - 1));
            return;
        }

        var before = _librarySelected;
        var at = Math.Max(0, visible.IndexOf(_librarySelected));
        at = Math.Clamp(at + delta, 0, visible.Count - 1);
        _librarySelected = visible[at];

        if (!extendSelection)
        {
            _libraryRangeAnchor = _librarySelected;
            return;
        }

        if (_libraryRangeAnchor < 0 || _libraryRangeAnchor >= _library.Count)
            _libraryRangeAnchor = before;
        SelectLibraryRange(_libraryRangeAnchor, _librarySelected);
    }

    public void ToggleLibrarySelectionAtCursor(bool additive)
    {
        if (_library.Count == 0) return;
        _librarySelected = Math.Clamp(_librarySelected, 0, _library.Count - 1);

        if (!additive)
            _libraryMarked.Clear();

        if (!_libraryMarked.Add(_librarySelected))
            _libraryMarked.Remove(_librarySelected);

        _libraryRangeAnchor = _librarySelected;
        SetStatus(_libraryMarked.Count == 0
            ? "selection cleared"
            : $"{_libraryMarked.Count} track(s) selected");
    }

    public void EnqueueLibrarySelection()
    {
        if (_library.Count == 0)
        {
            SetStatus("library is empty");
            return;
        }

        var indices = BuildLibrarySelectionForQueueIndices();
        if (indices.Count == 0)
        {
            SetStatus("no tracks selected");
            return;
        }

        foreach (var index in indices)
            _manualQueueTrackIds.Add(_library[index].Id);

        if (indices.Count == 1)
            SetStatus($"queued  {_library[indices[0]].Title}");
        else
            SetStatus($"queued  {indices.Count} track(s)");
    }

    public void MovePlaylistPanel(int delta)
    {
        _playlistPanelSelected = Math.Clamp(_playlistPanelSelected + delta, 0, Math.Max(0, _playlists.Count - 1));
    }

    public void MovePlaylistTrackSelection(int delta)
    {
        var visible = BuildVisiblePlaylistTrackIndices();
        if (visible.Count == 0)
        {
            _playlistTrackSelected = Math.Clamp(_playlistTrackSelected, 0, Math.Max(0, _playlistTracks.Count - 1));
            return;
        }

        var at = Math.Max(0, visible.IndexOf(_playlistTrackSelected));
        at = Math.Clamp(at + delta, 0, visible.Count - 1);
        _playlistTrackSelected = visible[at];
    }

    public void EnqueuePlaylistTrackSelected()
    {
        if (_playlistTracks.Count == 0)
        {
            SetStatus("playlist is empty");
            return;
        }

        var visible = BuildVisiblePlaylistTrackIndices();
        if (visible.Count == 0)
        {
            SetStatus("no tracks selected");
            return;
        }

        if (!visible.Contains(_playlistTrackSelected))
            _playlistTrackSelected = visible[0];

        var track = _playlistTracks[_playlistTrackSelected];
        _manualQueueTrackIds.Add(track.Id);
        SetStatus($"queued  {track.Title}");
    }

    public void ClearManualQueue()
    {
        var pending = _manualQueueTrackIds.Count;
        _manualQueueTrackIds.Clear();
        _manualQueueHistoryIds.Clear();
        SetStatus(pending == 0 ? "queue already empty" : "queue cleared");
    }

    public void MoveBrowserSelection(int delta, bool extendSelection = false)
    {
        var before = _browserSelected;
        _browserSelected = Math.Clamp(_browserSelected + delta, 0, Math.Max(0, _browserEntries.Count - 1));
        if (!extendSelection)
        {
            _browserRangeAnchor = _browserSelected;
            return;
        }

        if (_browserRangeAnchor < 0)
            _browserRangeAnchor = before;
        SelectBrowserRange(_browserRangeAnchor, _browserSelected);
    }


    public void CueLibrarySelected()
    {
        var visible = BuildVisibleLibraryIndices();
        if (visible.Count == 0) return;
        if (!visible.Contains(_librarySelected))
            _librarySelected = visible[0];
        _queueFromPlaylist = false;
        PlayTrack(_library[_librarySelected]);
    }

    public void OpenBrowser()
    {
        SwitchView(AppView.Browser);
    }

    public void BrowserActivate()
    {
        if (_browserMarked.Count > 0)
        {
            ImportMarkedFiles();
            SwitchView(AppView.Library);
            return;
        }

        if (_browserEntries.Count == 0) return;
        var entry = _browserEntries[_browserSelected];

        if (entry == "../")
        {
            NavigateUp();
            return;
        }

        var fullPath = Path.Combine(_browserPath, entry);

        if (Directory.Exists(fullPath))
        {
            _browserPath = fullPath;
            _browserSelected = 0;
            RefreshBrowser();
            return;
        }

        if (File.Exists(fullPath) && fullPath.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
        {
            ImportFile(fullPath);
            SwitchView(AppView.Library);
        }
    }

    public void ToggleBrowserSelectionAtCursor(bool additive)
    {
        if (!IsBrowserEntrySelectable(_browserSelected)) return;

        if (!additive)
            _browserMarked.Clear();

        if (!_browserMarked.Add(_browserSelected))
            _browserMarked.Remove(_browserSelected);

        _browserRangeAnchor = _browserSelected;
        SetStatus(_browserMarked.Count == 0
            ? "selection cleared"
            : $"{_browserMarked.Count} file(s) selected");
    }

    public bool IsBrowserEntryMarked(int index) => _browserMarked.Contains(index);
    public bool IsBrowserEntrySelectableForImport(int index) => IsBrowserEntrySelectable(index);

    public void NavigateUp()
    {
        var parent = Directory.GetParent(_browserPath)?.FullName;
        if (parent is null) return;
        _browserPath = parent;
        _browserSelected = 0;
        RefreshBrowser();
    }

    private void ImportFile(string path)
    {
        var track = Mp3Scanner.ScanFile(path);
        if (track is null)
        {
            SetStatus("could not read file");
            return;
        }
        _db.UpsertTrack(track);
        Reload();
        SetStatus($"imported  {track.Title}");
        _librarySelected = Math.Max(0, _library.FindIndex(t => t.Id == track.Id));
    }

    private void ImportMarkedFiles()
    {
        var imported = 0;
        string? lastId = null;

        foreach (var index in _browserMarked.OrderBy(i => i))
        {
            if (index < 0 || index >= _browserEntries.Count) continue;
            if (!IsBrowserEntrySelectable(index)) continue;

            var path = Path.Combine(_browserPath, _browserEntries[index]);
            if (!File.Exists(path)) continue;

            var track = Mp3Scanner.ScanFile(path);
            if (track is null) continue;
            _db.UpsertTrack(track);
            imported++;
            lastId = track.Id;
        }

        _browserMarked.Clear();
        _browserRangeAnchor = -1;
        Reload();

        if (imported == 0)
        {
            SetStatus("no files imported");
            return;
        }

        if (lastId is not null)
            _librarySelected = Math.Max(0, _library.FindIndex(t => t.Id == lastId));
        SetStatus($"imported {imported} file(s)");
    }

    public void DeleteLibrarySelected()
    {
        var visible = BuildVisibleLibraryIndices();
        if (visible.Count == 0) return;
        if (!visible.Contains(_librarySelected))
            _librarySelected = visible[0];
        var track = _library[_librarySelected];
        _db.DeleteTrack(track.Id);
        _loudnessCache.TryRemove(track.Id, out _);
        Reload();
        _librarySelected = Math.Clamp(_librarySelected, 0, Math.Max(0, _library.Count - 1));
        SetStatus($"removed  {track.Title}  from library");
    }

    public void SelectPlaylist()
    {
        if (_playlists.Count == 0) return;
        _activePlaylistId      = _playlists[_playlistPanelSelected].Id;
        _playlistTracks        = _db.LoadPlaylistTracks(_activePlaylistId).ToList();
        _playlistTrackSelected = 0;
        _queueFromPlaylist = true;
        SetStatus($"opened  {ActivePlaylist!.Name}");
    }

    public void CuePlaylistTrack()
    {
        var visible = BuildVisiblePlaylistTrackIndices();
        if (visible.Count == 0) return;
        if (!visible.Contains(_playlistTrackSelected))
            _playlistTrackSelected = visible[0];
        _queueFromPlaylist = true;
        PlayTrack(_playlistTracks[_playlistTrackSelected]);
    }

    public void RemovePlaylistTrackSelected()
    {
        if (_activePlaylistId is null) return;
        var visible = BuildVisiblePlaylistTrackIndices();
        if (visible.Count == 0) return;
        if (!visible.Contains(_playlistTrackSelected))
            _playlistTrackSelected = visible[0];
        var track = _playlistTracks[_playlistTrackSelected];
        _db.RemoveTrackFromPlaylist(_activePlaylistId, track.Id);
        _playlistTracks = _db.LoadPlaylistTracks(_activePlaylistId).ToList();
        _playlistTrackSelected = Math.Clamp(_playlistTrackSelected, 0, Math.Max(0, _playlistTracks.Count - 1));
        SetStatus($"removed  {track.Title}  from playlist");
    }

    public void StartAddToPlaylistDialog()
    {
        if (_library.Count == 0)
        {
            SetStatus("library is empty");
            return;
        }

        if (_playlists.Count == 0)
        {
            SetStatus("no playlists yet (press F2, then n)");
            return;
        }

        _addToPlaylistSelected = Math.Clamp(_playlistPanelSelected, 0, _playlists.Count - 1);
        View = AppView.AddToPlaylist;
    }

    public void MoveAddToPlaylistSelection(int delta)
    {
        _addToPlaylistSelected = Math.Clamp(_addToPlaylistSelected + delta, 0, Math.Max(0, _playlists.Count - 1));
    }

    public void ConfirmAddToPlaylist()
    {
        if (_library.Count == 0 || _playlists.Count == 0)
        {
            View = AppView.Library;
            return;
        }

        _playlistPanelSelected = Math.Clamp(_addToPlaylistSelected, 0, _playlists.Count - 1);
        _activePlaylistId = _playlists[_playlistPanelSelected].Id;

        var selectedIndices = BuildLibrarySelectionForPlaylistAddIndices();
        if (selectedIndices.Count == 0)
        {
            View = AppView.Library;
            SetStatus("no tracks selected");
            return;
        }

        foreach (var index in selectedIndices)
            _db.AddTrackToPlaylist(_activePlaylistId, _library[index].Id);

        _playlistTracks = _db.LoadPlaylistTracks(_activePlaylistId).ToList();
        if (selectedIndices.Count == 1)
        {
            var track = _library[selectedIndices[0]];
            SetStatus($"added  {track.Title}  →  {ActivePlaylist?.Name}");
        }
        else
        {
            SetStatus($"added  {selectedIndices.Count} track(s)  →  {ActivePlaylist?.Name}");
        }

        _libraryMarked.Clear();
        _libraryRangeAnchor = _librarySelected;
        View = AppView.Library;
    }

    public void CancelAddToPlaylistDialog()
    {
        View = AppView.Library;
    }

    public void StartNewPlaylist()
    {
        _newPlaylistName = string.Empty;
        View = AppView.NewPlaylist;
    }

    public void StartGoogleDriveImportDialog()
    {
        _googleDriveFolderLink = string.Empty;
        View = AppView.GoogleDriveImport;
    }

    public void GoogleDriveLinkAppendChar(char c)
    {
        if (_googleDriveFolderLink.Length < 800)
            _googleDriveFolderLink += c;
    }

    public void GoogleDriveLinkBackspace()
    {
        if (_googleDriveFolderLink.Length > 0)
            _googleDriveFolderLink = _googleDriveFolderLink[..^1];
    }

    public void CancelGoogleDriveImportDialog()
    {
        View = AppView.Browser;
    }

    public void ConfirmGoogleDriveImport()
    {
        var link = _googleDriveFolderLink.Trim();
        if (string.IsNullOrWhiteSpace(link))
        {
            SetStatus("paste a google drive folder link");
            return;
        }

        SetStatus("google drive import started...");
        GoogleDriveDownloadResult downloaded;
        try
        {
            downloaded = GoogleDriveFolderDownloader
                .DownloadMp3FilesAsync(link, _importCacheDir)
                .GetAwaiter()
                .GetResult();
        }
        catch
        {
            SetStatus("google drive import failed");
            View = AppView.Browser;
            return;
        }

        if (!string.IsNullOrWhiteSpace(downloaded.Error))
        {
            SetStatus(downloaded.Error!);
            View = AppView.Browser;
            return;
        }

        var imported = 0;
        string? lastId = null;
        foreach (var path in downloaded.DownloadedPaths)
        {
            var track = Mp3Scanner.ScanFile(path);
            if (track is null) continue;
            _db.UpsertTrack(track);
            imported++;
            lastId = track.Id;
        }

        Reload();
        if (lastId is not null)
            _librarySelected = Math.Max(0, _library.FindIndex(t => t.Id == lastId));

        View = imported > 0 ? AppView.Library : AppView.Browser;
        if (imported == 0)
        {
            SetStatus("downloaded files but found no valid mp3 tracks");
            return;
        }

        var failures = downloaded.FailedDownloads;
        if (failures > 0)
        {
            SetStatus($"imported {imported} track(s) from {downloaded.TotalFiles} downloaded file(s)");
            return;
        }

        SetStatus($"imported {imported} track(s) from google drive");
    }

    public void ActivateSearch()
    {
        IsSearchActive = true;
    }

    public void DeactivateSearch()
    {
        IsSearchActive = false;
    }

    public void ClearSearch()
    {
        var hadQuery = !string.IsNullOrWhiteSpace(_searchQuery);
        _searchQuery = string.Empty;
        IsSearchActive = false;
        AlignSelectionsToSearch();
        if (hadQuery)
            SetStatus("search cleared");
    }

    public void AppendSearchChar(char c)
    {
        _searchQuery += c;
        AlignSelectionsToSearch();
    }

    public void BackspaceSearch()
    {
        if (_searchQuery.Length == 0) return;
        _searchQuery = _searchQuery[..^1];
        AlignSelectionsToSearch();
    }

    public void NewPlaylistAppendChar(char c)
    {
        if (_newPlaylistName.Length < 40)
            _newPlaylistName += c;
    }

    public void NewPlaylistBackspace()
    {
        if (_newPlaylistName.Length > 0)
            _newPlaylistName = _newPlaylistName[..^1];
    }

    public void ConfirmNewPlaylist()
    {
        var name = _newPlaylistName.Trim();
        if (string.IsNullOrEmpty(name)) { View = AppView.Playlists; return; }
        var pl = _db.CreatePlaylist(name);
        Reload();
        _activePlaylistId = pl.Id;
        _playlistTracks = _db.LoadPlaylistTracks(_activePlaylistId).ToList();
        _playlistTrackSelected = 0;
        _playlistPanelSelected = _playlists.FindIndex(p => p.Id == pl.Id);
        View = AppView.Playlists;
        SetStatus($"created playlist  {name}");
    }

    public void DeleteActivePlaylist()
    {
        if (_activePlaylistId is null) return;
        var name = ActivePlaylist?.Name ?? "playlist";
        _db.DeletePlaylist(_activePlaylistId);
        _activePlaylistId = null;
        _playlistTracks.Clear();
        _queueFromPlaylist = false;
        Reload();
        _playlistPanelSelected = Math.Clamp(_playlistPanelSelected, 0, Math.Max(0, _playlists.Count - 1));
        SetStatus($"deleted  {name}");
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

    private void Reload()
    {
        _library   = _db.LoadAllTracks().ToList();
        _playlists = _db.LoadAllPlaylists().ToList();
        var validIds = _library.Select(t => t.Id).ToHashSet(StringComparer.Ordinal);
        _manualQueueTrackIds.RemoveAll(id => !validIds.Contains(id));
        _manualQueueHistoryIds.RemoveAll(id => !validIds.Contains(id));
        _libraryMarked.Clear();
        _libraryRangeAnchor = -1;
        if (_activePlaylistId is not null)
            _playlistTracks = _db.LoadPlaylistTracks(_activePlaylistId).ToList();
    }

    public void Dispose()
    {
        _levelMonitor.Stop();
        _audio.Stop();
        _audio.Dispose();
    }

    private void RefreshBrowser()
    {
        _browserEntries.Clear();
        _browserMarked.Clear();
        _browserRangeAnchor = -1;
        _browserEntries.Add("../");
        try
        {
            foreach (var dir in Directory.GetDirectories(_browserPath).OrderBy(d => d))
                _browserEntries.Add(Path.GetFileName(dir) + "/");

            foreach (var file in Directory.GetFiles(_browserPath, "*.mp3").OrderBy(f => f))
                _browserEntries.Add(Path.GetFileName(file));
        }
        catch
        {
            SetStatus("cannot read directory");
        }
    }

    private bool IsBrowserEntrySelectable(int index)
    {
        if (index <= 0 || index >= _browserEntries.Count) return false;
        var entry = _browserEntries[index];
        if (entry.EndsWith("/")) return false;
        var path = Path.Combine(_browserPath, entry);
        return File.Exists(path) && path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase);
    }

    private void SelectBrowserRange(int a, int b)
    {
        _browserMarked.Clear();
        var start = Math.Min(a, b);
        var end = Math.Max(a, b);
        for (var i = start; i <= end; i++)
            if (IsBrowserEntrySelectable(i))
                _browserMarked.Add(i);
    }

    private void SelectLibraryRange(int a, int b)
    {
        _libraryMarked.Clear();
        if (_library.Count == 0) return;

        var start = Math.Clamp(Math.Min(a, b), 0, _library.Count - 1);
        var end = Math.Clamp(Math.Max(a, b), 0, _library.Count - 1);
        for (var i = start; i <= end; i++)
            _libraryMarked.Add(i);

        SetStatus($"{_libraryMarked.Count} track(s) selected");
    }

    public bool IsLibraryTrackMarked(int index) => _libraryMarked.Contains(index);

    private List<int> BuildLibrarySelectionForPlaylistAddIndices()
    {
        if (_library.Count == 0) return [];

        if (_libraryMarked.Count == 0)
            return [Math.Clamp(_librarySelected, 0, _library.Count - 1)];

        var selected = _libraryMarked
            .Where(i => i >= 0 && i < _library.Count)
            .OrderBy(i => i)
            .ToList();
        return selected.Count == 0 ? [Math.Clamp(_librarySelected, 0, _library.Count - 1)] : selected;
    }

    private List<int> BuildLibrarySelectionForQueueIndices()
    {
        if (_library.Count == 0) return [];

        if (_libraryMarked.Count == 0)
            return [Math.Clamp(_librarySelected, 0, _library.Count - 1)];

        var visible = BuildVisibleLibraryIndices().ToHashSet();
        var selected = _libraryMarked
            .Where(i => i >= 0 && i < _library.Count)
            .Where(i => visible.Contains(i))
            .OrderBy(i => i)
            .ToList();
        return selected.Count == 0 ? [Math.Clamp(_librarySelected, 0, _library.Count - 1)] : selected;
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
        var levels = "▁▂▃▄▅▆▇█";
        var sb = new StringBuilder(bars);

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
                var tonal = (0.52 + (lowBandBias * 0.48));
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
        var radius = 2;
        var sum = 0.0;
        var count = 0;
        for (var i = centerIndex - radius; i <= centerIndex + radius; i++)
        {
            var idx = Math.Clamp(i, 0, envelope.Length - 1);
            sum += envelope[idx];
            count++;
        }

        return count == 0 ? 0 : (sum / count);
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

        var rng = new Random(1979 + bars);
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
        }
        else
        {
            SetStatus($"seek set  {LibraryTrack.FormatTime(_positionSeconds)}");
        }
    }

    private void SetStatus(string message)
    {
        StatusLine = message;
        _eventLog.Add($"{DateTime.Now:HH:mm:ss}  {message}");
        if (_eventLog.Count > 8)
            _eventLog.RemoveAt(0);
    }
}
