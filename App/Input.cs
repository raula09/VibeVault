using Tessera;

namespace VibeVault;

internal sealed partial class VibeVaultApp
{
    private TesseraEffect? HandleKey(KeyPressed key)
    {
        if (key.IsCharacter('c', ModifierKeys.Ctrl)) return TesseraEffects.Quit;

        if (HandleVisualizerInput(key)) return null;

        if (HandleDialogInput(key)) return null;

        if ((_state.View == AppView.Library || _state.View == AppView.Playlists) && HandleSearchInput(key))
            return null;

        if (HandleBrowserInput(key)) return null;
        if (HandleGlobalViewShortcuts(key)) return null;
        if (HandleGlobalPlaybackShortcuts(key)) return null;
        if (HandleLibraryInput(key)) return null;
        if (HandlePlaylistInput(key)) return null;

        return null;
    }

    private bool HandleVisualizerInput(KeyPressed key)
    {
        return TryExecuteKeyBindings(
            key,
            (static k => k.IsCharacter('v'), ToggleVisualizerView),
            (k => _state.View == AppView.Visualizer && k.Is(Key.Escape), ExitVisualizerView),
            (k => _state.View == AppView.Visualizer && k.IsCharacter('i'), ToggleVisualizerRenderMode));
    }

    private bool HandleGlobalViewShortcuts(KeyPressed key)
    {
        return TryExecuteKeyBindings(
            key,
            (static k => k.Is(Key.F1) || k.IsCharacter('1'), () => SwitchToView(AppView.Library)),
            (static k => k.Is(Key.F2) || k.IsCharacter('2'), () => SwitchToView(AppView.Playlists)),
            (static k => k.Is(Key.F4) || k.IsCharacter('4'), _state.OpenBrowser));
    }

    private bool HandleGlobalPlaybackShortcuts(KeyPressed key)
    {
        return TryExecuteKeyBindings(
            key,
            (static k => k.IsCharacter(' '), _state.TogglePlayPause),
            (static k => k.IsCharacter('c'), CycleUiPalette),
            (static k => k.IsCharacter('?'), ToggleCommandDeck),
            (static k => k.IsCharacter('`'), ToggleActivityFeed),
            (static k => k.IsCharacter('+') || k.IsCharacter('='), () => _state.AdjustVolume(+5)),
            (static k => k.IsCharacter('-') || k.IsCharacter('_'), () => _state.AdjustVolume(-5)),
            (k => k.IsCharacter('n') && _state.View != AppView.Playlists, _state.PlayNext),
            (static k => k.IsCharacter('p'), _state.PlayPrevious),
            (static k => k.IsCharacter('s'), _state.ToggleShuffle),
            (static k => k.Is(Key.Left), () => _state.SeekBy(-5)),
            (static k => k.Is(Key.Right), () => _state.SeekBy(5)));
    }

    private bool HandleDialogInput(KeyPressed key)
    {
        switch (_state.View)
        {
            case AppView.NewPlaylist:
                if (TryExecuteKeyBindings(
                        key,
                        (static k => k.Is(Key.Enter), _state.ConfirmNewPlaylist),
                        (static k => k.Is(Key.Escape), () => _state.SwitchView(AppView.Playlists)),
                        (static k => k.Is(Key.Backspace), _state.NewPlaylistBackspace)))
                {
                    return true;
                }

                if (TryGetTypedChar(key, out char newPlaylistChar))
                    _state.NewPlaylistAppendChar(newPlaylistChar);
                return true;
            case AppView.GoogleDriveImport:
                if (TryExecuteKeyBindings(
                        key,
                        (static k => k.Is(Key.Enter), _state.ConfirmGoogleDriveImport),
                        (static k => k.Is(Key.Escape), _state.CancelGoogleDriveImportDialog),
                        (static k => k.Is(Key.Backspace), _state.GoogleDriveLinkBackspace)))
                {
                    return true;
                }

                if (TryGetTypedChar(key, out char googleDriveChar))
                    _state.GoogleDriveLinkAppendChar(googleDriveChar);
                return true;
            case AppView.AddToPlaylist:
                _ = TryExecuteKeyBindings(
                    key,
                    (static k => k.Is(Key.Escape), _state.CancelAddToPlaylistDialog),
                    (static k => k.Is(Key.Enter), _state.ConfirmAddToPlaylist),
                    (static k => k.Is(Key.Up) || k.IsCharacter('k'), () => _state.MoveAddToPlaylistSelection(-1)),
                    (static k => k.Is(Key.Down) || k.IsCharacter('j'), () => _state.MoveAddToPlaylistSelection(1)));
                return true;
            case AppView.DeletePlaylistConfirm:
                _ = TryExecuteKeyBindings(
                    key,
                    (static k => k.Is(Key.Escape), _state.CancelDeletePlaylistDialog),
                    (static k => k.Is(Key.Enter), _state.ConfirmDeletePlaylist));
                return true;
            default:
                return false;
        }
    }

