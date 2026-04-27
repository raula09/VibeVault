using Tessera.Layout;

namespace VibeVault;

internal sealed partial class VibeVaultApp
{
    private void BuildBody(ContentBuilder body)
    {
        switch (_state.View)
        {
            case AppView.Library:
                BuildLibraryView(body);
                break;
            case AppView.Playlists:
                BuildPlaylistView(body);
                break;
            case AppView.Browser:
                BuildBrowserView(body);
                break;
            case AppView.Visualizer:
                BuildVisualizerView(body);
                break;
            case AppView.NewPlaylist:
                BuildNewPlaylistDialog(body);
                break;
            case AppView.AddToPlaylist:
                BuildAddToPlaylistDialog(body);
                break;
            case AppView.GoogleDriveImport:
                BuildGoogleDriveImportDialog(body);
                break;
        }
    }


    private void BuildLibraryView(ContentBuilder body)
    {
        body.Column(col =>
        {
            col.Fixed(4, top => top.Row(row =>
            {
                row.Fill(_workspaceTabs);
                row.Fill(_modeChips);
            }));
            col.Fixed(3, _searchBar);
            col.Fixed(6, summary => summary.Row(row =>
            {
                row.Fixed(24, _sessionCard);
                row.Fill(_trackFactsCard);
                row.Fixed(40, _visualizerCard);
            }));
            col.Weighted(5, bottom => bottom.Row(row =>
            {
                row.Fill(_libraryList);
                row.Fixed(34, sidebar => sidebar.Column(sidebarCol =>
                {
                    sidebarCol.Weighted(1, _playerStats);
                    sidebarCol.Weighted(1, _libraryStats);
                }));
            }));
            col.Weighted(3, _activityFeed);
            if (_showCommandDeck)
                col.Fixed(7, _commandDeckCard);
            col.Fixed(4, _seekBar);
            col.Fixed(8, meter => meter.Center(center => center.Row(row => row.Fill(_audioMeter)), width: 140));
        });
    }

    private void BuildPlaylistView(ContentBuilder body)
    {
        body.Column(col =>
        {
            col.Fixed(4, top => top.Row(row =>
            {
                row.Fill(_workspaceTabs);
                row.Fill(_modeChips);
            }));
            col.Fixed(3, _searchBar);
            col.Fixed(5, summary => summary.Row(row =>
            {
                row.Fixed(24, _sessionCard);
                row.Fill(_trackFactsCard);
                row.Fixed(40, _visualizerCard);
            }));
            col.Weighted(5, main => main.Row(row =>
            {
                row.Fixed(34, _playlistPanel);
                row.Fill(_playlistTracks);
            }));
            col.Weighted(3, _activityFeed);
            if (_showCommandDeck)
                col.Fixed(7, _commandDeckCard);
            col.Fixed(4, _seekBar);
            col.Fixed(8, meter => meter.Center(center => center.Row(row => row.Fill(_audioMeter)), width: 200));
        });
    }


    private void BuildBrowserView(ContentBuilder body)
    {
        body.Row(row => row.Fill(_browserList));
    }

    private void BuildVisualizerView(ContentBuilder body)
    {
        body.Row(row => row.Fill(_albumArtVisualizer));
    }


    private void BuildNewPlaylistDialog(ContentBuilder body)
    {
        body.Center(center => center.Row(row => row.Fill(_dialogLabel)), width: 50, height: 5);
    }

    private void BuildAddToPlaylistDialog(ContentBuilder body)
    {
        _dialogLabel.Title = "Add To Playlist";
        _dialogLabel.Text = _state.AddToPlaylistPrompt;

        body.Center(center => center.Column(col =>
        {
            col.Fixed(5, dialog => dialog.Row(row => row.Fill(_dialogLabel)));
            col.Fixed(10, list => list.Row(row => row.Fill(_addToPlaylistList)));
        }), width: 56, height: 15);
    }

    private void BuildGoogleDriveImportDialog(ContentBuilder body)
    {
        _dialogLabel.Title = "Google Drive Import";
        _dialogLabel.Text = "Paste shared folder link and press Enter";

        body.Center(center => center.Column(col =>
        {
            col.Fixed(5, dialog => dialog.Row(row => row.Fill(_dialogLabel)));
            col.Fixed(3, search => search.Row(row => row.Fill(_searchBar)));
        }), width: 92, height: 8);
    }
}
