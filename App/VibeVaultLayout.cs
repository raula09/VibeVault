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
            col.Fixed(8, meter => meter.Row(row =>
            {
                row.Fill(new Tessera.Controls.Label { Border = Tessera.BorderStyle.None, Text = string.Empty });
                row.Fixed(140, _audioMeter);
                row.Fill(new Tessera.Controls.Label { Border = Tessera.BorderStyle.None, Text = string.Empty });
            }));
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
            col.Fixed(8, meter => meter.Row(row =>
            {
                row.Fill(new Tessera.Controls.Label { Border = Tessera.BorderStyle.None, Text = string.Empty });
                row.Fixed(200, _audioMeter);
                row.Fill(new Tessera.Controls.Label { Border = Tessera.BorderStyle.None, Text = string.Empty });
            }));
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
        body.Row(row =>
        {
            row.Fill(new Tessera.Controls.Label { Border = Tessera.BorderStyle.None, Text = string.Empty });
            row.Fixed(50, inner => inner.Column(col =>
            {
                col.Fill(new Tessera.Controls.Label { Border = Tessera.BorderStyle.None, Text = string.Empty });
                col.Fixed(5, _dialogLabel);
                col.Fill(new Tessera.Controls.Label { Border = Tessera.BorderStyle.None, Text = string.Empty });
            }));
            row.Fill(new Tessera.Controls.Label { Border = Tessera.BorderStyle.None, Text = string.Empty });
        });
    }

    private void BuildAddToPlaylistDialog(ContentBuilder body)
    {
        _dialogLabel.Title = "Add To Playlist";
        _dialogLabel.Text = _state.AddToPlaylistPrompt;

        body.Row(row =>
        {
            row.Fill(new Tessera.Controls.Label { Border = Tessera.BorderStyle.None, Text = string.Empty });
            row.Fixed(56, inner => inner.Column(col =>
            {
                col.Fill(new Tessera.Controls.Label { Border = Tessera.BorderStyle.None, Text = string.Empty });
                col.Fixed(5, _dialogLabel);
                col.Fixed(10, _addToPlaylistList);
                col.Fill(new Tessera.Controls.Label { Border = Tessera.BorderStyle.None, Text = string.Empty });
            }));
            row.Fill(new Tessera.Controls.Label { Border = Tessera.BorderStyle.None, Text = string.Empty });
        });
    }
}
