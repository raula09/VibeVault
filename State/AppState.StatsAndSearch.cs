using Tessera.Controls;

namespace VibeVault;

internal sealed partial class VibeVaultState
{
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
        var artistCount = _library
            .Select(t => t.Artist)
            .Where(static a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        var albumCount = _library
            .Select(t => t.Album)
            .Where(static a => !string.IsNullOrWhiteSpace(a))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

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

    private static int FindVisibleIndex(int fullIndex, IReadOnlyList<int> visibleIndices) =>
        fullIndex < 0 ? -1 : IndexOfValue(visibleIndices, fullIndex);

    private List<int> BuildVisibleLibraryIndices()
        => BuildVisibleIndices(_library);

    private List<int> BuildVisiblePlaylistTrackIndices()
        => BuildVisibleIndices(_playlistTracks);

    private List<int> BuildVisibleIndices(IReadOnlyList<LibraryTrack> source)
    {
        List<int> result = new List<int>(source.Count);
        for (var i = 0; i < source.Count; i++)
        {
            if (MatchesSearch(source[i], _searchQuery))
                result.Add(i);
        }

        return result;
    }

    private static int MoveWithinVisibleIndices(
        int selectedIndex,
        int delta,
        IReadOnlyList<int> visibleIndices,
        int sourceCount)
    {
        if (visibleIndices.Count == 0)
            return Math.Clamp(selectedIndex, 0, Math.Max(0, sourceCount - 1));

        var currentVisibleIndex = IndexOfValue(visibleIndices, selectedIndex);
        if (currentVisibleIndex < 0)
            currentVisibleIndex = 0;

        currentVisibleIndex = Math.Clamp(currentVisibleIndex + delta, 0, visibleIndices.Count - 1);
        return visibleIndices[currentVisibleIndex];
    }

    private static int IndexOfValue(IReadOnlyList<int> values, int value)
    {
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i] == value)
                return i;
        }

        return -1;
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
}
