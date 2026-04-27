namespace VibeVault;

internal sealed partial class VibeVaultState
{
    private void Reload()
    {
        _library = _db.LoadAllTracks().ToList();
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

    private void SetStatus(string message)
    {
        StatusLine = message;
        _eventLog.Add($"{DateTime.Now:HH:mm:ss}  {message}");
        if (_eventLog.Count > 8)
            _eventLog.RemoveAt(0);
    }
}
