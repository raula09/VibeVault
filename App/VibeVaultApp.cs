using Tessera;
using Tessera.Controls;
using Tessera.Styles;
using System.Text.Json;

namespace VibeVault;

internal sealed partial class VibeVaultApp : TesseraApp
{
    private readonly record struct UiPalette(
        string Name,
        int BorderColor,
        int PrimaryTextColor,
        int SecondaryTextColor,
        int MutedTextColor,
        int AccentPrimaryColor,
        int AccentSecondaryColor,
        int SelectionForegroundColor,
        int SelectionBackgroundColor,
        int IdleChipBackgroundColor,
        int LiveChipForegroundColor,
        int LiveChipBackgroundColor,
        int MutedChipForegroundColor,
        int MutedChipBackgroundColor,
        int SeekFillColor,
        int SeekTrackColor,
        int SeekLabelColor,
        int SeekKnobColor,
        int ActivityTextColor,
        int VisualizerTextColor);

    private static readonly UiPalette[] UiPalettes =
    [
        new("Neon Violet", 0x8D4BCE, 0xF2E6FF, 0xCFB4F2, 0x8E73B0, 0xE4A0FF, 0xFFCB72, 0x1A0E2A, 0xD689FF, 0x311D4C, 0x122215, 0x8FE1A8, 0xEBD6FF, 0x4A2F58, 0xF29CFF, 0x5A4D73, 0xD3EEFF, 0xFFD978, 0xB6FFD9, 0xAEE9FF),
        new("Aqua Pulse", 0x45BEE0, 0xE9F8FF, 0xA9DEF2, 0x6FA5BD, 0x7FE8FF, 0xFFD27A, 0x06222A, 0x7FE8FF, 0x123847, 0x0E2A1A, 0x87DFA8, 0xD8ECF7, 0x28485A, 0x75E4FF, 0x355B6A, 0xCEF6FF, 0xFFCF69, 0xB6F4D8, 0x9FE9FF),
        new("Solar Flare", 0xD79A3D, 0xFFF4E2, 0xEFC18A, 0xB88A54, 0xFFBA69, 0xFFE380, 0x2C1807, 0xFFB55E, 0x4E2E0D, 0x25220F, 0xB8E286, 0xFFE8CC, 0x61452A, 0xFFB76A, 0x6E5438, 0xFFEFD2, 0xFFE380, 0xDDF7B3, 0xFFE0B3),
        new("Emerald Night", 0x47B97E, 0xE9FFF3, 0xAEE7CA, 0x74B896, 0x7CF0B7, 0xC7F28F, 0x082014, 0x77E2B2, 0x173D2A, 0x122617, 0x97E9B8, 0xD9F7E8, 0x325A46, 0x7EEFBF, 0x40624E, 0xDFFFEF, 0xC8F49A, 0xBDF7D6, 0xA8FFD2),
        new("Rosewave", 0xD36EA4, 0xFFF0F8, 0xF2B9D5, 0xBE84A1, 0xFFA9D4, 0xFFD58C, 0x2A0E1F, 0xFF9FCE, 0x4A2238, 0x261810, 0xF0C77B, 0xFFE3EF, 0x60334A, 0xFFA8D7, 0x6A4A5B, 0xFFEAF4, 0xFFD57B, 0xFFD2E8, 0xFFC8EA),
        new("Mono Ice", 0x90A4B8, 0xF5FAFF, 0xCFDCE8, 0x97A8B7, 0xDFEFFF, 0xD8E2EC, 0x121A22, 0xD2E6FA, 0x2A3642, 0x1E2730, 0xB8C8D8, 0xEDF4FA, 0x3B4A58, 0xD2E6FA, 0x4D5D6D, 0xEAF6FF, 0xE0EAF4, 0xDAE7F3, 0xD7E9FF)
    ];


    private readonly VibeVaultDb    _db;
    private readonly IAudioPlayer   _audio;
    private readonly VibeVaultState _state;
    private readonly string _settingsPath;
    private readonly TerminalGlyphProfile _glyphProfile = TerminalGlyphProfile.Detect();
    private bool _mouseSeekActive;
    private bool _showCommandDeck = true;
    private int _uiPaletteIndex;
    private long _visualFrameCounter;
    private VisualRenderMode _visualRenderMode = VisualRenderMode.Ascii;
    private AppView _viewBeforeVisualizer = AppView.Library;
    private readonly Dictionary<string, AlbumArtFrame?> _albumArtCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly NowPlayingControl _nowPlaying = new()
    {
        Border  = BorderStyle.Rounded,
        Padding = Thickness.All(1)
    };

    private readonly SeekBarControl _seekBar = new()
    {
        Title       = "Timeline",
        Border      = BorderStyle.Rounded,
        Padding     = Thickness.All(0),
        FocusMarker = "✦"
    };
    
