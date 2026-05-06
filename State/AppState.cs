using System.Collections.Concurrent;

namespace VibeVault;

internal enum AppView { Library, Playlists, Browser, Visualizer, NewPlaylist, AddToPlaylist, GoogleDriveImport, DeletePlaylistConfirm }

internal sealed partial class VibeVaultState : IDisposable
{
    private readonly VibeVaultDb _db;
    private readonly IAudioPlayer _audio;
    private readonly RealtimeAudioLevelMonitor _levelMonitor = new();
    private readonly DateTime _startedAtUtc = DateTime.UtcNow;
    private readonly List<string> _eventLog = [];
    private readonly ConcurrentDictionary<string, double[]> _loudnessCache = new();

    private List<LibraryTrack> _library = [];
    private List<Playlist> _playlists = [];
    private List<LibraryTrack> _playlistTracks = [];

    private int _positionSeconds;
    private double _positionRemainderSeconds;
    private DateTime _lastTickUtc = DateTime.UtcNow;
    private int _volumePercent = 70;
    private bool _shuffleOn;
    private int _pulseTick;
    private int _analysisGeneration;
    private bool _queueFromPlaylist;
    private double[]? _currentLoudnessEnvelope;
    private bool _liveLevelEnabled;
    private readonly List<double> _fallbackBandState = [];
    private readonly List<double> _fallbackBandPhase = [];
    private readonly List<double> _fallbackBandRate = [];
    private readonly List<string> _manualQueueTrackIds = [];
    private readonly List<string> _manualQueueHistoryIds = [];

    private int _librarySelected;
    private readonly HashSet<int> _libraryMarked = [];
    private int _libraryRangeAnchor = -1;
    private int _playlistTrackSelected;
    private int _playlistPanelSelected;
    private int _addToPlaylistSelected;
    private string? _activePlaylistId;

    private string _browserPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
    private List<string> _browserEntries = [];
    private int _browserSelected;
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
        _manualQueueTrackIds.Clear();
        _manualQueueHistoryIds.Clear();

        if (!_audio.IsAvailable)
            SetStatus("no audio backend found (install ffplay/mpv/mpg123/vlc)");
        else
            SetStatus($"ready · backend {_audio.BackendName}");
    }

    public AppView View { get; private set; } = AppView.Library;
    public bool IsPlaying { get; private set; }
    public bool ShuffleOn => _shuffleOn;
    public string StatusLine { get; private set; } = "welcome to vibevault ✦";
    public string NewPlaylistName => _newPlaylistName;
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
    public IReadOnlyList<LibraryTrack> Library => _library;
    public IReadOnlyList<Playlist> Playlists => _playlists;
    public IReadOnlyList<LibraryTrack> PlaylistTracks => _playlistTracks;
    public IReadOnlyList<string> BrowserEntries => _browserEntries;

    public int LibrarySelectedIndex => _librarySelected;
    public int PlaylistTrackSelectedIndex => _playlistTrackSelected;
    public int PlaylistPanelSelectedIndex => _playlistPanelSelected;
    public int AddToPlaylistSelectedIndex => _addToPlaylistSelected;
    public int BrowserSelectedIndex => _browserSelected;
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

    public string DeletePlaylistPrompt
    {
        get
        {
            if (_playlists.Count == 0) return "Delete selected playlist?";
            var selectedIndex = Math.Clamp(_playlistPanelSelected, 0, _playlists.Count - 1);
            var name = _playlists[selectedIndex].Name;
            return $"Delete playlist \"{name}\"? This cannot be undone.";
        }
    }

    public string VisualizerLine => BuildVisualizerLine();
    public double CurrentLoudnessLevel => GetCurrentLoudnessLevel();

    public LibraryTrack? NowPlaying { get; private set; }
    public Playlist? ActivePlaylist => _playlists.FirstOrDefault(p => p.Id == _activePlaylistId);

    public double Progress => NowPlaying is { DurationSeconds: > 0 }
        ? _positionSeconds / (double)NowPlaying.DurationSeconds
        : 0;

    public string ProgressText =>
        $"{LibraryTrack.FormatTime(_positionSeconds)} / {LibraryTrack.FormatTime(NowPlaying?.DurationSeconds ?? 0)}";

    public string RemainingText =>
        $"-{LibraryTrack.FormatTime(Math.Max(0, (NowPlaying?.DurationSeconds ?? 0) - _positionSeconds))}";

    public int PositionSeconds => _positionSeconds;
    public int DurationSeconds => NowPlaying?.DurationSeconds ?? 0;
    public int VolumePercent => _volumePercent;
    public int VisibleLibraryCount => BuildVisibleLibraryIndices().Count;
    public int VisiblePlaylistTrackCount => BuildVisiblePlaylistTrackIndices().Count;
}
