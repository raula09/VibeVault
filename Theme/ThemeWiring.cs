using Tessera;
using Tessera.Styles;

namespace VibeVault;

internal sealed partial class VibeVaultApp
{
    private void ApplyTheme()
    {
        var t = VibeVaultTheme.DefaultTheme;
        var p = CurrentUiPalette;
        var borderBase = VibeVaultTheme.Foreground(p.BorderColor);
        var borderCool = VibeVaultTheme.Foreground(MixColor(p.BorderColor, p.AccentSecondaryColor, 0.32));
        var borderWarm = VibeVaultTheme.Foreground(MixColor(p.BorderColor, p.AccentPrimaryColor, 0.32));
        var borderMuted = VibeVaultTheme.Foreground(MixColor(p.BorderColor, p.MutedTextColor, 0.42));

        var textPrimary = VibeVaultTheme.Foreground(p.PrimaryTextColor);
        var textSecondary = VibeVaultTheme.Foreground(p.SecondaryTextColor);
        var textMuted = VibeVaultTheme.Foreground(p.MutedTextColor);
        var accentPrimary = VibeVaultTheme.Foreground(p.AccentPrimaryColor);
        var accentSecondary = VibeVaultTheme.Foreground(p.AccentSecondaryColor);
        var titleCool = VibeVaultTheme.Foreground(MixColor(p.SecondaryTextColor, p.AccentSecondaryColor, 0.35));
        var titleWarm = VibeVaultTheme.Foreground(MixColor(p.SecondaryTextColor, p.AccentPrimaryColor, 0.35));
        var selection = VibeVaultTheme.Chip(p.SelectionForegroundColor, p.SelectionBackgroundColor);
        var focus = VibeVaultTheme.Foreground(p.SelectionBackgroundColor).WithBold();

        _nowPlaying.TitleStyle    = titleWarm.WithBold();
        _nowPlaying.TrackStyle    = textPrimary.WithBold();
        _nowPlaying.ArtistStyle   = accentPrimary.WithBold();
        _nowPlaying.AlbumStyle    = textSecondary;
        _nowPlaying.ChipStyle     = selection;
        _nowPlaying.ProgressStyle = accentSecondary.WithBold();
        _nowPlaying.MutedStyle    = textMuted;
        _nowPlaying.BorderStyleText = borderWarm;

        _seekBar.TitleStyle        = titleCool.WithBold();
        _seekBar.FocusedTitleStyle = focus;
        _seekBar.BorderStyleText   = borderCool;
        _seekBar.FocusedBorderStyle= focus;
        _seekBar.FillStyle         = VibeVaultTheme.Foreground(p.SeekFillColor).WithBold();
        _seekBar.TrackStyle        = VibeVaultTheme.Foreground(p.SeekTrackColor);
        _seekBar.LabelStyle        = VibeVaultTheme.Foreground(p.SeekLabelColor).WithBold();
        _seekBar.KnobStyle         = VibeVaultTheme.Foreground(p.SeekKnobColor).WithBold();

        _audioMeter.TitleStyle = titleWarm.WithBold();
        _audioMeter.FocusedTitleStyle = focus;
        _audioMeter.BorderStyleText = borderWarm;
        _audioMeter.FocusedBorderStyle = focus;
        _audioMeter.TopBarStyle = accentPrimary.WithBold();
        _audioMeter.BottomBarStyle = accentSecondary.WithBold();

        _albumArtVisualizer.TitleStyle = titleCool.WithBold();
        _albumArtVisualizer.FocusedTitleStyle = focus;
        _albumArtVisualizer.BorderStyleText = borderCool;
        _albumArtVisualizer.FocusedBorderStyle = focus;
        _albumArtVisualizer.InfoStyle = textPrimary.WithBold();
        _albumArtVisualizer.HintStyle = textMuted;

        StyleList(_libraryList, p,
            borderColor: p.BorderColor,
            titleColor: MixColor(p.SecondaryTextColor, p.AccentSecondaryColor, 0.20));

        StyleList(_playlistPanel, p,
            borderColor: MixColor(p.BorderColor, p.AccentSecondaryColor, 0.35),
            titleColor: MixColor(p.SecondaryTextColor, p.AccentSecondaryColor, 0.42),
            currentColor: MixColor(p.AccentSecondaryColor, p.AccentPrimaryColor, 0.35));
        _playlistPanel.SelectedItemStyle = selection;

        StyleList(_playlistTracks, p,
            borderColor: MixColor(p.BorderColor, p.AccentPrimaryColor, 0.35),
            titleColor: MixColor(p.SecondaryTextColor, p.AccentPrimaryColor, 0.42));

        StyleList(_browserList, p,
            borderColor: MixColor(p.BorderColor, p.MutedTextColor, 0.35),
            titleColor: MixColor(p.SecondaryTextColor, p.AccentSecondaryColor, 0.30));
        _browserList.SelectedItemStyle = selection;

        StyleList(_addToPlaylistList, p,
            borderColor: MixColor(p.BorderColor, p.AccentPrimaryColor, 0.30),
            titleColor: MixColor(p.SecondaryTextColor, p.AccentPrimaryColor, 0.35));
        _addToPlaylistList.SelectedItemStyle = selection;

        _workspaceTabs.TitleStyle = titleCool.WithBold();
        _workspaceTabs.FocusedTitleStyle = focus;
        _workspaceTabs.BorderStyleText = borderCool;
        _workspaceTabs.FocusedBorderStyle = focus;

        _modeChips.TitleStyle = titleWarm.WithBold();
        _modeChips.FocusedTitleStyle = focus;
        _modeChips.BorderStyleText = borderWarm;
        _modeChips.FocusedBorderStyle = focus;

        StyleCard(_playerStats, accentPrimary.WithBold(), borderWarm, titleWarm.WithBold());
        StyleCard(_libraryStats, accentSecondary.WithBold(), borderCool, titleCool.WithBold());
        StyleCard(_sessionCard, accentSecondary.WithBold(), borderMuted, titleCool.WithBold());
        StyleCard(_trackFactsCard, accentPrimary.WithBold(), borderWarm, titleWarm.WithBold());
        StyleCard(_visualizerCard, accentPrimary.WithBold(), borderCool, titleCool.WithBold());
        _commandDeckCard.TitleStyle = titleCool.WithBold();
        _commandDeckCard.FocusedTitleStyle = focus;
        _commandDeckCard.BorderStyleText = borderCool;
        _commandDeckCard.FocusedBorderStyle = focus;
        _commandDeckCard.GroupStyle = accentSecondary.WithBold();
        _commandDeckCard.CommandStyle = textPrimary.WithBold();
        StyleCard(_activityFeed, accentPrimary.WithBold(), borderMuted, titleWarm.WithBold());
        _activityFeed.TextStyle = VibeVaultTheme.Foreground(p.ActivityTextColor).WithBold();
        _visualizerCard.TextStyle = VibeVaultTheme.Foreground(p.VisualizerTextColor).WithBold();
        StyleCard(_searchBar, textPrimary, borderMuted, titleCool.WithBold());

        _dialogLabel.ApplyTheme(t);
        _dialogLabel.TitleStyle     = focus;
        _dialogLabel.BorderStyleText= borderWarm;
        _dialogLabel.TextStyle      = textPrimary.WithBold();

    }