    private readonly AudioMeterControl _audioMeter = new()
    {
        Title = "Audio Meter",
        Border = BorderStyle.Rounded,
        Padding = Thickness.All(0),
        FocusMarker = "✦"
    };

    private readonly AlbumArtVisualizerControl _albumArtVisualizer = new()
    {
        Title = "Cover Visual · v exits",
        Border = BorderStyle.Rounded,
        Padding = Thickness.All(0),
        FocusMarker = "✦"
    };

    private readonly ScrollListControl _libraryList = new()
    {
        Title       = "Library · F1",
        Border      = BorderStyle.Rounded,
        Padding     = Thickness.All(1),
        EmptyMessage = "— add MP3s with F4 to import —"
    };

    private readonly ScrollListControl _playlistPanel = new()
    {
        Title       = "Playlists · F2",
        Border      = BorderStyle.Rounded,
        Padding     = Thickness.All(1),
        EmptyMessage = "— press n to create a playlist —"
    };

    private readonly ScrollListControl _playlistTracks = new()
    {
        Title       = "Playlist Tracks",
        Border      = BorderStyle.Rounded,
        Padding     = Thickness.All(1),
        EmptyMessage = "— select a playlist, then press a to add tracks —"
    };

    private readonly ScrollListControl _browserList = new()
    {
        Title       = "Import · Browse MP3s  (Enter=open  Esc=cancel)",
        Border      = BorderStyle.Rounded,
        Padding     = Thickness.All(1)
    };

    private readonly Label _playerStats = new()
    {
        Title   = "Player",
        Border  = BorderStyle.Rounded,
    };

    private readonly Label _libraryStats = new()
    {
        Title   = "Library",
        Border  = BorderStyle.Rounded,
    };

    private readonly Label _sessionCard = new()
    {
        Title   = "Session Pulse",
        Border  = BorderStyle.Rounded,
    };

    private readonly Label _trackFactsCard = new()
    {
        Title   = "Track Lens",
        Border  = BorderStyle.Rounded,
    };

    private readonly Label _visualizerCard = new()
    {
        Title   = "Sound Level",
        Border  = BorderStyle.Rounded,
    };

    private readonly CommandBoardControl _commandDeckCard = new()
    {
        Title   = "Controls",
        Border  = BorderStyle.Rounded
    };

    private readonly Label _activityFeed = new()
    {
        Title   = "Execution Lane",
        Border  = BorderStyle.Rounded,
    };

    private readonly Label _searchBar = new()
    {
        Title   = "Search",
        Border  = BorderStyle.Rounded,
        Padding = Thickness.Symmetric(1, 0)
    };

    private readonly SegmentBarControl _workspaceTabs = new()
    {
        Title = "Workspace",
        Border = BorderStyle.Rounded,
        Padding = Thickness.All(0)
    };

    private readonly SegmentBarControl _modeChips = new()
    {
        Title = "Modes",
        Border = BorderStyle.Rounded,
        Padding = Thickness.All(0)
    };

    private readonly Label _dialogLabel = new()
    {
        Title   = "New Playlist",
        Border  = BorderStyle.Rounded,
        Padding = Thickness.Symmetric(2, 1)
    };

    private readonly ScrollListControl _addToPlaylistList = new()
    {
        Title        = "Choose Playlist",
        Border       = BorderStyle.Rounded,
        Padding      = Thickness.All(1),
        EmptyMessage = "— no playlists —"
    };
    public VibeVaultApp()
    {
        var appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "VibeVault");
        Directory.CreateDirectory(appDataDir);

        var dbPath = Path.Combine(appDataDir, "library.db");
        _settingsPath = Path.Combine(appDataDir, "ui-settings.json");

        _db    = new VibeVaultDb(dbPath);
        _audio = AudioPlayerFactory.Create();
        _state = new VibeVaultState(_db, _audio);

