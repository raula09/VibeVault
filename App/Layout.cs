using Tessera;
using Tessera.Layout;

namespace VibeVault;

internal sealed partial class VibeVaultApp
{
    private readonly record struct LayoutMetrics(
        int SummaryHeight,
        int SessionCardWidth,
        int VisualizerCardWidth,
        int LibrarySidebarWidth,
        int PlaylistPanelWidth,
        int MeterHeight,
        int CommandDeckHeight,
        int ActivityWeight,
        int MeterCenterWidth);

    private void ConfigureBody(ContentBuilder body, ScreenContext context)
    {
        switch (_state.View)
        {
            case AppView.Library:
                ConfigureLibraryPage(body, context);
                break;
            case AppView.Playlists:
                ConfigurePlaylistsPage(body, context);
                break;
            case AppView.Browser:
                ConfigureBrowserPage(body);
                break;
            case AppView.Visualizer:
                ConfigureVisualizerPage(body);
                break;
            case AppView.NewPlaylist:
                ConfigureNewPlaylistDialog(body);
                break;
            case AppView.AddToPlaylist:
                ConfigureAddToPlaylistDialog(body);
                break;
            case AppView.GoogleDriveImport:
                ConfigureGoogleDriveImportDialog(body);
                break;
            case AppView.DeletePlaylistConfirm:
                ConfigureDeletePlaylistConfirmDialog(body);
                break;
        }
    }

    private void ConfigureLibraryPage(ContentBuilder body, ScreenContext context)
    {
        LayoutMetrics metrics = BuildLayoutMetrics(context, isPlaylistPage: false);
        ConfigurePrimaryShell(
            body,
            metrics,
            mainContent: main => main.Row(row =>
            {
                row.Fill(_libraryList);
                row.Fixed(metrics.LibrarySidebarWidth, sidebar => sidebar.Column(column =>
                {
                    column.Weighted(1, _playerStats);
                    column.Weighted(1, _libraryStats);
                }));
            }));
    }

    private void ConfigurePlaylistsPage(ContentBuilder body, ScreenContext context)
    {
        LayoutMetrics metrics = BuildLayoutMetrics(context, isPlaylistPage: true);
        ConfigurePrimaryShell(
            body,
            metrics,
            mainContent: main => main.Row(row =>
            {
                row.Fixed(metrics.PlaylistPanelWidth, _playlistPanel);
                row.Fill(_playlistTracks);
            }));
    }

    private void ConfigurePrimaryShell(
        ContentBuilder body,
        LayoutMetrics metrics,
        Action<ContentBuilder> mainContent)
    {
        body.Column(column =>
        {
            column.Fixed(4, top => top.Row(row =>
            {
                row.Fill(_workspaceTabs);
                row.Fill(_modeChips);
            }));
            column.Fixed(3, _searchBar);
            column.Fixed(metrics.SummaryHeight, summary => summary.Row(row =>
            {
                row.Fixed(metrics.SessionCardWidth, _sessionCard);
                row.Fill(_trackFactsCard);
                row.Fixed(metrics.VisualizerCardWidth, _visualizerCard);
            }));
            column.Weighted(5, mainContent);

            if (_showActivityFeed)
                column.Weighted(metrics.ActivityWeight, _activityFeed);

            if (_showCommandDeck)
                column.Fixed(metrics.CommandDeckHeight, _commandDeckCard);

            column.Fixed(4, _seekBar);
            column.Fixed(metrics.MeterHeight, meter =>
                meter.Center(center => center.Row(row => row.Fill(_audioMeter)), width: metrics.MeterCenterWidth));
        });
    }

    private LayoutMetrics BuildLayoutMetrics(ScreenContext context, bool isPlaylistPage)
    {
        int summaryHeight = context.Height < 34 ? 5 : 6;
        int sessionCardWidth = Math.Clamp(context.Width / 5, 20, 28);
        int visualizerCardWidth = Math.Clamp(context.Width / 3, 32, 46);
        int librarySidebarWidth = Math.Clamp(context.Width / 4, 30, 38);
        int playlistPanelWidth = Math.Clamp(context.Width / 3, 30, 38);
        int meterHeight = context.Height < 34 ? 6 : 8;
        int commandDeckHeight = context.Height < 34 ? 6 : 7;
        int activityWeight = context.Height < 34 ? 2 : 3;
        int meterCenterWidth = Math.Clamp(context.Width - 4, 90, 220);

        if (isPlaylistPage)
            summaryHeight = Math.Max(5, summaryHeight - 1);

        return new LayoutMetrics(
            summaryHeight,
            sessionCardWidth,
            visualizerCardWidth,
            librarySidebarWidth,
            playlistPanelWidth,
            meterHeight,
            commandDeckHeight,
            activityWeight,
            meterCenterWidth);
    }

    private void ConfigureBrowserPage(ContentBuilder body)
    {
        body.Row(row => row.Fill(_browserList));
    }

    private void ConfigureVisualizerPage(ContentBuilder body)
    {
        body.Row(row => row.Fill(_albumArtVisualizer));
    }

    private void ConfigureNewPlaylistDialog(ContentBuilder body)
    {
        body.Center(center => center.Row(row => row.Fill(_dialogLabel)), width: 50, height: 5);
    }

    private void ConfigureAddToPlaylistDialog(ContentBuilder body)
    {
        _dialogLabel.Title = "Add To Playlist";
        _dialogLabel.Text = _state.AddToPlaylistPrompt;

        body.Center(center => center.Column(column =>
        {
            column.Fixed(5, dialog => dialog.Row(row => row.Fill(_dialogLabel)));
            column.Fixed(10, list => list.Row(row => row.Fill(_addToPlaylistList)));
        }), width: 56, height: 15);
    }

    private void ConfigureGoogleDriveImportDialog(ContentBuilder body)
    {
        _dialogLabel.Title = "Google Drive Import";
        _dialogLabel.Text = "Paste shared folder link and press Enter";

        body.Center(center => center.Column(column =>
        {
            column.Fixed(5, dialog => dialog.Row(row => row.Fill(_dialogLabel)));
            column.Fixed(3, search => search.Row(row => row.Fill(_searchBar)));
        }), width: 92, height: 8);
    }

    private void ConfigureDeletePlaylistConfirmDialog(ContentBuilder body)
    {
        _dialogLabel.Title = "Delete Playlist";
        _dialogLabel.Text = $"{_state.DeletePlaylistPrompt}\nEnter confirm  Esc cancel";
        body.Center(center => center.Row(row => row.Fill(_dialogLabel)), width: 72, height: 6);
    }

}
