namespace VibeVault;

internal sealed record LibraryTrack(
    string Id,
    string FilePath,
    string Title,
    string Artist,
    string Album,
    int    DurationSeconds,
    int    Bpm,
    int    Year,
    string AddedAt)
{
    public string DisplayDuration => FormatTime(DurationSeconds);

    public static string FormatTime(int totalSeconds)
    {
        var minutes = Math.Max(0, totalSeconds) / 60;
        var seconds = Math.Max(0, totalSeconds) % 60;
        return $"{minutes:00}:{seconds:00}";
    }
}

internal sealed record Playlist(string Id, string Name);
internal sealed record StatItem(string Key, string Value);


internal static class Mp3Scanner
{
    public static LibraryTrack? ScanFile(string path)
    {
        if (!File.Exists(path)) return null;
        if (!path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)) return null;

        try
        {
            using var file = TagLib.File.Create(path);

            var title  = Coalesce(file.Tag.Title,  Path.GetFileNameWithoutExtension(path));
            var artist = Coalesce(file.Tag.FirstPerformer, file.Tag.FirstAlbumArtist, "Unknown Artist");
            var album  = Coalesce(file.Tag.Album,  "Unknown Album");
            var dur    = (int)file.Properties.Duration.TotalSeconds;
            var bpm    = (int)(file.Tag.BeatsPerMinute);
            var year   = (int)file.Tag.Year;

            return new LibraryTrack(
                Id:              Guid.NewGuid().ToString("N"),
                FilePath:        path,
                Title:           title,
                Artist:          artist,
                Album:           album,
                DurationSeconds: dur,
                Bpm:             bpm,
                Year:            year,
                AddedAt:         DateTime.UtcNow.ToString("O"));
        }
        catch
        {
            return null;
        }
    }

    private static string Coalesce(params string?[] values)
    {
        foreach (var v in values)
            if (!string.IsNullOrWhiteSpace(v)) return v!.Trim();
        return "—";
    }
}
