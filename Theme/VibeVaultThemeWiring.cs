using Tessera;
using Tessera.Styles;

namespace VibeVault;

internal sealed partial class VibeVaultApp
{
    private void ApplyTheme()
    {
        var t = VibeVaultTheme.DefaultTheme;
        var p = CurrentUiPalette;
        var border = VibeVaultTheme.Foreground(p.BorderColor);
        var textPrimary = VibeVaultTheme.Foreground(p.PrimaryTextColor);
        var textSecondary = VibeVaultTheme.Foreground(p.SecondaryTextColor);
        var textMuted = VibeVaultTheme.Foreground(p.MutedTextColor);
        var accentPrimary = VibeVaultTheme.Foreground(p.AccentPrimaryColor);
        var accentSecondary = VibeVaultTheme.Foreground(p.AccentSecondaryColor);
        var selection = VibeVaultTheme.Chip(p.SelectionForegroundColor, p.SelectionBackgroundColor);
        var focus = accentSecondary.WithBold();

        _nowPlaying.TitleStyle    = textSecondary.WithBold();
        _nowPlaying.TrackStyle    = textPrimary.WithBold();
        _nowPlaying.ArtistStyle   = accentPrimary.WithBold();
        _nowPlaying.AlbumStyle    = textSecondary;
        _nowPlaying.ChipStyle     = selection;
        _nowPlaying.ProgressStyle = accentSecondary.WithBold();
        _nowPlaying.MutedStyle    = textMuted;
        _nowPlaying.BorderStyleText = border;

        _seekBar.TitleStyle        = textSecondary.WithBold();
        _seekBar.FocusedTitleStyle = focus;
        _seekBar.BorderStyleText   = border;
        _seekBar.FocusedBorderStyle= focus;
        _seekBar.FillStyle         = VibeVaultTheme.Foreground(p.SeekFillColor).WithBold();
        _seekBar.TrackStyle        = VibeVaultTheme.Foreground(p.SeekTrackColor);
        _seekBar.LabelStyle        = VibeVaultTheme.Foreground(p.SeekLabelColor).WithBold();
        _seekBar.KnobStyle         = VibeVaultTheme.Foreground(p.SeekKnobColor).WithBold();

        _audioMeter.TitleStyle = textSecondary.WithBold();
        _audioMeter.FocusedTitleStyle = focus;
        _audioMeter.BorderStyleText = border;
        _audioMeter.FocusedBorderStyle = focus;
        _audioMeter.TopBarStyle = accentPrimary.WithBold();
        _audioMeter.BottomBarStyle = accentSecondary.WithBold();

        _albumArtVisualizer.TitleStyle = textSecondary.WithBold();
        _albumArtVisualizer.FocusedTitleStyle = focus;
        _albumArtVisualizer.BorderStyleText = border;
        _albumArtVisualizer.FocusedBorderStyle = focus;
        _albumArtVisualizer.InfoStyle = textPrimary.WithBold();
        _albumArtVisualizer.HintStyle = textMuted;

        StyleList(_libraryList, p);

        StyleList(_playlistPanel, p);
        _playlistPanel.SelectedItemStyle = selection;

        StyleList(_playlistTracks, p);

        StyleList(_browserList, p);
        _browserList.SelectedItemStyle = VibeVaultTheme.Foreground(p.AccentSecondaryColor).WithBold();
        StyleList(_addToPlaylistList, p);
        _addToPlaylistList.SelectedItemStyle = selection;

        _workspaceTabs.TitleStyle = textSecondary.WithBold();
        _workspaceTabs.FocusedTitleStyle = focus;
        _workspaceTabs.BorderStyleText = border;
        _workspaceTabs.FocusedBorderStyle = focus;

        _modeChips.TitleStyle = textSecondary.WithBold();
        _modeChips.FocusedTitleStyle = focus;
        _modeChips.BorderStyleText = border;
        _modeChips.FocusedBorderStyle = focus;

        StyleCard(_playerStats,  accentPrimary.WithBold(), border, textSecondary.WithBold());
        StyleCard(_libraryStats, accentSecondary.WithBold(), border, textSecondary.WithBold());
        StyleCard(_sessionCard, accentSecondary.WithBold(), border, textSecondary.WithBold());
        StyleCard(_trackFactsCard, accentPrimary.WithBold(), border, textSecondary.WithBold());
        StyleCard(_visualizerCard, accentPrimary.WithBold(), border, textSecondary.WithBold());
        _commandDeckCard.TitleStyle = textSecondary.WithBold();
        _commandDeckCard.FocusedTitleStyle = focus;
        _commandDeckCard.BorderStyleText = border;
        _commandDeckCard.FocusedBorderStyle = focus;
        _commandDeckCard.GroupStyle = accentSecondary.WithBold();
        _commandDeckCard.CommandStyle = textPrimary.WithBold();
        StyleCard(_activityFeed, accentPrimary.WithBold(), border, textSecondary.WithBold());
        _activityFeed.TextStyle = VibeVaultTheme.Foreground(p.ActivityTextColor).WithBold();
        _visualizerCard.TextStyle = VibeVaultTheme.Foreground(p.VisualizerTextColor).WithBold();
        StyleCard(_searchBar, textPrimary, border, textSecondary.WithBold());

        _dialogLabel.ApplyTheme(t);
        _dialogLabel.TitleStyle     = focus;
        _dialogLabel.BorderStyleText= border;
        _dialogLabel.TextStyle      = textPrimary.WithBold();

    }

    private void StyleList(ScrollListControl list, UiPalette palette)
    {
        var border = VibeVaultTheme.Foreground(palette.BorderColor);
        var focus = VibeVaultTheme.Foreground(palette.AccentSecondaryColor).WithBold();
        list.TitleStyle         = VibeVaultTheme.Foreground(palette.SecondaryTextColor).WithBold();
        list.FocusedTitleStyle  = focus;
        list.ItemStyle          = VibeVaultTheme.Foreground(palette.PrimaryTextColor);
        list.CurrentItemStyle   = VibeVaultTheme.Foreground(palette.AccentPrimaryColor).WithBold();
        list.SelectedItemStyle  = VibeVaultTheme.Chip(palette.SelectionForegroundColor, palette.SelectionBackgroundColor);
        list.MetaStyle          = VibeVaultTheme.Foreground(palette.MutedTextColor);
        list.MutedStyle         = VibeVaultTheme.Foreground(palette.MutedTextColor);
        list.BorderStyleText    = border;
        list.FocusedBorderStyle = focus;
    }

    private static void StyleCard(Tessera.Controls.Label card, TesseraStyle valueStyle, TesseraStyle borderStyle, TesseraStyle titleStyle)
    {
        card.TitleStyle      = titleStyle;
        card.BorderStyleText = borderStyle;
        card.TextStyle       = valueStyle;
    }

}