    private bool HandleBrowserInput(KeyPressed key)
    {
        if (_state.View != AppView.Browser) return false;

        _ = TryExecuteKeyBindings(
            key,
            (static k => k.Is(Key.Escape), () => _state.SwitchView(AppView.Library)),
            (static k => k.IsCharacter('g'), _state.StartGoogleDriveImportDialog),
            (static k => k.IsCharacter(' ', ModifierKeys.Ctrl), () => _state.ToggleBrowserSelectionAtCursor(true)),
            (static k => k.IsCharacter(' '), () => _state.ToggleBrowserSelectionAtCursor(false)),
            (static k => k.Is(Key.Up, ModifierKeys.Shift), () => _state.MoveBrowserSelection(-1, extendSelection: true)),
            (static k => k.Is(Key.Down, ModifierKeys.Shift), () => _state.MoveBrowserSelection(1, extendSelection: true)),
            (static k => k.IsCharacter('K') || k.IsCharacter('J'), () => _state.MoveBrowserSelection(key.IsCharacter('K') ? -1 : 1, extendSelection: true)),
            (static k => k.Is(Key.Up) || k.IsCharacter('k'), () => _state.MoveBrowserSelection(-1)),
            (static k => k.Is(Key.Down) || k.IsCharacter('j'), () => _state.MoveBrowserSelection(1)),
            (static k => k.Is(Key.Enter), _state.BrowserActivate),
            (static k => k.Is(Key.Backspace), _state.NavigateUp));

        return true;
    }

    private bool HandleLibraryInput(KeyPressed key)
    {
        if (_state.View != AppView.Library) return false;

        return TryExecuteKeyBindings(
            key,
            (static k => k.IsCharacter(' ', ModifierKeys.Ctrl), () => _state.ToggleLibrarySelectionAtCursor(true)),
            (static k => k.Is(Key.Up, ModifierKeys.Shift), () => _state.MoveLibrarySelection(-1, extendSelection: true)),
            (static k => k.Is(Key.Down, ModifierKeys.Shift), () => _state.MoveLibrarySelection(1, extendSelection: true)),
            (static k => k.IsCharacter('K') || k.IsCharacter('J'), () => _state.MoveLibrarySelection(key.IsCharacter('K') ? -1 : 1, extendSelection: true)),
            (static k => k.Is(Key.Up) || k.IsCharacter('k'), () => _state.MoveLibrarySelection(-1)),
            (static k => k.Is(Key.Down) || k.IsCharacter('j'), () => _state.MoveLibrarySelection(1)),
            (static k => k.Is(Key.Enter), _state.CueLibrarySelected),
            (static k => k.IsCharacter('q') || k.IsCharacter('Q'), _state.EnqueueLibrarySelection),
            (static k => k.IsCharacter('a') || k.IsCharacter('A'), _state.StartAddToPlaylistDialog),
            (static k => k.Is(Key.Delete) || k.IsCharacter('d'), _state.DeleteLibrarySelected));
    }