    private void StyleList(
        ScrollListControl list,
        UiPalette palette,
        int? borderColor = null,
        int? titleColor = null,
        int? currentColor = null)
    {
        var border = VibeVaultTheme.Foreground(borderColor ?? palette.BorderColor);
        var focus = VibeVaultTheme.Foreground(palette.AccentSecondaryColor).WithBold();
        list.TitleStyle         = VibeVaultTheme.Foreground(titleColor ?? palette.SecondaryTextColor).WithBold();
        list.FocusedTitleStyle  = focus;
        list.ItemStyle          = VibeVaultTheme.Foreground(palette.PrimaryTextColor);
        list.CurrentItemStyle   = VibeVaultTheme.Foreground(currentColor ?? palette.AccentPrimaryColor).WithBold();
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

    private static int MixColor(int a, int b, double t)
    {
        t = Math.Clamp(t, 0.0, 1.0);
        var ar = (a >> 16) & 0xFF;
        var ag = (a >> 8) & 0xFF;
        var ab = a & 0xFF;
        var br = (b >> 16) & 0xFF;
        var bg = (b >> 8) & 0xFF;
        var bb = b & 0xFF;

        int rr = (int)Math.Round(ar + ((br - ar) * t));
        int rg = (int)Math.Round(ag + ((bg - ag) * t));
        int rb = (int)Math.Round(ab + ((bb - ab) * t));
        return (rr << 16) | (rg << 8) | rb;
    }

}