        ApplyGlyphProfile();
        LoadUiPreferences();
        ApplyTheme();
        _libraryList.RequestFocus();

        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            SaveUiPreferences();
            _state.Dispose();
        };
    }


    public override TesseraEffect? Initialize() =>
        TesseraEffects.Periodic(TimeSpan.FromMilliseconds(80), _ => new TickMessage());

    public override TesseraEffect? Update(Message message)
    {
        switch (message)
        {
            case KeyPressed key: return HandleKey(key);
            case TickMessage:
                _state.Tick();
                _visualFrameCounter++;
                return null;
            default:             return TryHandleMouseSeek(message) ? null : null;
        }
    }

    public override Screen Build(ScreenContext context)
    {
        RefreshControls();
        return Screen.Build(window =>
        {
            window.Padding(1);
            window.Gap(1);
            window.Body(body => BuildBody(body));
        });
    }

    private TesseraEffect? HandleKey(KeyPressed key)
    {
        if (key.IsCharacter('c', ModifierKeys.Ctrl)) return TesseraEffects.Quit;

        if (key.IsCharacter('v'))
        {
            ToggleVisualizerView();
            return null;
        }

        if (_state.View == AppView.Visualizer && key.Is(Key.Escape))
        {
            ExitVisualizerView();
            return null;
        }
        if (_state.View == AppView.Visualizer && key.IsCharacter('i'))
        {
            _visualRenderMode = _visualRenderMode == VisualRenderMode.Ascii
                ? VisualRenderMode.Image
                : VisualRenderMode.Ascii;
            SaveUiPreferences();
            _state.NotifyStatus(_visualRenderMode == VisualRenderMode.Ascii
                ? "visual render: ascii"
                : "visual render: image");
            return null;
        }

        if (_state.View == AppView.NewPlaylist)
        {
            if (key.Is(Key.Enter))   { _state.ConfirmNewPlaylist(); return null; }
            if (key.Is(Key.Escape))  { _state.SwitchView(AppView.Playlists); return null; }
            if (key.Is(Key.Backspace)) { _state.NewPlaylistBackspace(); return null; }
            if (TryGetTypedChar(key, out var ch))
                _state.NewPlaylistAppendChar(ch);
            return null;
        }

        if (_state.View == AppView.AddToPlaylist)
        {
            if (key.Is(Key.Escape)) { _state.CancelAddToPlaylistDialog(); return null; }
            if (key.Is(Key.Enter)) { _state.ConfirmAddToPlaylist(); return null; }
            if (key.Is(Key.Up) || key.IsCharacter('k')) { _state.MoveAddToPlaylistSelection(-1); return null; }
            if (key.Is(Key.Down) || key.IsCharacter('j')) { _state.MoveAddToPlaylistSelection(1); return null; }
            return null;
        }

        if ((_state.View == AppView.Library || _state.View == AppView.Playlists) && HandleSearchInput(key))
            return null;

        if (_state.View == AppView.Browser)
        {
            if (key.Is(Key.Escape))  { _state.SwitchView(AppView.Library); return null; }
            if (key.IsCharacter(' ', ModifierKeys.Ctrl)) { _state.ToggleBrowserSelectionAtCursor(true); return null; }
            if (key.IsCharacter(' ')) { _state.ToggleBrowserSelectionAtCursor(false); return null; }
            if (key.Is(Key.Up, ModifierKeys.Shift)) { _state.MoveBrowserSelection(-1, extendSelection: true); return null; }
            if (key.Is(Key.Down, ModifierKeys.Shift)) { _state.MoveBrowserSelection(1, extendSelection: true); return null; }
            if (key.IsCharacter('K') || key.IsCharacter('J'))
            {
                _state.MoveBrowserSelection(key.IsCharacter('K') ? -1 : 1, extendSelection: true);
                return null;
            }
            if (key.Is(Key.Up)   || key.IsCharacter('k')) { _state.MoveBrowserSelection(-1); return null; }
            if (key.Is(Key.Down) || key.IsCharacter('j')) { _state.MoveBrowserSelection(1);  return null; }
            if (key.Is(Key.Enter)) { _state.BrowserActivate(); return null; }
            if (key.Is(Key.Backspace)) { _state.NavigateUp(); return null; }
            return null;
        }
        if (key.Is(Key.F1)) { SwitchToView(AppView.Library);   return null; }
        if (key.Is(Key.F2)) { SwitchToView(AppView.Playlists); return null; }
        if (key.Is(Key.F4)) { _state.OpenBrowser(); return null; }
        if (key.IsCharacter('1')) { SwitchToView(AppView.Library); return null; }
        if (key.IsCharacter('2')) { SwitchToView(AppView.Playlists); return null; }
        if (key.IsCharacter('4')) { _state.OpenBrowser(); return null; }

        if (key.IsCharacter(' ')) { _state.TogglePlayPause(); return null; }
        if (key.IsCharacter('c')) { CycleUiPalette(); return null; }
        if (key.IsCharacter('?'))
        {
            _showCommandDeck = !_showCommandDeck;
            SaveUiPreferences();
            _state.NotifyStatus(_showCommandDeck ? "controls panel shown" : "controls panel hidden");
            return null;
        }
        if (key.IsCharacter('+') || key.IsCharacter('=')) { _state.AdjustVolume(+5); return null; }
        if (key.IsCharacter('-') || key.IsCharacter('_')) { _state.AdjustVolume(-5); return null; }
        if (key.IsCharacter('n') && _state.View != AppView.Playlists) { _state.PlayNext(); return null; }
        if (key.IsCharacter('p')) { _state.PlayPrevious();    return null; }
        if (key.IsCharacter('s')) { _state.ToggleShuffle();   return null; }
        if (key.Is(Key.Left))  { _state.SeekBy(-5); return null; }
        if (key.Is(Key.Right)) { _state.SeekBy(5);  return null; }
        if (_state.View == AppView.Library)
        {
            if (key.Is(Key.Up)   || key.IsCharacter('k')) { _state.MoveLibrarySelection(-1); return null; }
            if (key.Is(Key.Down) || key.IsCharacter('j')) { _state.MoveLibrarySelection(1);  return null; }
            if (key.Is(Key.Enter)) { _state.CueLibrarySelected(); return null; }
            if (key.IsCharacter('a')) { _state.StartAddToPlaylistDialog(); return null; }
            if (key.Is(Key.Delete) || key.IsCharacter('d')) { _state.DeleteLibrarySelected(); return null; }
        }

        if (_state.View == AppView.Playlists)
        {
            if (key.Is(Key.Tab) || key.IsCharacter('l') || key.IsCharacter('h'))
            {
                if (_playlistTracks.IsFocused) _playlistPanel.RequestFocus();
                else _playlistTracks.RequestFocus();
                return null;
            }

            if (_playlistTracks.IsFocused)
            {
                if (key.Is(Key.Up)   || key.IsCharacter('k')) { _state.MovePlaylistTrackSelection(-1); return null; }
                if (key.Is(Key.Down) || key.IsCharacter('j')) { _state.MovePlaylistTrackSelection(1);  return null; }
                if (key.Is(Key.Enter)) { _state.CuePlaylistTrack(); return null; }
                if (key.IsCharacter('r')) { _state.RemovePlaylistTrackSelected(); return null; }
                if (key.IsCharacter('n')) { _state.PlayNext(); return null; }
                return null;
            }

            if (key.Is(Key.Up)   || key.IsCharacter('k')) { _state.MovePlaylistPanel(-1);       return null; }
            if (key.Is(Key.Down) || key.IsCharacter('j')) { _state.MovePlaylistPanel(1);         return null; }
            if (key.Is(Key.Enter)) { _state.SelectPlaylist(); return null; }
            if (key.IsCharacter('n')) { _state.StartNewPlaylist(); return null; }
            if (key.IsCharacter('r')) { _state.RemovePlaylistTrackSelected(); return null; }
            if (key.IsCharacter('D')) { _state.DeleteActivePlaylist(); return null; }
        }

        return null;
    }

    private static bool TryGetTypedChar(KeyPressed key, out char ch)
    {
        const string printable =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 " +
            "-_.,:;!?'\"()[]{}+/\\&@#$%^*=~`|<>";

        foreach (var candidate in printable)
        {
            if (key.IsCharacter(candidate))
            {
                ch = candidate;
                return true;
            }
        }

        ch = default;
        return false;
    }

    private void ToggleVisualizerView()
    {
        if (_state.View == AppView.NewPlaylist || _state.View == AppView.AddToPlaylist)
            return;

        if (_state.View == AppView.Visualizer)
        {
            ExitVisualizerView();
            return;
        }

        _viewBeforeVisualizer = _state.View switch
        {
            AppView.Library => AppView.Library,
            AppView.Playlists => AppView.Playlists,
            _ => AppView.Library
        };
        _state.SwitchView(AppView.Visualizer);
        _albumArtVisualizer.RequestFocus();
        _state.NotifyStatus("cover visual mode on");
    }

    private void ExitVisualizerView()
    {
        SwitchToView(_viewBeforeVisualizer);
        _state.NotifyStatus("cover visual mode off");
    }

    private void SwitchToView(AppView view)
    {
        _state.SwitchView(view);
        if (view == AppView.Library) _libraryList.RequestFocus();
        if (view == AppView.Playlists) _playlistPanel.RequestFocus();
    }

    private bool HandleSearchInput(KeyPressed key)
    {
        if (key.IsCharacter('f', ModifierKeys.Ctrl))
        {
            _state.ActivateSearch();
            return true;
        }

        if (key.Is(Key.Escape))
        {
            if (_state.IsSearchActive || !string.IsNullOrWhiteSpace(_state.SearchQuery))
            {
                _state.ClearSearch();
                return true;
            }
            return false;
        }

        if (!_state.IsSearchActive) return false;

        if (key.Is(Key.Tab) || key.IsCharacter('c'))
        {
            _state.DeactivateSearch();
            return false;
        }

        if (key.Is(Key.Enter))
        {
            _state.DeactivateSearch();
            return true;
        }

        if (key.Is(Key.Backspace))
        {
            _state.BackspaceSearch();
            return true;
        }

        if (key.Is(Key.Up) || key.Is(Key.Down) || key.Is(Key.Left) || key.Is(Key.Right))
            return true;

        if (TryGetTypedChar(key, out var ch))
        {
            _state.AppendSearchChar(ch);
            return true;
        }

        return false;
    }

    private void RefreshControls()
    {
        var track = _state.NowPlaying;

        _nowPlaying.TrackTitle    = track is null ? N("— nothing playing —") : track.Title.ToUpperInvariant();
        _nowPlaying.ArtistLine    = track?.Artist ?? string.Empty;
        _nowPlaying.AlbumLine     = track?.Album  ?? string.Empty;
        _nowPlaying.StatusChip    = N(_state.IsPlaying ? "▶ playing" : "▌▌paused");
        _nowPlaying.ShuffleChip   = N(_state.ShuffleOn ? "⇌ shuffle" : "→ linear");
        _nowPlaying.PlaylistChip  = _state.ActivePlaylist?.Name ?? "library";
        _nowPlaying.ProgressLine  = _state.ProgressText;
        _nowPlaying.RemainingLine = _state.RemainingText;

        _seekBar.CurrentSeconds = _state.PositionSeconds;
        _seekBar.TotalSeconds = _state.DurationSeconds;
        _seekBar.IsPlaying = _state.IsPlaying;
        _seekBar.LeftTime = LibraryTrack.FormatTime(_state.PositionSeconds);
        _seekBar.RightTime = LibraryTrack.FormatTime(_state.DurationSeconds);
        _seekBar.VolumePercent = _state.VolumePercent;
        _audioMeter.Levels = _state.VisualizerLine;
        _audioMeter.OverallLevel = _state.CurrentLoudnessLevel;
        _albumArtVisualizer.CoverArt = ResolveAlbumArt(track);
        _albumArtVisualizer.TrackTitle = track?.Title ?? N("— no track playing —");
        _albumArtVisualizer.ArtistAlbumLine = track is null
            ? N("—")
            : $"{track.Artist}{Sep}{track.Album}{Sep}{track.Year}";
        _albumArtVisualizer.TimingLine =
            $"{_state.ProgressText}  {(_state.DurationSeconds > 0 ? $"{(int)Math.Round(_state.Progress * 100)}%" : "0%")}";
        _albumArtVisualizer.MetaLine =
            $"{(_state.IsPlaying ? "LIVE" : "PAUSED")}{Sep}vol {_state.VolumePercent}%{Sep}{_state.AudioBackend}";
        _albumArtVisualizer.Loudness = _state.CurrentLoudnessLevel;
        _albumArtVisualizer.AnimationFrame = _visualFrameCounter;
        _albumArtVisualizer.RenderMode = _visualRenderMode;
     
        var visibleLibrary = _state.BuildVisibleLibrary();
        _libraryList.SetItems(visibleLibrary.Select(t =>
            new ScrollListControl.ListItem($"{t.Artist}  {N("–")}  {t.Title}", t.DisplayDuration)).ToArray());
        _libraryList.SelectedIndex = _state.BuildVisibleLibrarySelectedIndex();
        _libraryList.CurrentIndex  = _state.BuildVisibleLibraryCurrentIndex();

        _playlistPanel.SetItems(_state.Playlists.Select(static p =>
            new ScrollListControl.ListItem(p.Name)).ToArray());
        _playlistPanel.SelectedIndex = _state.PlaylistPanelSelectedIndex;
        _playlistPanel.CurrentIndex  = _state.Playlists.ToList()
            .FindIndex(p => p.Id == _state.ActivePlaylist?.Id);

        var visiblePlaylistTracks = _state.BuildVisiblePlaylistTracks();
        _playlistTracks.SetItems(visiblePlaylistTracks.Select(t =>
            new ScrollListControl.ListItem($"{t.Artist}  {N("–")}  {t.Title}", t.DisplayDuration)).ToArray());
        _playlistTracks.SelectedIndex = _state.BuildVisiblePlaylistSelectedIndex();
        _playlistTracks.CurrentIndex  = _state.BuildVisiblePlaylistCurrentIndex();

        _browserList.SetItems(_state.BrowserEntries.Select((entry, i) =>
        {
            var selectable = _state.IsBrowserEntrySelectableForImport(i);
            var mark = selectable ? (_state.IsBrowserEntryMarked(i) ? "[x]" : "[ ]") : "   ";
            return new ScrollListControl.ListItem($"{mark} {FormatBrowserEntry(entry, _glyphProfile.UseAscii)}");
        }).ToArray());
        _browserList.SelectedIndex = _state.BrowserSelectedIndex;
        _browserList.Title         = $"Import{Sep}{_state.BrowserPath}{Sep}selected {_state.BrowserMarkedCount}";

        _playerStats.Text  = FormatStatBlock(_state.BuildNowPlayingStats());
        _libraryStats.Text = FormatStatBlock(_state.BuildLibraryStats());
        _sessionCard.Text = N(
            $"Pulse   {_state.PulseGlyph}\n" +
            $"Backend {_state.AudioBackend}\n" +
            $"Uptime  {(int)_state.Uptime.TotalHours:00}:{_state.Uptime.Minutes:00}:{_state.Uptime.Seconds:00}\n" +
            $"View    {_state.View}");
        _trackFactsCard.Text = N(
            $"Track    {track?.Title ?? N("—")}\n" +
            $"Artist   {track?.Artist ?? N("—")}\n" +
            $"Album    {track?.Album ?? N("—")}\n" +
            $"State    {N(_state.IsPlaying ? "▶ playing" : "▌▌paused")}  {N(_state.ShuffleOn ? "⇌ shuffle" : "→ linear")}\n" +
            $"Queue    {_state.ActivePlaylist?.Name ?? "library"}\n" +
            $"Time     {_state.ProgressText}  {_state.RemainingText}");
        _visualizerCard.Text = "\n" + N(_state.VisualizerLine);
        if (_showCommandDeck)
            _commandDeckCard.SetRows(BuildCommandRows());
        _activityFeed.Text = _state.RecentEvents.Count == 0
            ? N("— no activity yet —")
            : N(string.Join('\n', _state.RecentEvents.Select(static e => $"OUT  {e}")));
        var searchScope = _state.View == AppView.Playlists ? "playlist tracks" : "library tracks";
        var visibleMatches = _state.View == AppView.Playlists ? _state.VisiblePlaylistTrackCount : _state.VisibleLibraryCount;
        var matchWord = visibleMatches == 1 ? "match" : "matches";
        var searchText = string.IsNullOrWhiteSpace(_state.SearchQuery)
            ? "type track / artist / album"
            : _state.SearchQuery;
        var caret = _state.IsSearchActive ? "_" : string.Empty;
        _searchBar.Title = _state.IsSearchActive
            ? $"Search{Sep}Ctrl+F editing{Sep}Enter done{Sep}Esc clear"
            : $"Search{Sep}Ctrl+F edit{Sep}Esc clear";
        _searchBar.Text = $"/ {searchText}{caret}  [{visibleMatches} {matchWord} in {searchScope}]";
        _workspaceTabs.SetSegments(BuildWorkspaceSegments());
        _modeChips.SetSegments(BuildModeSegments(track));

        _dialogLabel.Title = "New Playlist";
        _dialogLabel.Text = $"> {_state.NewPlaylistName}_";
        _addToPlaylistList.SetItems(_state.Playlists.Select(static p =>
            new ScrollListControl.ListItem(p.Name)).ToArray());
        _addToPlaylistList.SelectedIndex = _state.AddToPlaylistSelectedIndex;

    }

    private string N(string text) => _glyphProfile.Normalize(text);

    private string Sep => _glyphProfile.UseAscii ? " | " : " · ";

    private void ApplyGlyphProfile()
    {
        _albumArtVisualizer.Title = N(_albumArtVisualizer.Title);
        _albumArtVisualizer.EmptyMessage = N(_albumArtVisualizer.EmptyMessage);
        _libraryList.Title = N(_libraryList.Title);
        _libraryList.EmptyMessage = N(_libraryList.EmptyMessage);
        _playlistPanel.Title = N(_playlistPanel.Title);
        _playlistPanel.EmptyMessage = N(_playlistPanel.EmptyMessage);
        _playlistTracks.EmptyMessage = N(_playlistTracks.EmptyMessage);
        _browserList.Title = N(_browserList.Title);
        _addToPlaylistList.EmptyMessage = N(_addToPlaylistList.EmptyMessage);

        if (!_glyphProfile.UseAscii) return;

        var focus = _glyphProfile.FocusMarker;
        var border = _glyphProfile.BorderStyle;

        _nowPlaying.Border = border;

        _seekBar.Border = border;
        _seekBar.FocusMarker = focus;
        _seekBar.UseAsciiGlyphs = true;

        _audioMeter.Border = border;
        _audioMeter.FocusMarker = focus;
        _audioMeter.UseAsciiGlyphs = true;

        _albumArtVisualizer.Border = border;
        _albumArtVisualizer.FocusMarker = focus;
        _albumArtVisualizer.UseAsciiGlyphs = true;

        ConfigureList(_libraryList, border, focus, "*", ">", ".");
        ConfigureList(_playlistPanel, border, focus, "*", ">", ".");
        ConfigureList(_playlistTracks, border, focus, "*", ">", ".");
        ConfigureList(_browserList, border, focus, "*", ">", ".");
        ConfigureList(_addToPlaylistList, border, focus, "*", ">", ".");

        _playerStats.Border = border;
        _libraryStats.Border = border;
        _sessionCard.Border = border;
        _trackFactsCard.Border = border;
        _visualizerCard.Border = border;
        _commandDeckCard.Border = border;
        _commandDeckCard.FocusMarker = focus;
        _activityFeed.Border = border;
        _searchBar.Border = border;

        _workspaceTabs.Border = border;
        _workspaceTabs.FocusMarker = focus;
        _modeChips.Border = border;
        _modeChips.FocusMarker = focus;

        _dialogLabel.Border = border;
    }

    private static void ConfigureList(
        ScrollListControl list,
        BorderStyle border,
        string focusMarker,
        string currentPrefix,
        string selectedPrefix,
        string itemPrefix)
    {
        list.Border = border;
        list.FocusMarker = focusMarker;
        list.CurrentPrefix = currentPrefix;
        list.SelectedPrefix = selectedPrefix;
        list.ItemPrefix = itemPrefix;
    }

    private IReadOnlyList<SegmentBarControl.Segment> BuildWorkspaceSegments()
    {
        var palette = CurrentUiPalette;
        var active = VibeVaultTheme.Chip(palette.SelectionForegroundColor, palette.SelectionBackgroundColor);
        var idle = VibeVaultTheme.Chip(palette.SecondaryTextColor, palette.IdleChipBackgroundColor, false);
        return
        [
            new SegmentBarControl.Segment("[1 Library]", _state.View == AppView.Library ? active : idle),
            new SegmentBarControl.Segment("[2 Playlists]", _state.View == AppView.Playlists ? active : idle),
            new SegmentBarControl.Segment("[4 Import]", _state.View == AppView.Browser ? active : idle),
            new SegmentBarControl.Segment("[v Visual]", _state.View == AppView.Visualizer ? active : idle)
        ];
    }

    private IReadOnlyList<SegmentBarControl.Segment> BuildModeSegments(LibraryTrack? track)
    {
        var palette = CurrentUiPalette;
        var on = VibeVaultTheme.Chip(palette.SelectionForegroundColor, palette.SelectionBackgroundColor);
        var off = VibeVaultTheme.Chip(palette.SecondaryTextColor, palette.IdleChipBackgroundColor, false);
        var live = VibeVaultTheme.Chip(palette.LiveChipForegroundColor, palette.LiveChipBackgroundColor);
        var muted = VibeVaultTheme.Chip(palette.MutedChipForegroundColor, palette.MutedChipBackgroundColor, false);

        return
        [
            new SegmentBarControl.Segment(_state.IsPlaying ? "[Playback Live]" : "[Playback Idle]", _state.IsPlaying ? live : muted),
            new SegmentBarControl.Segment(_state.ShuffleOn ? "[Queue Shuffle]" : "[Queue Linear]", _state.ShuffleOn ? on : off),
            new SegmentBarControl.Segment(N($"[Theme {CurrentUiPalette.Name}{Sep}c]"), on),
            new SegmentBarControl.Segment(N(_state.View == AppView.Visualizer ? $"[Visual Mode On{Sep}v]" : $"[Visual Mode Off{Sep}v]"), off),
            new SegmentBarControl.Segment(N(_visualRenderMode == VisualRenderMode.Ascii ? $"[Render ASCII{Sep}i]" : $"[Render IMAGE{Sep}i]"), off),
            new SegmentBarControl.Segment(_showCommandDeck ? "[? Hide Controls]" : "[? Show Controls]", off),
            new SegmentBarControl.Segment(_state.ActivePlaylist is null ? "[Scope Library]" : $"[Scope {_state.ActivePlaylist.Name}]", off),
            new SegmentBarControl.Segment($"[Backend {_state.AudioBackend}]", off),
            new SegmentBarControl.Segment(track is null ? "[Track none]" : $"[Track {track.Title}]", off)
        ];
    }

    private IReadOnlyList<CommandBoardControl.CommandRow> BuildCommandRows()
    {
        var rows = new List<CommandBoardControl.CommandRow>
        {
            new("Global", N("Space play/pause · n/p next-prev · s shuffle · +/- volume · c cycle theme · ? controls")),
            new("Views", N("F1/F2/F4 switch · 1/2/4 quick switch · v cover visual"))
        };

        switch (_state.View)
        {
            case AppView.Library:
                rows.Add(new("Library", N("j/k move · Enter play · a add-to-list · d delete")));
                rows.Add(new("Search", N("Ctrl+F edit filter · Enter finish · Esc clear")));
                break;

            case AppView.Playlists:
                rows.Add(new("Lists", N("j/k move playlists · Enter open · n new · D delete")));
                rows.Add(new("Tracks", N("Tab/l toggle panes · h move left · j/k move · Enter play · r remove")));
                rows.Add(new("Search", "Ctrl+F filters playlist tracks by title/artist/album"));
                break;

            case AppView.Browser:
                rows.Add(new("Import", N("j/k move · Enter open/import · Backspace up-dir · Esc cancel")));
                rows.Add(new("Select", N("Space single-select · Ctrl+Space toggle · Shift+Up/Down range")));
                break;

            case AppView.Visualizer:
                rows.Add(new("Visual", N("i toggle render ascii/image · v or Esc exit")));
                rows.Add(new("Visual", N("Space play/pause · n/p switch tracks")));
                rows.Add(new("Visual", "uses embedded album art from MP3 APIC/attached picture"));
                break;

            case AppView.NewPlaylist:
                rows.Add(new("Dialog", N("Type name · Backspace delete · Enter create · Esc cancel")));
                break;

            case AppView.AddToPlaylist:
                rows.Add(new("Dialog", N("j/k choose playlist · Enter confirm · Esc cancel")));
                break;
        }

        return rows;
    }

    private bool TryHandleMouseSeek(Message message)
    {
        var typeName = message.GetType().Name;
        var x = TryReadInt(message, "X", "Column", "Col");
        var y = TryReadInt(message, "Y", "Row", "Line");

        if (typeName == "MouseClickMsg")
        {
            if (x is null || y is null) return false;
            if (!_seekBar.TryGetRatioFromPoint(x.Value, y.Value, out var ratio))
                return false;
            _mouseSeekActive = true;
            _state.SeekToRatio(ratio);
            return true;
        }

        if (typeName == "MouseMotionMsg")
        {
            if (!_mouseSeekActive || x is null || y is null) return false;
            if (!_seekBar.TryGetRatioFromPoint(x.Value, y.Value, out var ratio))
                return false;
            _state.SeekToRatio(ratio);
            return true;
        }

        if (typeName == "MouseReleaseMsg")
        {
            if (!_mouseSeekActive) return false;
            _mouseSeekActive = false;
            if (x is null || y is null) return true;
            if (_seekBar.TryGetRatioFromPoint(x.Value, y.Value, out var ratio))
                _state.SeekToRatio(ratio);
            return true;
        }

        return false;
    }

    private static int? TryReadInt(object source, params string[] names)
    {
        var t = source.GetType();
        foreach (var name in names)
        {
            var prop = t.GetProperty(name);
            if (prop is null) continue;
            if (prop.PropertyType != typeof(int)) continue;
            return (int?)prop.GetValue(source);
        }

        return null;
    }

    private static string FormatBrowserEntry(string entry, bool ascii)
    {
        var up = "^";
        var folder = "[DIR]";
        var music = "[MP3]";
        var file = "[FILE]";

        if (entry == "../") return $"{up} ../";
        if (entry.EndsWith("/", StringComparison.Ordinal)) return $"{folder} {entry}";
        if (entry.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) return $"{music} {entry}";
        return $"{file} {entry}";
    }

    private AlbumArtFrame? ResolveAlbumArt(LibraryTrack? track)
    {
        if (track is null) return null;
        if (_albumArtCache.TryGetValue(track.FilePath, out var cached))
            return cached;

        var extracted = AlbumArtExtractor.TryExtract(track.FilePath, 112);
        _albumArtCache[track.FilePath] = extracted;
        return extracted;
    }

    private string FormatStatBlock(IReadOnlyList<StatItem> stats) =>
        stats.Count == 0
            ? N("—")
            : N(string.Join('\n', stats.Select(static s => $"{s.Key,-11} {s.Value}")));

    private UiPalette CurrentUiPalette => UiPalettes[_uiPaletteIndex];

    private void CycleUiPalette()
    {
        _uiPaletteIndex = (_uiPaletteIndex + 1) % UiPalettes.Length;
        ApplyTheme();
        _state.NotifyStatus($"theme palette  {CurrentUiPalette.Name.ToLowerInvariant()}");
    }

    private sealed record UiSettings
    {
        public int ThemeIndex { get; init; }
        public bool ShowCommandDeck { get; init; } = true;
        public string VisualRenderMode { get; init; } = "ascii";
    }

    private void LoadUiPreferences()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return;
            var json = File.ReadAllText(_settingsPath);
            var settings = JsonSerializer.Deserialize<UiSettings>(json);
            if (settings is null) return;
            _uiPaletteIndex = Math.Clamp(settings.ThemeIndex, 0, UiPalettes.Length - 1);
            _showCommandDeck = settings.ShowCommandDeck;
            _visualRenderMode = string.Equals(settings.VisualRenderMode, "image", StringComparison.OrdinalIgnoreCase)
                ? VisualRenderMode.Image
                : VisualRenderMode.Ascii;
        }
        catch
        {
        }
    }

    private void SaveUiPreferences()
    {
        try
        {
            var settings = new UiSettings
            {
                ThemeIndex = _uiPaletteIndex,
                ShowCommandDeck = _showCommandDeck,
                VisualRenderMode = _visualRenderMode == VisualRenderMode.Image ? "image" : "ascii"
            };
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_settingsPath, json);
        }
        catch
        {
        }
    }
}

internal sealed record TickMessage : Message;