    private bool HandlePlaylistInput(KeyPressed key)
    {
        if (_state.View != AppView.Playlists) return false;

        if (TryExecuteKeyBindings(
                key,
                (static k => k.Is(Key.Tab) || k.IsCharacter('l') || k.IsCharacter('h'), TogglePlaylistPaneFocus)))
        {
            return true;
        }

        if (_playlistTracks.IsFocused)
        {
            _ = TryExecuteKeyBindings(
                key,
                (static k => k.Is(Key.Up) || k.IsCharacter('k'), () => _state.MovePlaylistTrackSelection(-1)),
                (static k => k.Is(Key.Down) || k.IsCharacter('j'), () => _state.MovePlaylistTrackSelection(1)),
                (static k => k.Is(Key.Enter), _state.CuePlaylistTrack),
                (static k => k.IsCharacter('q') || k.IsCharacter('Q'), _state.EnqueuePlaylistTrackSelected),
                (static k => k.IsCharacter('a') || k.IsCharacter('A'), _state.StartAddToPlaylistDialog),
                (static k => k.IsCharacter('r'), _state.RemovePlaylistTrackSelected),
                (static k => k.IsCharacter('N'), _state.PlayNext));
            return true;
        }

        return TryExecuteKeyBindings(
            key,
            (static k => k.IsCharacter('q') || k.IsCharacter('Q'), _state.EnqueuePlaylistTrackSelected),
            (static k => k.IsCharacter('a') || k.IsCharacter('A'), _state.StartAddToPlaylistDialog),
            (static k => k.Is(Key.Up) || k.IsCharacter('k'), () => _state.MovePlaylistPanel(-1)),
            (static k => k.Is(Key.Down) || k.IsCharacter('j'), () => _state.MovePlaylistPanel(1)),
            (static k => k.Is(Key.Enter), OpenPlaylistAndFocusTracks),
            (static k => k.IsCharacter('n'), _state.StartNewPlaylist),
            (static k => k.IsCharacter('r'), _state.RemovePlaylistTrackSelected),
            (static k => k.IsCharacter('D'), _state.StartDeletePlaylistDialog));
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

    private bool TryExecuteKeyBindings(
        KeyPressed key,
        params (Func<KeyPressed, bool> Match, Action Execute)[] bindings)
    {
        foreach (var binding in bindings)
        {
            if (!binding.Match(key)) continue;
            binding.Execute();
            return true;
        }

        return false;
    }

    private void ToggleVisualizerRenderMode()
    {
        _visualRenderMode = _visualRenderMode == VisualRenderMode.Ascii
            ? VisualRenderMode.Image
            : VisualRenderMode.Ascii;
        SaveUiPreferences();
        _state.NotifyStatus(_visualRenderMode == VisualRenderMode.Ascii
            ? "visual render: ascii"
            : "visual render: image");
    }

    private void ToggleCommandDeck()
    {
        _showCommandDeck = !_showCommandDeck;
        SaveUiPreferences();
        _state.NotifyStatus(_showCommandDeck ? "controls panel shown" : "controls panel hidden");
    }

    private void ToggleActivityFeed()
    {
        _showActivityFeed = !_showActivityFeed;
        SaveUiPreferences();
        _state.NotifyStatus(_showActivityFeed ? "execution lane shown" : "execution lane hidden");
    }

    private void TogglePlaylistPaneFocus()
    {
        if (_playlistTracks.IsFocused) _playlistPanel.RequestFocus();
        else _playlistTracks.RequestFocus();
    }

    private void OpenPlaylistAndFocusTracks()
    {
        _state.SelectPlaylist();
        if (_state.PlaylistTracks.Count > 0)
            _playlistTracks.RequestFocus();
    }

    private void ToggleVisualizerView()
    {
        if (_state.View == AppView.NewPlaylist || _state.View == AppView.AddToPlaylist || _state.View == AppView.GoogleDriveImport || _state.View == AppView.DeletePlaylistConfirm)
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
        {
            if (key.Is(Key.Up))
            {
                if (_state.View == AppView.Library) _state.MoveLibrarySelection(-1);
                else if (_state.View == AppView.Playlists)
                {
                    if (_playlistTracks.IsFocused) _state.MovePlaylistTrackSelection(-1);
                    else _state.MovePlaylistPanel(-1);
                }
            }
            else if (key.Is(Key.Down))
            {
                if (_state.View == AppView.Library) _state.MoveLibrarySelection(1);
                else if (_state.View == AppView.Playlists)
                {
                    if (_playlistTracks.IsFocused) _state.MovePlaylistTrackSelection(1);
                    else _state.MovePlaylistPanel(1);
                }
            }

            return true;
        }

        if (TryGetTypedChar(key, out var ch))
        {
            _state.AppendSearchChar(ch);
            return true;
        }

        return false;
    }
}
