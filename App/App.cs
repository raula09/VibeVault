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
        new("Twilight Orchid", 0x9387BF, 0xF6F2FF, 0xDED5F6, 0xACA3C8, 0xD0BEFF, 0xE0D8FF, 0x181327, 0xC4B1FF, 0x312949, 0x13261A, 0x9FE2BA, 0xEFE8FF, 0x5A4E79, 0xD5C6FF, 0x6F6592, 0xF3ECFF, 0xE4DCFF, 0xE1F2EA, 0xDDF0FF),
        new("Harbor Cyan", 0x79C4D4, 0xF1FDFF, 0xCAF1F8, 0x9ABDC3, 0xA2F0FF, 0xBDEFFF, 0x0B1D22, 0x95E4F5, 0x21424B, 0x10271D, 0xA0E3BF, 0xE5F8FC, 0x45707A, 0xA6EEFF, 0x4F7983, 0xE6FBFF, 0xC5F4FF, 0xDCF4EF, 0xD7F6FF),
        new("Amber Dusk", 0xD1A262, 0xFFF8EA, 0xF5DCB8, 0xC0A17A, 0xFFC98B, 0xFFE0A8, 0x28180A, 0xF2BA7D, 0x553820, 0x1A2A1D, 0xA7D99A, 0xFFF0DE, 0x74563E, 0xFFCC92, 0x83674F, 0xFFF2DB, 0xFFE3AE, 0xEAF3D8, 0xFFEBCB),
        new("Forest Glass", 0x78B695, 0xF1FFF6, 0xCDF3E1, 0x9ABCAC, 0xA9EFC8, 0xC9F1D8, 0x0C2218, 0x98E0B9, 0x234632, 0x142A1F, 0xA8E7C1, 0xE8FBF2, 0x4A6D5A, 0xACEFCB, 0x557564, 0xEAFBF2, 0xCFF3DD, 0xDCF4E8, 0xD6FFE9),
        new("Rose Clay", 0xCE95AE, 0xFFF6FA, 0xF5D8E6, 0xBF9CAB, 0xF5BED5, 0xF0CDE0, 0x2A1520, 0xEAB2CA, 0x523140, 0x21281B, 0xC2DFB1, 0xFDECF4, 0x6D4B5B, 0xF6C2D8, 0x7A6070, 0xFEEEF5, 0xF3D4E5, 0xF7E4EC, 0xFFDDED),
        new("Slate Frost", 0xA5BAC8, 0xFAFDFF, 0xDFEAF1, 0xAFC0CD, 0xD8E8F8, 0xE3EEFF, 0x121A21, 0xCCDDF0, 0x34414C, 0x1A2A22, 0xB9DFCC, 0xF1F7FB, 0x5A6876, 0xDDEBFA, 0x677685, 0xF3F8FD, 0xE7F0FF, 0xE8F0F7, 0xE4F3FF),
        new("Lagoon Mint", 0x7ACDBA, 0xF2FFFB, 0xCBF4EA, 0x9DC6BC, 0x99F5DE, 0xBDF8EA, 0x0B211B, 0x8BE7CF, 0x21463B, 0x11281F, 0xA8EFCB, 0xE6FCF4, 0x46705F, 0x9CF6E0, 0x4F7C69, 0xE7FFF7, 0xC8FAED, 0xD8F5EA, 0xD4FFF3),
        new("Sunset Coral", 0xDA8E83, 0xFFF5F2, 0xF7D0C8, 0xC7A09A, 0xFFB4A7, 0xFFC7BC, 0x2A1512, 0xF4A295, 0x57322D, 0x20281A, 0xBEE2B0, 0xFFE8E3, 0x77524C, 0xFFBAAD, 0x84605A, 0xFFF0EB, 0xFFD3CA, 0xF8E5E0, 0xFFDCD5),
        new("Aurora Lime", 0x9DCB74, 0xF8FFE9, 0xDCF2BC, 0xB0C794, 0xC7F59B, 0xDBFFC2, 0x18220D, 0xB9E98C, 0x394E24, 0x13281B, 0xACE8C6, 0xEFFBDD, 0x5F7B45, 0xCAF6A1, 0x6C8751, 0xF3FEE3, 0xE0FFC5, 0xE6F5DB, 0xDEFFD5)
    ];


    private readonly VibeVaultDb    _db;
    private readonly IAudioPlayer   _audio;
    private readonly VibeVaultState _state;
    private readonly string _settingsPath;
    private readonly TerminalGlyphProfile _glyphProfile = TerminalGlyphProfile.Detect();
    private bool _mouseSeekActive;
    private bool _showCommandDeck = true;
    private bool _showActivityFeed = false;
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
            case PointerInput pointer:
                return HandlePointerInput(pointer) ? null : null;
            case TickMessage:
                _state.Tick();
                _visualFrameCounter++;
                return null;
            default:             return TryHandleMouseSeek(message) ? null : null;
        }
    }

    private bool HandlePointerInput(PointerInput pointer)
    {
        switch (pointer.Kind)
        {
            case PointerEventKind.Wheel:
                if (pointer.Button == PointerButton.WheelUp)
                    return HandleMouseScroll(pointer.X, pointer.Y, +1);
                if (pointer.Button == PointerButton.WheelDown)
                    return HandleMouseScroll(pointer.X, pointer.Y, -1);
                return false;

            case PointerEventKind.Press:
                if (TryHandleSegmentTap(pointer.X, pointer.Y))
                    return true;
                if (TrySelectListItemAtPoint(pointer.X, pointer.Y))
                    return true;
                if (_seekBar.TryGetRatioFromPoint(pointer.X, pointer.Y, out var ratio))
                {
                    _mouseSeekActive = true;
                    _state.SeekToRatio(ratio);
                    return true;
                }
                return false;

            case PointerEventKind.Motion:
                if (!_mouseSeekActive) return false;
                if (_seekBar.TryGetRatioFromPoint(pointer.X, pointer.Y, out var dragRatio))
                {
                    _state.SeekToRatio(dragRatio);
                    return true;
                }
                return false;

            case PointerEventKind.Release:
                if (_mouseSeekActive)
                {
                    _mouseSeekActive = false;
                    if (_seekBar.TryGetRatioFromPoint(pointer.X, pointer.Y, out var releaseRatio))
                        _state.SeekToRatio(releaseRatio);
                    return true;
                }

                // Touchpad taps may arrive as release-only pointer events.
                if (pointer.ClickCount > 0 && TrySelectListItemAtPoint(pointer.X, pointer.Y))
                    return true;
                return false;

            default:
                return false;
        }
    }

    private bool TryHandleSegmentTap(int x, int y)
    {
        if (_workspaceTabs.TryGetSegmentIndexAtPoint(x, y, out var workspaceIndex))
        {
            switch (workspaceIndex)
            {
                case 0: SwitchToView(AppView.Library); return true;
                case 1: SwitchToView(AppView.Playlists); return true;
                case 2: _state.OpenBrowser(); return true;
                case 3: ToggleVisualizerView(); return true;
            }
        }

        if (_modeChips.TryGetSegmentIndexAtPoint(x, y, out var modeIndex))
        {
            switch (modeIndex)
            {
                case 0:
                    _state.TogglePlayPause();
                    return true;
                case 1:
                    _state.ToggleShuffle();
                    return true;
                case 2:
                    CycleUiPalette();
                    return true;
                case 3:
                    ToggleVisualizerView();
                    return true;
                case 4:
                    ToggleVisualizerRenderMode();
                    return true;
                case 5:
                    ToggleCommandDeck();
                    return true;
                case 6:
                    ToggleActivityFeed();
                    return true;
                default:
                    return false;
            }
        }

        return false;
    }

    public override Screen Build(ScreenContext context)
    {
        RefreshControls();
        return Screen.Build(window =>
        {
            window.Padding(1);
            window.Gap(1);
            window.Body(body => ConfigureBody(body, context));
        });
    }

    private void RefreshControls()
    {
        LibraryTrack? track = _state.NowPlaying;
        RefreshPlaybackCards(track);
        RefreshListPanels();
        RefreshStatsAndChips(track);
        RefreshSearchAndDialogViews();
    }

    private void RefreshPlaybackCards(LibraryTrack? track)
    {
        _nowPlaying.TrackTitle = track is null ? N("— nothing playing —") : track.Title.ToUpperInvariant();
        _nowPlaying.ArtistLine = track?.Artist ?? string.Empty;
        _nowPlaying.AlbumLine = track?.Album ?? string.Empty;
        _nowPlaying.StatusChip = N(_state.IsPlaying ? "▶ playing" : "▌▌paused");
        _nowPlaying.ShuffleChip = N(_state.ShuffleOn ? "⇌ shuffle" : "→ linear");
        _nowPlaying.PlaylistChip = _state.ActivePlaylist?.Name ?? "library";
        _nowPlaying.ProgressLine = _state.ProgressText;
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
        _albumArtVisualizer.TimingLine = _state.ProgressText;
        _albumArtVisualizer.MetaLine =
            $"{(_state.IsPlaying ? "LIVE" : "PAUSED")}{Sep}vol {_state.VolumePercent}{Sep}{_state.AudioBackend}";
        _albumArtVisualizer.Loudness = _state.CurrentLoudnessLevel;
        _albumArtVisualizer.AnimationFrame = _visualFrameCounter;
        _albumArtVisualizer.RenderMode = _visualRenderMode;
    }

    private void RefreshListPanels()
    {
        IReadOnlyList<int> visibleLibraryIndices = _state.BuildVisibleLibrarySourceIndices();
        _libraryList.SetItems(visibleLibraryIndices.Select(index =>
        {
            LibraryTrack track = _state.Library[index];
            string mark = _state.IsLibraryTrackMarked(index) ? "[x]" : "[ ]";
            return new ScrollListControl.ListItem($"{mark} {track.Artist}  {N("–")}  {track.Title}", track.DisplayDuration);
        }).ToArray());
        _libraryList.Title = _state.LibraryMarkedCount > 0
            ? $"Library{Sep}F1{Sep}selected {_state.LibraryMarkedCount}"
            : $"Library{Sep}F1";
        _libraryList.SelectedIndex = _state.BuildVisibleLibrarySelectedIndex();
        _libraryList.CurrentIndex = _state.BuildVisibleLibraryCurrentIndex();

        _playlistPanel.SetItems(_state.Playlists.Select(static p =>
            new ScrollListControl.ListItem(p.Name)).ToArray());
        _playlistPanel.SelectedIndex = _state.PlaylistPanelSelectedIndex;
        _playlistPanel.CurrentIndex = _state.Playlists.ToList()
            .FindIndex(p => p.Id == _state.ActivePlaylist?.Id);

        IReadOnlyList<LibraryTrack> visiblePlaylistTracks = _state.BuildVisiblePlaylistTracks();
        _playlistTracks.SetItems(visiblePlaylistTracks.Select(track =>
            new ScrollListControl.ListItem($"{track.Artist}  {N("–")}  {track.Title}", track.DisplayDuration)).ToArray());
        _playlistTracks.SelectedIndex = _state.BuildVisiblePlaylistSelectedIndex();
        _playlistTracks.CurrentIndex = _state.BuildVisiblePlaylistCurrentIndex();

        _browserList.SetItems(_state.BrowserEntries.Select((entry, index) =>
        {
            bool selectable = _state.IsBrowserEntrySelectableForImport(index);
            string mark = selectable ? (_state.IsBrowserEntryMarked(index) ? "[x]" : "[ ]") : "   ";
            return new ScrollListControl.ListItem($"{mark} {FormatBrowserEntry(entry)}");
        }).ToArray());
        _browserList.SelectedIndex = _state.BrowserSelectedIndex;
        _browserList.Title = $"Import{Sep}{_state.BrowserPath}{Sep}selected {_state.BrowserMarkedCount}";

        _addToPlaylistList.SetItems(_state.Playlists.Select(static p =>
            new ScrollListControl.ListItem(p.Name)).ToArray());
        _addToPlaylistList.SelectedIndex = _state.AddToPlaylistSelectedIndex;
    }

    private void RefreshStatsAndChips(LibraryTrack? track)
    {
        _playerStats.Text = FormatStatBlock(_state.BuildNowPlayingStats());
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
            $"Queue    {_state.ActivePlaylist?.Name ?? "library"}  (next {_state.PendingQueueCount})\n" +
            $"Time     {_state.ProgressText}  {_state.RemainingText}");
        _visualizerCard.Text = "\n" + N(_state.VisualizerLine);
        _activityFeed.Text = _state.RecentEvents.Count == 0
            ? N("— no activity yet —")
            : N(string.Join('\n', _state.RecentEvents.Select(static e => $"OUT  {e}")));

        if (_showCommandDeck)
            _commandDeckCard.SetRows(BuildCommandRows());

        _workspaceTabs.SetSegments(BuildWorkspaceSegments());
        _modeChips.SetSegments(BuildModeSegments(track));
    }

    private void RefreshSearchAndDialogViews()
    {
        string searchScope = _state.View == AppView.Playlists ? "playlist tracks" : "library tracks";
        int visibleMatches = _state.View == AppView.Playlists ? _state.VisiblePlaylistTrackCount : _state.VisibleLibraryCount;
        string matchWord = visibleMatches == 1 ? "match" : "matches";
        string searchText = string.IsNullOrWhiteSpace(_state.SearchQuery)
            ? "type track / artist / album"
            : _state.SearchQuery;
        string caret = _state.IsSearchActive ? "_" : string.Empty;

        _searchBar.Title = _state.IsSearchActive
            ? $"Search{Sep}Ctrl+F editing{Sep}Enter done{Sep}Esc clear"
            : $"Search{Sep}Ctrl+F edit{Sep}Esc clear";
        _searchBar.Text = $"/ {searchText}{caret}  [{visibleMatches} {matchWord} in {searchScope}]";

        if (_state.View == AppView.NewPlaylist)
        {
            _dialogLabel.Title = "New Playlist";
            _dialogLabel.Text = $"> {_state.NewPlaylistName}_";
            return;
        }

        if (_state.View == AppView.GoogleDriveImport)
        {
            _dialogLabel.Title = "Google Drive Import";
            _dialogLabel.Text = "Paste shared folder link and press Enter";
            _searchBar.Title = "Google Drive Folder Link";
            _searchBar.Text = $"> {_state.GoogleDriveFolderLink}_";
        }
    }

    private string N(string text) => _glyphProfile.Normalize(text);

    private string Sep => _glyphProfile.UseAscii ? " | " : " · ";

    private void ApplyGlyphProfile()
    {
        _nowPlaying.HeaderTitle = N(_nowPlaying.HeaderTitle);
        _albumArtVisualizer.Title = N(_albumArtVisualizer.Title);
        _albumArtVisualizer.EmptyMessage = N(_albumArtVisualizer.EmptyMessage);
        _libraryList.Title = N(_libraryList.Title);
        _libraryList.EmptyMessage = N(_libraryList.EmptyMessage);
        _playlistPanel.Title = N(_playlistPanel.Title);
        _playlistPanel.EmptyMessage = N(_playlistPanel.EmptyMessage);
        _playlistTracks.EmptyMessage = N(_playlistTracks.EmptyMessage);
        _browserList.Title = N(_browserList.Title);
        _addToPlaylistList.EmptyMessage = N(_addToPlaylistList.EmptyMessage);

        if (!_glyphProfile.UseAscii && !_glyphProfile.UseLegacyUnicodeGlyphs) return;

        var focus = _glyphProfile.FocusMarker;
        var border = _glyphProfile.BorderStyle;
        var useAsciiGlyphs = _glyphProfile.UseAscii;
        var useLegacyUnicodeGlyphs = _glyphProfile.UseLegacyUnicodeGlyphs;

        _nowPlaying.Border = border;

        _seekBar.Border = border;
        _seekBar.FocusMarker = focus;
        _seekBar.UseAsciiGlyphs = useAsciiGlyphs;
        _seekBar.UseLegacyUnicodeGlyphs = useLegacyUnicodeGlyphs;

        _audioMeter.Border = border;
        _audioMeter.FocusMarker = focus;
        _audioMeter.UseAsciiGlyphs = useAsciiGlyphs;
        _audioMeter.UseLegacyUnicodeGlyphs = useLegacyUnicodeGlyphs;

        _albumArtVisualizer.Border = border;
        _albumArtVisualizer.FocusMarker = focus;
        _albumArtVisualizer.UseAsciiGlyphs = useAsciiGlyphs;

        if (_glyphProfile.UseAscii)
            ConfigureAllLists(border, focus, "*", ">", ".");
        else if (_glyphProfile.UseLegacyUnicodeGlyphs)
            ConfigureAllLists(border, focus, "●", "◆", "·");

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

    private void ConfigureAllLists(
        BorderStyle border,
        string focusMarker,
        string currentPrefix,
        string selectedPrefix,
        string itemPrefix)
    {
        ConfigureList(_libraryList, border, focusMarker, currentPrefix, selectedPrefix, itemPrefix);
        ConfigureList(_playlistPanel, border, focusMarker, currentPrefix, selectedPrefix, itemPrefix);
        ConfigureList(_playlistTracks, border, focusMarker, currentPrefix, selectedPrefix, itemPrefix);
        ConfigureList(_browserList, border, focusMarker, currentPrefix, selectedPrefix, itemPrefix);
        ConfigureList(_addToPlaylistList, border, focusMarker, currentPrefix, selectedPrefix, itemPrefix);
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
            new SegmentBarControl.Segment(_showActivityFeed ? "[` Hide Lane]" : "[` Show Lane]", off),
            new SegmentBarControl.Segment(_state.ActivePlaylist is null ? "[Scope Library]" : $"[Scope {_state.ActivePlaylist.Name}]", off),
            new SegmentBarControl.Segment($"[Backend {_state.AudioBackend}]", off),
            new SegmentBarControl.Segment(track is null ? "[Track none]" : $"[Track {track.Title}]", off)
        ];
    }

    private IReadOnlyList<CommandBoardControl.CommandRow> BuildCommandRows()
    {
        var rows = new List<CommandBoardControl.CommandRow>
        {
            new("Global", N("Space play/pause · n/p next-prev · s shuffle · q queue add")),
            new("Views", N("F1/F2/F4 switch · 1/2/4 quick switch · v cover visual"))
        };

        switch (_state.View)
        {
            case AppView.Library:
                rows.Add(new("Library", N("j/k move · Shift+Up/Down range · Ctrl+Space toggle · q queue add")));
                rows.Add(new("Library", N("Enter play · d delete · Ctrl+F search · Esc clear")));
                rows.Add(new("Search", N("Ctrl+F edit filter · Enter finish · Esc clear")));
                break;

            case AppView.Playlists:
                rows.Add(new("Lists", N("j/k move playlists · Enter open · n new · D delete (confirm)")));
                rows.Add(new("Tracks", N("Tab/l toggle panes · h move left · j/k move · Enter play · N next · q queue · a add · r remove")));
                rows.Add(new("Search", "Ctrl+F filters playlist tracks by title/artist/album"));
                break;

            case AppView.Browser:
                rows.Add(new("Import", N("j/k move · Enter open/import · Backspace up-dir · g google-drive link · Esc cancel")));
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

            case AppView.GoogleDriveImport:
                rows.Add(new("Dialog", "Paste public Google Drive folder link"));
                rows.Add(new("Dialog", N("Enter import mp3 · Esc cancel")));
                break;

            case AppView.DeletePlaylistConfirm:
                rows.Add(new("Dialog", N("Enter confirm delete · Esc cancel")));
                break;
        }

        return rows;
    }

    private bool TryHandleMouseSeek(Message message)
    {
        var x = TryReadInt(message, "X", "Column", "Col");
        var y = TryReadInt(message, "Y", "Row", "Line");
        var scrollDelta = TryReadScrollDelta(message);

        if (scrollDelta is not null && x is not null && y is not null)
            return HandleMouseScroll(x.Value, y.Value, scrollDelta.Value);

        if (x is not null && y is not null && IsMouseClickLike(message))
        {
            if (TrySelectListItemAtPoint(x.Value, y.Value))
                return true;
            if (!_seekBar.TryGetRatioFromPoint(x.Value, y.Value, out var ratio))
                return false;
            _mouseSeekActive = true;
            _state.SeekToRatio(ratio);
            return true;
        }

        if (IsMouseMotionLike(message))
        {
            if (!_mouseSeekActive || x is null || y is null) return false;
            if (!_seekBar.TryGetRatioFromPoint(x.Value, y.Value, out var ratio))
                return false;
            _state.SeekToRatio(ratio);
            return true;
        }

        if (IsMouseReleaseLike(message))
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

    private static bool IsMouseClickLike(object source)
    {
        var typeName = source.GetType().Name;
        if (typeName.Contains("Click", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Press", StringComparison.OrdinalIgnoreCase))
            return true;

        var eventType = ReadStringProperty(source, "EventType");
        if (string.IsNullOrWhiteSpace(eventType)) return false;
        return eventType.Contains("Click", StringComparison.OrdinalIgnoreCase) ||
               eventType.Contains("Press", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMouseMotionLike(object source)
    {
        var typeName = source.GetType().Name;
        if (typeName.Contains("Motion", StringComparison.OrdinalIgnoreCase) ||
            typeName.Contains("Move", StringComparison.OrdinalIgnoreCase))
            return true;

        var eventType = ReadStringProperty(source, "EventType");
        if (string.IsNullOrWhiteSpace(eventType)) return false;
        return eventType.Contains("Motion", StringComparison.OrdinalIgnoreCase) ||
               eventType.Contains("Move", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMouseReleaseLike(object source)
    {
        var typeName = source.GetType().Name;
        if (typeName.Contains("Release", StringComparison.OrdinalIgnoreCase))
            return true;

        var eventType = ReadStringProperty(source, "EventType");
        if (string.IsNullOrWhiteSpace(eventType)) return false;
        return eventType.Contains("Release", StringComparison.OrdinalIgnoreCase);
    }

    private bool HandleMouseScroll(int x, int y, int delta)
    {
        if (delta == 0) return false;
        var direction = delta > 0 ? -1 : 1;

        if (_state.View == AppView.Library && _libraryList.ContainsPoint(x, y))
        {
            _libraryList.RequestFocus();
            _state.MoveLibrarySelection(direction);
            return true;
        }

        if (_state.View == AppView.Playlists)
        {
            if (_playlistPanel.ContainsPoint(x, y))
            {
                _playlistPanel.RequestFocus();
                _state.MovePlaylistPanel(direction);
                return true;
            }

            if (_playlistTracks.ContainsPoint(x, y))
            {
                _playlistTracks.RequestFocus();
                _state.MovePlaylistTrackSelection(direction);
                return true;
            }
        }

        if (_state.View == AppView.Browser && _browserList.ContainsPoint(x, y))
        {
            _browserList.RequestFocus();
            _state.MoveBrowserSelection(direction);
            return true;
        }

        if (_state.View == AppView.AddToPlaylist && _addToPlaylistList.ContainsPoint(x, y))
        {
            _addToPlaylistList.RequestFocus();
            _state.MoveAddToPlaylistSelection(direction);
            return true;
        }

        return false;
    }

    private bool TrySelectListItemAtPoint(int x, int y)
    {
        if (_state.View == AppView.Library && _libraryList.ContainsPoint(x, y))
        {
            _libraryList.RequestFocus();
            if (_libraryList.TryGetItemIndexAtPoint(x, y, out var visibleIndex))
                _state.SetVisibleLibrarySelection(visibleIndex);
            return true;
        }

        if (_state.View == AppView.Playlists)
        {
            if (_playlistPanel.ContainsPoint(x, y))
            {
                _playlistPanel.RequestFocus();
                if (_playlistPanel.TryGetItemIndexAtPoint(x, y, out var playlistIndex))
                    _state.SetPlaylistPanelSelection(playlistIndex);
                return true;
            }

            if (_playlistTracks.ContainsPoint(x, y))
            {
                _playlistTracks.RequestFocus();
                if (_playlistTracks.TryGetItemIndexAtPoint(x, y, out var visibleIndex))
                    _state.SetVisiblePlaylistTrackSelection(visibleIndex);
                return true;
            }
        }

        if (_state.View == AppView.Browser && _browserList.ContainsPoint(x, y))
        {
            _browserList.RequestFocus();
            if (_browserList.TryGetItemIndexAtPoint(x, y, out var browserIndex))
                _state.SetBrowserSelection(browserIndex);
            return true;
        }

        if (_state.View == AppView.AddToPlaylist && _addToPlaylistList.ContainsPoint(x, y))
        {
            _addToPlaylistList.RequestFocus();
            if (_addToPlaylistList.TryGetItemIndexAtPoint(x, y, out var playlistIndex))
                _state.SetAddToPlaylistSelection(playlistIndex);
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

    private static string? ReadStringProperty(object source, string name)
    {
        var prop = source.GetType().GetProperty(name);
        if (prop is null) return null;
        var value = prop.GetValue(source);
        return value?.ToString();
    }

    private static int? TryReadScrollDelta(object source)
    {
        var t = source.GetType();
        var typeName = t.Name;
        if (!typeName.Contains("Scroll", StringComparison.OrdinalIgnoreCase) &&
            !typeName.Contains("Wheel", StringComparison.OrdinalIgnoreCase))
            return null;

        return TryReadInt(source, "Delta", "DeltaY", "ScrollDelta", "WheelDelta", "Amount");
    }

    private static string FormatBrowserEntry(string entry)
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
        public bool ShowActivityFeed { get; init; } = false;
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
            _showActivityFeed = settings.ShowActivityFeed;
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
                ShowActivityFeed = _showActivityFeed,
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
