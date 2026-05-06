using Terminal.Gui;

namespace VibeVault;

internal static class Program
{
    private readonly record struct UiTheme(
        string Name,
        Color BaseFg,
        Color BaseBg,
        Color FocusFg,
        Color FocusBg,
        Color HotFg,
        Color AccentFg,
        Color PanelFg,
        Color MutedFg);

    private static readonly UiTheme[] Themes =
    [
        new("Twilight Orchid", Color.BrightMagenta, Color.Black, Color.BrightYellow, Color.DarkGray, Color.BrightCyan, Color.BrightCyan, Color.BrightGreen, Color.DarkGray),
        new("Harbor Cyan", Color.BrightCyan, Color.Black, Color.BrightYellow, Color.DarkGray, Color.BrightBlue, Color.BrightBlue, Color.BrightCyan, Color.Gray),
        new("Amber Dusk", Color.BrightYellow, Color.Black, Color.White, Color.DarkGray, Color.BrightRed, Color.BrightYellow, Color.BrightRed, Color.Gray),
        new("Forest Glass", Color.BrightGreen, Color.Black, Color.BrightYellow, Color.DarkGray, Color.BrightCyan, Color.BrightGreen, Color.BrightGreen, Color.DarkGray),
        new("Rose Clay", Color.BrightMagenta, Color.Black, Color.BrightYellow, Color.DarkGray, Color.BrightRed, Color.BrightMagenta, Color.BrightRed, Color.Gray),
        new("Slate Frost", Color.Gray, Color.Black, Color.White, Color.DarkGray, Color.BrightCyan, Color.White, Color.BrightCyan, Color.DarkGray),
        new("Lagoon Mint", Color.BrightGreen, Color.Black, Color.BrightCyan, Color.DarkGray, Color.BrightGreen, Color.BrightCyan, Color.BrightGreen, Color.Gray),
        new("Sunset Coral", Color.BrightRed, Color.Black, Color.BrightYellow, Color.DarkGray, Color.BrightMagenta, Color.BrightRed, Color.BrightYellow, Color.Gray),
        new("Aurora Lime", Color.BrightGreen, Color.Black, Color.BrightYellow, Color.DarkGray, Color.White, Color.BrightYellow, Color.BrightGreen, Color.DarkGray),
        new("Mono Ice", Color.White, Color.Black, Color.Black, Color.Gray, Color.White, Color.White, Color.Gray, Color.DarkGray),
        new("Midnight Blue", Color.BrightBlue, Color.Black, Color.White, Color.DarkGray, Color.BrightCyan, Color.BrightBlue, Color.Cyan, Color.Gray),
        new("CRT Classic", Color.Green, Color.Black, Color.BrightGreen, Color.Black, Color.BrightGreen, Color.Green, Color.Green, Color.DarkGray)
    ];

    private static VibeVaultState? _state;
    private static int _paletteIndex;
    private static bool _showCommands = true;
    private static bool _showActivity;
    private static bool _visualImageMode;
    private static AppView _viewBeforeVisualizer = AppView.Library;
    private static bool _playlistTracksPaneActive;

    private static Window? _window;
    private static FrameView? _workspaceFrame;
    private static FrameView? _modesFrame;
    private static FrameView? _searchFrame;
    private static Label? _workspace;
    private static Label? _modes;
    private static Label? _search;
    private static FrameView? _libraryFrame;
    private static FrameView? _playlistPanelFrame;
    private static FrameView? _playlistTracksFrame;
    private static FrameView? _browserFrame;
    private static FrameView? _visualFrame;
    private static FrameView? _sessionFrame;
    private static FrameView? _trackLensFrame;
    private static FrameView? _playerFrame;
    private static FrameView? _libraryStatsFrame;
    private static FrameView? _soundLevelFrame;
    private static FrameView? _timelineFrame;
    private static FrameView? _meterFrame;
    private static ListView? _libraryList;
    private static ListView? _playlistPanelList;
    private static ListView? _playlistTracksList;
    private static ListView? _browserList;
    private static Label? _session;
    private static Label? _trackLens;
    private static Label? _playerStats;
    private static Label? _libraryStats;
    private static Label? _activity;
    private static Label? _commands;
    private static Label? _timeline;
    private static Label? _meter;
    private static Label? _visualText;
    private static Label? _soundLevelText;

    private static FrameView? _dialogFrame;
    private static Label? _dialogText;
    private static ListView? _dialogList;
    private static ColorScheme? _accentScheme;
    private static ColorScheme? _panelScheme;
    private static ColorScheme? _mutedScheme;

    private static void Main()
    {
        var appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VibeVault");
        Directory.CreateDirectory(appDataDir);

        var db = new VibeVaultDb(Path.Combine(appDataDir, "library.db"));
        var audio = AudioPlayerFactory.Create();
        _state = new VibeVaultState(db, audio);

        Application.Init();
        ApplyTheme();
        ApplyThemeVariant();
        BuildUi();
        RefreshUi();

        Application.Top.KeyPress += HandleTopLevelKey;

        Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(80), _ =>
        {
            _state.Tick();
            RefreshUi();
            return true;
        });

