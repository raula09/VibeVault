namespace VibeVault;

internal sealed partial class VibeVaultState
{
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

    public void SetBrowserSelection(int index)
    {
        _browserSelected = Math.Clamp(index, 0, Math.Max(0, _browserEntries.Count - 1));
        _browserRangeAnchor = _browserSelected;
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
        {
            if (IsBrowserEntrySelectable(i))
                _browserMarked.Add(i);
        }
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
}
