using Tessera;

namespace VibeVault;

internal sealed partial class 
    VibeVaultApp
{
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

        if (HandleDialogInput(key)) return null;

        if ((_state.View == AppView.Library || _state.View == AppView.Playlists) && HandleSearchInput(key))
            return null;

        if (HandleBrowserInput(key)) return null;
        if (key.Is(Key.F1)) { SwitchToView(AppView.Library); return null; }
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
        if (key.IsCharacter('`'))
        {
            _showActivityFeed = !_showActivityFeed;
            SaveUiPreferences();
            _state.NotifyStatus(_showActivityFeed ? "execution lane shown" : "execution lane hidden");
            return null;
        }
        if (key.IsCharacter('n') && _state.PendingQueueCount > 0) { _state.PlayNext(); return null; }
        if (key.IsCharacter('+') || key.IsCharacter('=')) { _state.AdjustVolume(+5); return null; }
        if (key.IsCharacter('-') || key.IsCharacter('_')) { _state.AdjustVolume(-5); return null; }
        if (key.IsCharacter('n') && _state.View != AppView.Playlists) { _state.PlayNext(); return null; }
        if (key.IsCharacter('p')) { _state.PlayPrevious(); return null; }
        if (key.IsCharacter('s')) { _state.ToggleShuffle(); return null; }
        if (key.Is(Key.Left)) { _state.SeekBy(-5); return null; }
        if (key.Is(Key.Right)) { _state.SeekBy(5); return null; }
        if (HandleLibraryInput(key)) return null;
        if (HandlePlaylistInput(key)) return null;

        return null;
    }

    private bool HandleDialogInput(KeyPressed key)
    {
        if (_state.View == AppView.NewPlaylist)
        {
            if (key.Is(Key.Enter)) { _state.ConfirmNewPlaylist(); return true; }
            if (key.Is(Key.Escape)) { _state.SwitchView(AppView.Playlists); return true; }
            if (key.Is(Key.Backspace)) { _state.NewPlaylistBackspace(); return true; }
            if (TryGetTypedChar(key, out var ch))
                _state.NewPlaylistAppendChar(ch);
            return true;
        }

        if (_state.View == AppView.GoogleDriveImport)
        {
            if (key.Is(Key.Enter)) { _state.ConfirmGoogleDriveImport(); return true; }
            if (key.Is(Key.Escape)) { _state.CancelGoogleDriveImportDialog(); return true; }
            if (key.Is(Key.Backspace)) { _state.GoogleDriveLinkBackspace(); return true; }
            if (TryGetTypedChar(key, out var ch))
                _state.GoogleDriveLinkAppendChar(ch);
            return true;
        }

        if (_state.View == AppView.AddToPlaylist)
        {
            if (key.Is(Key.Escape)) { _state.CancelAddToPlaylistDialog(); return true; }
            if (key.Is(Key.Enter)) { _state.ConfirmAddToPlaylist(); return true; }
            if (key.Is(Key.Up) || key.IsCharacter('k')) { _state.MoveAddToPlaylistSelection(-1); return true; }
            if (key.Is(Key.Down) || key.IsCharacter('j')) { _state.MoveAddToPlaylistSelection(1); return true; }
            return true;
        }

        return false;
    }

    private bool HandleBrowserInput(KeyPressed key)
    {
        if (_state.View != AppView.Browser) return false;

        if (key.Is(Key.Escape)) { _state.SwitchView(AppView.Library); return true; }
        if (key.IsCharacter('g')) { _state.StartGoogleDriveImportDialog(); return true; }
        if (key.IsCharacter(' ', ModifierKeys.Ctrl)) { _state.ToggleBrowserSelectionAtCursor(true); return true; }
        if (key.IsCharacter(' ')) { _state.ToggleBrowserSelectionAtCursor(false); return true; }
        if (key.Is(Key.Up, ModifierKeys.Shift)) { _state.MoveBrowserSelection(-1, extendSelection: true); return true; }
        if (key.Is(Key.Down, ModifierKeys.Shift)) { _state.MoveBrowserSelection(1, extendSelection: true); return true; }
        if (key.IsCharacter('K') || key.IsCharacter('J'))
        {
            _state.MoveBrowserSelection(key.IsCharacter('K') ? -1 : 1, extendSelection: true);
            return true;
        }
        if (key.Is(Key.Up) || key.IsCharacter('k')) { _state.MoveBrowserSelection(-1); return true; }
        if (key.Is(Key.Down) || key.IsCharacter('j')) { _state.MoveBrowserSelection(1); return true; }
        if (key.Is(Key.Enter)) { _state.BrowserActivate(); return true; }
        if (key.Is(Key.Backspace)) { _state.NavigateUp(); return true; }
        return true;
    }

    private bool HandleLibraryInput(KeyPressed key)
    {
        if (_state.View != AppView.Library) return false;

        if (key.IsCharacter(' ', ModifierKeys.Ctrl)) { _state.ToggleLibrarySelectionAtCursor(true); return true; }
        if (key.Is(Key.Up, ModifierKeys.Shift)) { _state.MoveLibrarySelection(-1, extendSelection: true); return true; }
        if (key.Is(Key.Down, ModifierKeys.Shift)) { _state.MoveLibrarySelection(1, extendSelection: true); return true; }
        if (key.IsCharacter('K') || key.IsCharacter('J'))
        {
            _state.MoveLibrarySelection(key.IsCharacter('K') ? -1 : 1, extendSelection: true);
            return true;
        }
        if (key.Is(Key.Up) || key.IsCharacter('k')) { _state.MoveLibrarySelection(-1); return true; }
        if (key.Is(Key.Down) || key.IsCharacter('j')) { _state.MoveLibrarySelection(1); return true; }
        if (key.Is(Key.Enter)) { _state.CueLibrarySelected(); return true; }
        if (key.IsCharacter('q') || key.IsCharacter('Q')) { _state.EnqueueLibrarySelection(); return true; }
        if (key.IsCharacter('a')) { _state.StartAddToPlaylistDialog(); return true; }
        if (key.Is(Key.Delete) || key.IsCharacter('d')) { _state.DeleteLibrarySelected(); return true; }
        return false;
    }

    private bool HandlePlaylistInput(KeyPressed key)
    {
        if (_state.View != AppView.Playlists) return false;

        if (key.Is(Key.Tab) || key.IsCharacter('l') || key.IsCharacter('h'))
        {
            if (_playlistTracks.IsFocused) _playlistPanel.RequestFocus();
            else _playlistTracks.RequestFocus();
            return true;
        }

        if (_playlistTracks.IsFocused)
        {
            if (key.Is(Key.Up) || key.IsCharacter('k')) { _state.MovePlaylistTrackSelection(-1); return true; }
            if (key.Is(Key.Down) || key.IsCharacter('j')) { _state.MovePlaylistTrackSelection(1); return true; }
            if (key.Is(Key.Enter)) { _state.CuePlaylistTrack(); return true; }
            if (key.IsCharacter('q') || key.IsCharacter('Q')) { _state.EnqueuePlaylistTrackSelected(); return true; }
            if (key.IsCharacter('r')) { _state.RemovePlaylistTrackSelected(); return true; }
            if (key.IsCharacter('n')) { _state.PlayNext(); return true; }
            return true;
        }

        if (key.IsCharacter('q') || key.IsCharacter('Q')) { _state.EnqueuePlaylistTrackSelected(); return true; }
        if (key.Is(Key.Up) || key.IsCharacter('k')) { _state.MovePlaylistPanel(-1); return true; }
        if (key.Is(Key.Down) || key.IsCharacter('j')) { _state.MovePlaylistPanel(1); return true; }
        if (key.Is(Key.Enter)) { _state.SelectPlaylist(); return true; }
        if (key.IsCharacter('n'))
        {
            if (_state.PendingQueueCount > 0) _state.PlayNext();
            else _state.StartNewPlaylist();
            return true;
        }
        if (key.IsCharacter('r')) { _state.RemovePlaylistTrackSelected(); return true; }
        if (key.IsCharacter('D')) { _state.DeleteActivePlaylist(); return true; }
        return false;
    }

    private static bool TryGetTypedChar(KeyPressed key, out char ch)
    {
        const string printable =
            "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789 " +
            "-_.,:;!?\'\"()[]{}+/\\&@#$%^*=~`|<>";

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
        if (_state.View == AppView.NewPlaylist || _state.View == AppView.AddToPlaylist || _state.View == AppView.GoogleDriveImport)
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
}