        Application.Run();
        _state.Dispose();
        Application.Shutdown();
    }

    private static void ApplyTheme()
    {
        var theme = Themes[Math.Clamp(_paletteIndex, 0, Themes.Length - 1)];
        var baseScheme = new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(theme.BaseFg, theme.BaseBg),
            Focus = Application.Driver.MakeAttribute(theme.FocusFg, theme.FocusBg),
            HotNormal = Application.Driver.MakeAttribute(theme.HotFg, theme.BaseBg),
            HotFocus = Application.Driver.MakeAttribute(theme.FocusFg, theme.FocusBg),
            Disabled = Application.Driver.MakeAttribute(theme.MutedFg, theme.BaseBg)
        };

        _accentScheme = new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(theme.AccentFg, theme.BaseBg),
            Focus = Application.Driver.MakeAttribute(theme.FocusFg, theme.FocusBg),
            HotNormal = Application.Driver.MakeAttribute(theme.HotFg, theme.BaseBg),
            HotFocus = Application.Driver.MakeAttribute(theme.FocusFg, theme.FocusBg),
            Disabled = Application.Driver.MakeAttribute(theme.MutedFg, theme.BaseBg)
        };

        _panelScheme = new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(theme.PanelFg, theme.BaseBg),
            Focus = Application.Driver.MakeAttribute(theme.FocusFg, theme.FocusBg),
            HotNormal = Application.Driver.MakeAttribute(theme.PanelFg, theme.BaseBg),
            HotFocus = Application.Driver.MakeAttribute(theme.FocusFg, theme.FocusBg),
            Disabled = Application.Driver.MakeAttribute(theme.MutedFg, theme.BaseBg)
        };

        _mutedScheme = new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(theme.MutedFg, theme.BaseBg),
            Focus = Application.Driver.MakeAttribute(theme.BaseFg, theme.BaseBg),
            HotNormal = Application.Driver.MakeAttribute(theme.BaseFg, theme.BaseBg),
            HotFocus = Application.Driver.MakeAttribute(theme.BaseFg, theme.BaseBg),
            Disabled = Application.Driver.MakeAttribute(theme.MutedFg, theme.BaseBg)
        };

        Colors.Base = baseScheme;
        Colors.Menu = baseScheme;
        Colors.Dialog = baseScheme;
        Colors.Error = new ColorScheme
        {
            Normal = Application.Driver.MakeAttribute(Color.BrightRed, Color.Black),
            Focus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.DarkGray),
            HotNormal = Application.Driver.MakeAttribute(Color.BrightYellow, Color.Black),
            HotFocus = Application.Driver.MakeAttribute(Color.BrightYellow, Color.DarkGray),
            Disabled = Application.Driver.MakeAttribute(Color.DarkGray, Color.Black)
        };
    }

    private static void ApplyThemeVariant()
    {
        ApplyTheme();
        if (_workspace is not null) _workspace.ColorScheme = _accentScheme;
        if (_modes is not null) _modes.ColorScheme = _accentScheme;
        if (_search is not null) _search.ColorScheme = _panelScheme;
        if (_meter is not null) _meter.ColorScheme = _accentScheme;
        if (_commands is not null) _commands.ColorScheme = _mutedScheme;
        if (_window is not null) _window.ColorScheme = Colors.Base;
    }

    private static void BuildUi()
    {
        _window = new Window("VibeVault · Terminal.Gui")
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill(),
            ColorScheme = Colors.Base
        };

        _workspaceFrame = new FrameView("Workspace") { X = 0, Y = 0, Width = Dim.Percent(50), Height = 3 };
        _modesFrame = new FrameView("Modes") { X = Pos.Right(_workspaceFrame), Y = 0, Width = Dim.Fill(), Height = 3 };
        _searchFrame = new FrameView("Search · Ctrl+F edit · Esc clear") { X = 0, Y = 3, Width = Dim.Fill(), Height = 3 };
        _workspace = NewLine(0);
        _modes = NewLine(0);
        _search = NewLine(0);
        _workspace.ColorScheme = _accentScheme;
        _modes.ColorScheme = _accentScheme;
        _search.ColorScheme = _panelScheme;
        _workspaceFrame.Add(_workspace);
        _modesFrame.Add(_modes);
        _searchFrame.Add(_search);

        _libraryFrame = new FrameView("Library · F1 ✦") { X = 0, Y = 6, Width = Dim.Percent(83), Height = Dim.Percent(48) };
        _libraryList = new ListView { Width = Dim.Fill(), Height = Dim.Fill(), CanFocus = false };
        _libraryFrame.Add(_libraryList);
        _libraryList.MouseClick += e => HandleListMouse(e, () => _state?.MoveLibrarySelection(-1), () => _state?.MoveLibrarySelection(1));

        _playlistPanelFrame = new FrameView("Playlists · F2") { X = 0, Y = 6, Width = 1, Height = 1, Visible = false };
        _playlistPanelList = new ListView { Width = Dim.Fill(), Height = Dim.Fill(), CanFocus = false };
        _playlistPanelFrame.Add(_playlistPanelList);
        _playlistPanelList.MouseClick += e => HandleListMouse(e, () => _state?.MovePlaylistPanel(-1), () => _state?.MovePlaylistPanel(1));

        _playlistTracksFrame = new FrameView("Playlist Tracks") { X = 0, Y = 6, Width = 1, Height = 1, Visible = false };
        _playlistTracksList = new ListView { Width = Dim.Fill(), Height = Dim.Fill(), CanFocus = false };
        _playlistTracksFrame.Add(_playlistTracksList);
        _playlistTracksList.MouseClick += e => HandleListMouse(e, () => _state?.MovePlaylistTrackSelection(-1), () => _state?.MovePlaylistTrackSelection(1));

        _browserFrame = new FrameView("Import · F4") { X = 0, Y = 6, Width = Dim.Fill(), Height = Dim.Percent(48), Visible = false };
        _browserList = new ListView { Width = Dim.Fill(), Height = Dim.Fill(), CanFocus = false };
        _browserFrame.Add(_browserList);
        _browserList.MouseClick += e => HandleListMouse(e, () => _state?.MoveBrowserSelection(-1), () => _state?.MoveBrowserSelection(1));

        _visualFrame = new FrameView("Cover Visual · v exits") { X = 0, Y = 6, Width = Dim.Fill(), Height = Dim.Percent(48), Visible = false };
        _visualText = new Label("") { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill() };
        _visualFrame.Add(_visualText);

        _sessionFrame = new FrameView("Session Pulse") { X = 0, Y = Pos.Bottom(_searchFrame), Width = Dim.Percent(12), Height = 6 };
        _trackLensFrame = new FrameView("Track Lens") { X = Pos.Right(_sessionFrame), Y = Pos.Bottom(_searchFrame), Width = Dim.Percent(68), Height = 6 };
        _soundLevelFrame = new FrameView("Sound Level") { X = Pos.Right(_trackLensFrame), Y = Pos.Bottom(_searchFrame), Width = Dim.Fill(), Height = 6 };
        _session = NewLine(0); _trackLens = NewLine(0); _soundLevelText = NewLine(0);
        _sessionFrame.Add(_session); _trackLensFrame.Add(_trackLens); _soundLevelFrame.Add(_soundLevelText);

        _playerFrame = new FrameView("Player") { X = Pos.Right(_libraryFrame), Y = Pos.Bottom(_sessionFrame), Width = Dim.Fill(), Height = Dim.Percent(24) };
        _libraryStatsFrame = new FrameView("Library") { X = Pos.Right(_libraryFrame), Y = Pos.Bottom(_playerFrame), Width = Dim.Fill(), Height = Dim.Percent(24) };
        _playerStats = NewLine(0); _libraryStats = NewLine(0);
        _playerFrame.Add(_playerStats); _libraryStatsFrame.Add(_libraryStats);

        _commands = new Label("") { X = 0, Y = Pos.Bottom(_libraryFrame), Width = Dim.Fill(), Height = 1, ColorScheme = _mutedScheme };
        _timelineFrame = new FrameView("Timeline") { X = 0, Y = Pos.Bottom(_commands), Width = Dim.Fill(), Height = 3 };
        _meterFrame = new FrameView("Audio Meter") { X = 0, Y = Pos.Bottom(_timelineFrame), Width = Dim.Fill(), Height = 6 };
        _timeline = NewLine(0);
        _meter = new Label("") { X = 0, Y = 0, Width = Dim.Fill(), Height = Dim.Fill(), ColorScheme = _accentScheme };
        _timelineFrame.Add(_timeline);
        _meterFrame.Add(_meter);
        _activity = NewLine(0);

        _dialogFrame = new FrameView("Dialog")
        {
            X = Pos.Center() - 34,
            Y = Pos.Center() - 7,
            Width = 68,
            Height = 14,
            Visible = false
        };
        _dialogText = new Label("") { X = 1, Y = 1, Width = Dim.Fill(2), Height = 4 };
        _dialogList = new ListView { X = 1, Y = 5, Width = Dim.Fill(2), Height = Dim.Fill(2), Visible = false, CanFocus = false };
        _dialogFrame.Add(_dialogText, _dialogList);
        _dialogList.MouseClick += e => HandleListMouse(e, () => _state?.MoveAddToPlaylistSelection(-1), () => _state?.MoveAddToPlaylistSelection(1));

        _window.Add(
            _workspaceFrame, _modesFrame, _searchFrame,
            _sessionFrame, _trackLensFrame, _soundLevelFrame,
            _libraryFrame, _playlistPanelFrame, _playlistTracksFrame, _browserFrame, _visualFrame,
            _playerFrame, _libraryStatsFrame,
            _commands, _timelineFrame, _meterFrame,
            _dialogFrame);

        Application.Top.Add(_window);
    }

    private static Label NewLine(int y) => new() { X = 0, Y = y, Width = Dim.Fill(), Height = 1, Text = "" };

    private static Label NewBlock(string title, Pos x, Pos y, int h, Dim w)
    {
        return new Label
        {
            X = x,
            Y = y,
            Width = w,
            Height = h,
            Text = title
        };
    }

    private static void HandleTopLevelKey(View.KeyEventEventArgs args)
    {
        if (_state is null) return;
        var key = args.KeyEvent.Key;

        if (IsCtrlChar(args, 'c')) { Application.RequestStop(); args.Handled = true; return; }

        if (HandleVisualizerInput(args)) return;
        if (HandleDialogInput(args)) return;
        if ((_state.View == AppView.Library || _state.View == AppView.Playlists) && HandleSearchInput(args)) return;
        if (HandleBrowserInput(args)) return;
        if (HandleGlobalViewShortcuts(args)) return;
        if (HandleGlobalPlaybackShortcuts(args)) return;
        if (HandleLibraryInput(args)) return;
        if (HandlePlaylistInput(args)) return;
    }

    private static bool HandleVisualizerInput(View.KeyEventEventArgs args)
    {
        var state = _state;
        if (state is null) return false;

        if (IsChar(args, 'v'))
        {
            if (state.View is AppView.NewPlaylist or AppView.AddToPlaylist or AppView.GoogleDriveImport or AppView.DeletePlaylistConfirm) return true;
            if (state.View == AppView.Visualizer)
            {
                SwitchToView(_viewBeforeVisualizer);
                state.NotifyStatus("cover visual mode off");
            }
            else
            {
                _viewBeforeVisualizer = state.View is AppView.Playlists ? AppView.Playlists : AppView.Library;
                state.SwitchView(AppView.Visualizer);
                state.NotifyStatus("cover visual mode on");
            }

            args.Handled = true;
            return true;
        }

        if (state.View == AppView.Visualizer && args.KeyEvent.Key == Key.Esc)
        {
            SwitchToView(_viewBeforeVisualizer);
            state.NotifyStatus("cover visual mode off");
            args.Handled = true;
            return true;
        }

        if (state.View == AppView.Visualizer && IsChar(args, 'i'))
        {
            _visualImageMode = !_visualImageMode;
            state.NotifyStatus(_visualImageMode ? "visual render: image" : "visual render: ascii");
            args.Handled = true;
            return true;
        }

        return false;
    }

    private static bool HandleGlobalViewShortcuts(View.KeyEventEventArgs args)
    {
        if (args.KeyEvent.Key == Key.F1 || IsChar(args, '1')) { SwitchToView(AppView.Library); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.F2 || IsChar(args, '2')) { SwitchToView(AppView.Playlists); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.F4 || IsChar(args, '4')) { _state!.OpenBrowser(); SwitchToView(AppView.Browser); args.Handled = true; return true; }
        return false;
    }

    private static bool HandleGlobalPlaybackShortcuts(View.KeyEventEventArgs args)
    {
        if (args.KeyEvent.Key == Key.Space) { _state!.TogglePlayPause(); args.Handled = true; return true; }
        if (IsChar(args, 'c')) { _paletteIndex = (_paletteIndex + 1) % Themes.Length; ApplyThemeVariant(); _state!.NotifyStatus($"theme palette  {Themes[_paletteIndex].Name.ToLowerInvariant()}"); args.Handled = true; return true; }
        if (IsChar(args, '?')) { _showCommands = !_showCommands; _state!.NotifyStatus(_showCommands ? "controls panel shown" : "controls panel hidden"); args.Handled = true; return true; }
        if (IsChar(args, '`')) { _showActivity = !_showActivity; _state!.NotifyStatus(_showActivity ? "execution lane shown" : "execution lane hidden"); args.Handled = true; return true; }
        if (IsChar(args, '+') || IsChar(args, '=')) { _state!.AdjustVolume(+5); args.Handled = true; return true; }
        if (IsChar(args, '-') || IsChar(args, '_')) { _state!.AdjustVolume(-5); args.Handled = true; return true; }
        if (IsChar(args, 'n') && _state!.View != AppView.Playlists) { _state.PlayNext(); args.Handled = true; return true; }
        if (IsChar(args, 'p')) { _state!.PlayPrevious(); args.Handled = true; return true; }
        if (IsChar(args, 's')) { _state!.ToggleShuffle(); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.CursorLeft) { _state!.SeekBy(-5); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.CursorRight) { _state!.SeekBy(5); args.Handled = true; return true; }
        return false;
    }

    private static bool HandleDialogInput(View.KeyEventEventArgs args)
    {
        if (_state is null) return false;

        switch (_state.View)
        {
            case AppView.NewPlaylist:
                if (args.KeyEvent.Key == Key.Enter) { _state.ConfirmNewPlaylist(); args.Handled = true; return true; }
                if (args.KeyEvent.Key == Key.Esc) { _state.SwitchView(AppView.Playlists); args.Handled = true; return true; }
                if (args.KeyEvent.Key == Key.Backspace) { _state.NewPlaylistBackspace(); args.Handled = true; return true; }
                if (TryTypedChar(args, out var ch1)) { _state.NewPlaylistAppendChar(ch1); args.Handled = true; return true; }
                return true;

            case AppView.GoogleDriveImport:
                if (args.KeyEvent.Key == Key.Enter) { _state.ConfirmGoogleDriveImport(); args.Handled = true; return true; }
                if (args.KeyEvent.Key == Key.Esc) { _state.CancelGoogleDriveImportDialog(); args.Handled = true; return true; }
                if (args.KeyEvent.Key == Key.Backspace) { _state.GoogleDriveLinkBackspace(); args.Handled = true; return true; }
                if (TryTypedChar(args, out var ch2)) { _state.GoogleDriveLinkAppendChar(ch2); args.Handled = true; return true; }
                return true;

            case AppView.AddToPlaylist:
                if (args.KeyEvent.Key == Key.Esc) { _state.CancelAddToPlaylistDialog(); args.Handled = true; return true; }
                if (args.KeyEvent.Key == Key.Enter) { _state.ConfirmAddToPlaylist(); args.Handled = true; return true; }
                if (args.KeyEvent.Key == Key.CursorUp || IsChar(args, 'k')) { _state.MoveAddToPlaylistSelection(-1); args.Handled = true; return true; }
                if (args.KeyEvent.Key == Key.CursorDown || IsChar(args, 'j')) { _state.MoveAddToPlaylistSelection(1); args.Handled = true; return true; }
                return true;

            case AppView.DeletePlaylistConfirm:
                if (args.KeyEvent.Key == Key.Esc) { _state.CancelDeletePlaylistDialog(); args.Handled = true; return true; }
                if (args.KeyEvent.Key == Key.Enter) { _state.ConfirmDeletePlaylist(); args.Handled = true; return true; }
                return true;

            default:
                return false;
        }
    }

    private static bool HandleBrowserInput(View.KeyEventEventArgs args)
    {
        if (_state?.View != AppView.Browser) return false;
        if (args.KeyEvent.Key == Key.Esc) { SwitchToView(AppView.Library); args.Handled = true; return true; }
        if (IsChar(args, 'g')) { _state.StartGoogleDriveImportDialog(); args.Handled = true; return true; }
        if (args.KeyEvent.Key == (Key.CtrlMask | Key.Space)) { _state.ToggleBrowserSelectionAtCursor(true); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.Space) { _state.ToggleBrowserSelectionAtCursor(false); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.CursorUp || IsChar(args, 'k')) { _state.MoveBrowserSelection(-1); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.CursorDown || IsChar(args, 'j')) { _state.MoveBrowserSelection(1); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.PageUp) { _state.MoveBrowserSelection(-10); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.PageDown) { _state.MoveBrowserSelection(10); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.Enter) { _state.BrowserActivate(); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.Backspace) { _state.NavigateUp(); args.Handled = true; return true; }
        return true;
    }

    private static bool HandleLibraryInput(View.KeyEventEventArgs args)
    {
        if (_state?.View != AppView.Library) return false;
        if (args.KeyEvent.Key == (Key.CtrlMask | Key.Space)) { _state.ToggleLibrarySelectionAtCursor(true); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.CursorUp || IsChar(args, 'k')) { _state.MoveLibrarySelection(-1); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.CursorDown || IsChar(args, 'j')) { _state.MoveLibrarySelection(1); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.PageUp) { _state.MoveLibrarySelection(-10); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.PageDown) { _state.MoveLibrarySelection(10); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.Home) { _state.MoveLibrarySelection(-10_000); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.End) { _state.MoveLibrarySelection(10_000); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.Enter) { _state.CueLibrarySelected(); args.Handled = true; return true; }
        if (IsChar(args, 'q') || IsChar(args, 'Q')) { _state.EnqueueLibrarySelection(); args.Handled = true; return true; }
        if (IsChar(args, 'a') || IsChar(args, 'A')) { _state.StartAddToPlaylistDialog(); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.DeleteChar || IsChar(args, 'd')) { _state.DeleteLibrarySelected(); args.Handled = true; return true; }
        return false;
    }

    private static bool HandlePlaylistInput(View.KeyEventEventArgs args)
    {
        if (_state?.View != AppView.Playlists) return false;

        if (args.KeyEvent.Key == Key.Tab || IsChar(args, 'l') || IsChar(args, 'h'))
        {
            _playlistTracksPaneActive = !_playlistTracksPaneActive;
            args.Handled = true;
            return true;
        }

        if (_playlistTracksPaneActive)
        {
            if (args.KeyEvent.Key == Key.CursorUp || IsChar(args, 'k')) { _state.MovePlaylistTrackSelection(-1); args.Handled = true; return true; }
            if (args.KeyEvent.Key == Key.CursorDown || IsChar(args, 'j')) { _state.MovePlaylistTrackSelection(1); args.Handled = true; return true; }
            if (args.KeyEvent.Key == Key.PageUp) { _state.MovePlaylistTrackSelection(-10); args.Handled = true; return true; }
            if (args.KeyEvent.Key == Key.PageDown) { _state.MovePlaylistTrackSelection(10); args.Handled = true; return true; }
            if (args.KeyEvent.Key == Key.Home) { _state.MovePlaylistTrackSelection(-10_000); args.Handled = true; return true; }
            if (args.KeyEvent.Key == Key.End) { _state.MovePlaylistTrackSelection(10_000); args.Handled = true; return true; }
            if (args.KeyEvent.Key == Key.Enter) { _state.CuePlaylistTrack(); args.Handled = true; return true; }
            if (IsChar(args, 'q') || IsChar(args, 'Q')) { _state.EnqueuePlaylistTrackSelected(); args.Handled = true; return true; }
            if (IsChar(args, 'a') || IsChar(args, 'A')) { _state.StartAddToPlaylistDialog(); args.Handled = true; return true; }
            if (IsChar(args, 'r')) { _state.RemovePlaylistTrackSelected(); args.Handled = true; return true; }
            if (IsChar(args, 'N')) { _state.PlayNext(); args.Handled = true; return true; }
            return true;
        }

        if (IsChar(args, 'q') || IsChar(args, 'Q')) { _state.EnqueuePlaylistTrackSelected(); args.Handled = true; return true; }
        if (IsChar(args, 'a') || IsChar(args, 'A')) { _state.StartAddToPlaylistDialog(); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.CursorUp || IsChar(args, 'k')) { _state.MovePlaylistPanel(-1); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.CursorDown || IsChar(args, 'j')) { _state.MovePlaylistPanel(1); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.PageUp) { _state.MovePlaylistPanel(-10); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.PageDown) { _state.MovePlaylistPanel(10); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.Enter)
        {
            _state.SelectPlaylist();
            _playlistTracksPaneActive = _state.PlaylistTracks.Count > 0;
            args.Handled = true;
            return true;
        }
        if (IsChar(args, 'n')) { _state.StartNewPlaylist(); args.Handled = true; return true; }
        if (IsChar(args, 'r')) { _state.RemovePlaylistTrackSelected(); args.Handled = true; return true; }
        if (IsChar(args, 'D')) { _state.StartDeletePlaylistDialog(); args.Handled = true; return true; }
        return false;
    }

    private static bool HandleSearchInput(View.KeyEventEventArgs args)
    {
        if (_state is null) return false;

        if (IsCtrlChar(args, 'f')) { _state.ActivateSearch(); args.Handled = true; return true; }

        if (args.KeyEvent.Key == Key.Esc)
        {
            if (_state.IsSearchActive || !string.IsNullOrWhiteSpace(_state.SearchQuery))
            {
                _state.ClearSearch();
                args.Handled = true;
                return true;
            }

            return false;
        }

        if (!_state.IsSearchActive) return false;

        if (args.KeyEvent.Key == Key.Tab || IsChar(args, 'c')) { _state.DeactivateSearch(); return false; }
        if (args.KeyEvent.Key == Key.Enter) { _state.DeactivateSearch(); args.Handled = true; return true; }
        if (args.KeyEvent.Key == Key.Backspace) { _state.BackspaceSearch(); args.Handled = true; return true; }

        if (args.KeyEvent.Key is Key.CursorUp or Key.CursorDown or Key.CursorLeft or Key.CursorRight)
        {
            if (args.KeyEvent.Key == Key.CursorUp)
            {
                if (_state.View == AppView.Library) _state.MoveLibrarySelection(-1);
                else if (_state.View == AppView.Playlists)
                {
                    if (_playlistTracksPaneActive) _state.MovePlaylistTrackSelection(-1);
                    else _state.MovePlaylistPanel(-1);
                }
            }
            else if (args.KeyEvent.Key == Key.CursorDown)
            {
                if (_state.View == AppView.Library) _state.MoveLibrarySelection(1);
                else if (_state.View == AppView.Playlists)
                {
                    if (_playlistTracksPaneActive) _state.MovePlaylistTrackSelection(1);
                    else _state.MovePlaylistPanel(1);
                }
            }

            args.Handled = true;
            return true;
        }

        if (IsChar(args, 'k') || IsChar(args, 'j'))
        {
            var step = IsChar(args, 'k') ? -1 : 1;
            if (_state.View == AppView.Library) _state.MoveLibrarySelection(step);
            else if (_state.View == AppView.Playlists)
            {
                if (_playlistTracksPaneActive) _state.MovePlaylistTrackSelection(step);
                else _state.MovePlaylistPanel(step);
            }

            args.Handled = true;
            return true;
        }

        if (TryTypedChar(args, out var ch))
        {
            _state.AppendSearchChar(ch);
            args.Handled = true;
            return true;
        }

        return false;
    }

    private static void SwitchToView(AppView view)
    {
        _state!.SwitchView(view);
        if (view == AppView.Playlists) _playlistTracksPaneActive = false;
    }

    private static bool TryTypedChar(View.KeyEventEventArgs args, out char ch)
    {
        var key = args.KeyEvent.Key;
        if ((key & Key.SpecialMask) != 0)
        {
            ch = default;
            return false;
        }

        var raw = (int)(key & Key.CharMask);
        if (raw is >= 32 and <= 126)
        {
            ch = (char)raw;
            return true;
        }

        ch = default;
        return false;
    }

    private static bool IsChar(View.KeyEventEventArgs args, char expected) => TryTypedChar(args, out var c) && c == expected;

    private static bool IsCtrlChar(View.KeyEventEventArgs args, char expected)
    {
        var key = args.KeyEvent.Key;
        var lower = char.ToLowerInvariant(expected);
        var upper = char.ToUpperInvariant(expected);
        return key == (Key.CtrlMask | (Key)lower) ||
               key == (Key.CtrlMask | (Key)upper);
    }

    private static void HandleListMouse(object e, Action scrollUp, Action scrollDown)
    {
        var eventObj = ReadProperty(e, "MouseEvent") ?? e;
        var flagsObj = ReadProperty(eventObj, "Flags");
        var flagsText = flagsObj?.ToString() ?? string.Empty;

        if (flagsText.Contains("WheeledUp", StringComparison.Ordinal))
        {
            scrollUp();
            WriteHandled(e, true);
            return;
        }

        if (flagsText.Contains("WheeledDown", StringComparison.Ordinal))
        {
            scrollDown();
            WriteHandled(e, true);
            return;
        }

        if (flagsText.Contains("Button1Clicked", StringComparison.Ordinal))
            WriteHandled(e, true);
    }

    private static object? ReadProperty(object source, string name)
    {
        var prop = source.GetType().GetProperty(name);
        return prop?.GetValue(source);
    }

    private static void WriteHandled(object source, bool handled)
    {
        var prop = source.GetType().GetProperty("Handled");
        if (prop is null || !prop.CanWrite) return;
        if (prop.PropertyType != typeof(bool)) return;
        prop.SetValue(source, handled);
    }

    private static void RefreshUi()
    {
        if (_state is null) return;

        var visibleLibraryIndices = _state.BuildVisibleLibrarySourceIndices();
        _libraryList!.SetSource(visibleLibraryIndices.Select(index =>
        {
            var track = _state.Library[index];
            var mark = _state.IsLibraryTrackMarked(index) ? "[x]" : "[ ]";
            return $"{mark} {track.Artist}  –  {track.Title}    {track.DisplayDuration}";
        }).ToList());
        _libraryList.SelectedItem = Math.Max(0, _state.BuildVisibleLibrarySelectedIndex());

        _playlistPanelList!.SetSource(_state.Playlists.Select(p => p.Name).ToList());
        _playlistPanelList.SelectedItem = Math.Max(0, _state.PlaylistPanelSelectedIndex);

        _playlistTracksList!.SetSource(_state.BuildVisiblePlaylistTracks().Select(t => $"{t.Artist}  –  {t.Title}    {t.DisplayDuration}").ToList());
        _playlistTracksList.SelectedItem = Math.Max(0, _state.BuildVisiblePlaylistSelectedIndex());

        _browserList!.SetSource(_state.BrowserEntries.Select((entry, index) =>
        {
            var selectable = _state.IsBrowserEntrySelectableForImport(index);
            var mark = selectable ? (_state.IsBrowserEntryMarked(index) ? "[x]" : "[ ]") : "   ";
            return $"{mark} {FormatBrowserEntry(entry)}";
        }).ToList());
        _browserList.SelectedItem = Math.Max(0, _state.BrowserSelectedIndex);

        var track = _state.NowPlaying;
        _workspace!.Text = $"Workspace  [1 Library]  [2 Playlists]  [4 Import]  [v Visual]";
        _modes!.Text = $"{(_state.IsPlaying ? "[Playback Live]" : "[Playback Idle]")}  {(_state.ShuffleOn ? "[Queue Shuffle]" : "[Queue Linear]")}  [Theme {Themes[_paletteIndex].Name} · c]  [{(_state.View == AppView.Visualizer ? "Visual Mode On" : "Visual Mode Off")} · v]  [{(_visualImageMode ? "Render IMAGE" : "Render ASCII")} · i]  [{(_showCommands ? "? Hide Controls" : "? Show Controls")}]  [{(_showActivity ? "` Hide Lane" : "` Show Lane")}]  [Scope {(_state.ActivePlaylist?.Name ?? "Library")}]  [Backend {_state.AudioBackend}]";

        var scope = _state.View == AppView.Playlists ? "playlist tracks" : "library tracks";
        var matches = _state.View == AppView.Playlists ? _state.VisiblePlaylistTrackCount : _state.VisibleLibraryCount;
        var stext = string.IsNullOrWhiteSpace(_state.SearchQuery) ? "type track / artist / album" : _state.SearchQuery;
        _searchFrame!.Title = _state.IsSearchActive
            ? "Search · Ctrl+F editing · Enter done · Esc clear"
            : "Search · Ctrl+F edit · Esc clear";
        _search!.Text = _state.IsSearchActive
            ? $"Search · Ctrl+F editing · Enter done · Esc clear   / {stext}_   [{matches} matches in {scope}]"
            : $"Search · Ctrl+F edit · Esc clear   / {stext}   [{matches} matches in {scope}]";

        _session!.Text = $"Pulse   {_state.PulseGlyph}\nBackend {_state.AudioBackend}\nUptime  {(int)_state.Uptime.TotalHours:00}:{_state.Uptime.Minutes:00}:{_state.Uptime.Seconds:00}\nView    {_state.View}";
        _trackLens!.Text = $"Track    {track?.Title ?? "—"}\nArtist   {track?.Artist ?? "—"}\nAlbum    {track?.Album ?? "—"}\nState    {(_state.IsPlaying ? "▶ playing" : "▌▌paused")}  {(_state.ShuffleOn ? "⇌ shuffle" : "→ linear")}";
        _playerStats!.Text = string.Join('\n', _state.BuildNowPlayingStats().Select(s => s.ToString()));
        _libraryStats!.Text = string.Join('\n', _state.BuildLibraryStats().Select(s => s.ToString()));

        _commands!.Text = _showCommands
            ? BuildCommandsText()
            : "Controls hidden (press ?)";

        _timeline!.Text = $"[▶ LIVE]  {BuildProgressBar(_state.Progress, 120)}  {_state.ProgressText}  VOL [{new string('|', Math.Clamp(_state.VolumePercent / 10, 0, 10)).PadRight(10, '·')}]";
        var meterHeight = Math.Max(3, _meter.Bounds.Height);
        var meterWidth = Math.Max(8, _meter.Bounds.Width);
        _meter!.Text = BuildMeterBars(_state.VisualizerLine, _state.CurrentLoudnessLevel, meterWidth, meterHeight);
        _soundLevelText!.Text = _state.VisualizerLine;

        _libraryFrame!.Visible = _state.View is AppView.Library or AppView.Playlists or AppView.Browser;
        _playlistPanelFrame!.Visible = _state.View is AppView.Library or AppView.Playlists;
        _playlistTracksFrame!.Visible = _state.View is AppView.Library or AppView.Playlists;
        _browserFrame!.Visible = _state.View == AppView.Browser;
        _visualFrame!.Visible = _state.View == AppView.Visualizer;

        _libraryFrame.Title = _state.LibraryMarkedCount > 0 ? $"Library · F1 · selected {_state.LibraryMarkedCount}" : "Library · F1";
        _playlistPanelFrame.Title = _playlistTracksPaneActive ? "Playlists · inactive" : "Playlists · active";
        _playlistTracksFrame.Title = _playlistTracksPaneActive ? "Playlist Tracks · active" : "Playlist Tracks · inactive";
        _browserFrame.Title = $"Import · {_state.BrowserPath} · selected {_state.BrowserMarkedCount}";

        _visualText!.Text = _state.View == AppView.Visualizer
            ? $"{_state.VisualizerLine}\n\n{track?.Artist ?? "-"} - {track?.Title ?? "-"}\n{_state.ProgressText}  {_state.RemainingText}\n{(_visualImageMode ? "IMAGE" : "ASCII")}" : "";

        RenderDialog();
    }

    private static void RenderDialog()
    {
        _dialogFrame!.Visible = false;
        _dialogList!.Visible = false;

        switch (_state!.View)
        {
            case AppView.NewPlaylist:
                _dialogFrame.Visible = true;
                _dialogFrame.Title = "New Playlist";
                _dialogText!.Text = $"> {_state.NewPlaylistName}_\n\nEnter create · Esc cancel";
                break;
            case AppView.GoogleDriveImport:
                _dialogFrame.Visible = true;
                _dialogFrame.Title = "Google Drive Import";
                _dialogText!.Text = $"Paste shared folder link and press Enter\n\n> {_state.GoogleDriveFolderLink}_\n\nEnter import · Esc cancel";
                break;
            case AppView.AddToPlaylist:
                _dialogFrame.Visible = true;
                _dialogFrame.Title = "Add To Playlist";
                _dialogText!.Text = _state.AddToPlaylistPrompt;
                _dialogList.Visible = true;
                _dialogList.SetSource(_state.Playlists.Select(p => p.Name).ToList());
                _dialogList.SelectedItem = Math.Max(0, _state.AddToPlaylistSelectedIndex);
                break;
            case AppView.DeletePlaylistConfirm:
                _dialogFrame.Visible = true;
                _dialogFrame.Title = "Delete Playlist";
                _dialogText!.Text = _state.DeletePlaylistPrompt + "\n\nEnter confirm · Esc cancel";
                break;
        }
    }

    private static string BuildCommandsText()
    {
        var rows = new List<string>
        {
            "Global: Space play/pause · n/p next-prev · s shuffle · q queue add",
            "Views: F1/F2/F4 switch · 1/2/4 quick switch · v cover visual"
        };

        switch (_state!.View)
        {
            case AppView.Library:
                rows.Add("Library: j/k move · Ctrl+Space toggle · q queue add · Enter play · d delete · a add to playlist");
                break;
            case AppView.Playlists:
                rows.Add("Lists: j/k playlists · Enter open · n new · D delete");
                rows.Add("Tracks: Tab/l toggle pane · j/k move · Enter play · q queue · a add · r remove · N next");
                break;
            case AppView.Browser:
                rows.Add("Import: j/k move · Enter open/import · Backspace up-dir · g google-drive · Esc cancel");
                rows.Add("Select: Space single-select · Ctrl+Space toggle");
                break;
            case AppView.Visualizer:
                rows.Add("Visual: i toggle render ascii/image · v or Esc exit");
                break;
        }

        return string.Join("\n", rows);
    }

    private static string FormatBrowserEntry(string entry)
    {
        if (entry == "../") return "^ ../";
        if (entry.EndsWith("/", StringComparison.Ordinal)) return $"[DIR] {entry}";
        if (entry.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) return $"[MP3] {entry}";
        return $"[FILE] {entry}";
    }

    private static string BuildProgressBar(double progress, int width)
    {
        var clamped = Math.Clamp(progress, 0, 1);
        var filled = (int)Math.Round(clamped * width);
        if (filled > width) filled = width;
        if (filled < 0) filled = 0;
        return "[" + new string('=', filled) + new string('.', width - filled) + "]";
    }

    private static string BuildMeterBars(string source, double overallLevel, int width, int height)
    {
        const string levels = "▁▂▃▄▅▆▇█";
        if (height < 3 || width < 3) return string.Empty;

        var innerHeight = Math.Max(2, height - 1);
        var baselineY = innerHeight - 1;
        var barCount = Math.Max(1, Math.Min((width + 1) / 2, 96));
        var usedWidth = (barCount * 2) - 1;
        var startX = Math.Max(0, (width - usedWidth) / 2);

        var parsed = new List<int>(source.Length);
        foreach (var ch in source)
        {
            var idx = levels.IndexOf(ch);
            if (idx >= 0) parsed.Add(idx);
        }
        if (parsed.Count == 0) parsed.Add(0);

        var grid = new char[innerHeight, width];
        for (var y = 0; y < innerHeight; y++)
        {
            for (var x = 0; x < width; x++)
                grid[y, x] = ' ';
        }

        for (var x = 0; x < width; x++)
            grid[baselineY, x] = '─';

        var loudnessBoost = 0.22 + (Math.Clamp(overallLevel, 0.0, 1.0) * 0.95);
        for (var i = 0; i < barCount; i++)
        {
            var x = startX + (i * 2);
            if (x < 0 || x >= width) continue;

            var sampleIndex = (int)Math.Round((i / (double)Math.Max(1, barCount - 1)) * Math.Max(0, parsed.Count - 1));
            var raw = parsed[Math.Clamp(sampleIndex, 0, parsed.Count - 1)];
            var normalized = raw / 7.0;
            var scaled = Math.Clamp((normalized * loudnessBoost) * innerHeight, 0.0, innerHeight);
            var fullCells = Math.Clamp((int)Math.Floor(scaled), 0, innerHeight);

            for (var h = 0; h < fullCells; h++)
            {
                var y = baselineY - 1 - h;
                if (y < 0 || y >= innerHeight) break;
                grid[y, x] = '█';
            }
        }

        var lines = new string[innerHeight];
        for (var y = 0; y < innerHeight; y++)
        {
            var lineChars = new char[width];
            for (var x = 0; x < width; x++)
                lineChars[x] = grid[y, x];
            lines[y] = new string(lineChars);
        }

        return string.Join('\n', lines);
    }
}
