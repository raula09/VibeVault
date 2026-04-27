namespace VibeVault;

internal sealed partial class VibeVaultState
{
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
        var before = _librarySelected;
        _librarySelected = MoveWithinVisibleIndices(_librarySelected, delta, visible, _library.Count);

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
        _playlistTrackSelected = MoveWithinVisibleIndices(_playlistTrackSelected, delta, visible, _playlistTracks.Count);
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

    public void CueLibrarySelected()
    {
        var visible = BuildVisibleLibraryIndices();
        if (visible.Count == 0) return;

        if (!visible.Contains(_librarySelected))
            _librarySelected = visible[0];

        _queueFromPlaylist = false;
        PlayTrack(_library[_librarySelected]);
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

        _activePlaylistId = _playlists[_playlistPanelSelected].Id;
        _playlistTracks = _db.LoadPlaylistTracks(_activePlaylistId).ToList();
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
        if (string.IsNullOrEmpty(name))
        {
            View = AppView.Playlists;
            return;
        }

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

    public bool IsLibraryTrackMarked(int index) => _libraryMarked.Contains(index);

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

    private List<int> BuildLibrarySelectionForPlaylistAddIndices()
    {
        if (_library.Count == 0) return [];

        if (_libraryMarked.Count == 0)
            return [Math.Clamp(_librarySelected, 0, _library.Count - 1)];

        var selected = _libraryMarked
            .Where(i => i >= 0 && i < _library.Count)
            .OrderBy(i => i)
            .ToList();

        return selected.Count == 0
            ? [Math.Clamp(_librarySelected, 0, _library.Count - 1)]
            : selected;
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

        return selected.Count == 0
            ? [Math.Clamp(_librarySelected, 0, _library.Count - 1)]
            : selected;
    }
}
