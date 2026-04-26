using Microsoft.Data.Sqlite;

namespace VibeVault;
internal sealed class VibeVaultDb : IDisposable
{
    private readonly SqliteConnection _conn;


    public VibeVaultDb(string dbPath)
    {
        _conn = new SqliteConnection($"Data Source={dbPath}");
        _conn.Open();
        Migrate();
    }

    public void Dispose() => _conn.Dispose();


    private void Migrate()
    {
        Exec("""
            CREATE TABLE IF NOT EXISTS tracks (
                id          TEXT PRIMARY KEY,
                path        TEXT NOT NULL UNIQUE,
                title       TEXT NOT NULL,
                artist      TEXT NOT NULL,
                album       TEXT NOT NULL,
                duration_s  INTEGER NOT NULL,
                bpm         INTEGER NOT NULL,
                year        INTEGER NOT NULL,
                added_at    TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS playlists (
                id   TEXT PRIMARY KEY,
                name TEXT NOT NULL UNIQUE
            );

            CREATE TABLE IF NOT EXISTS playlist_tracks (
                playlist_id TEXT NOT NULL REFERENCES playlists(id) ON DELETE CASCADE,
                track_id    TEXT NOT NULL REFERENCES tracks(id)    ON DELETE CASCADE,
                position    INTEGER NOT NULL,
                PRIMARY KEY (playlist_id, track_id)
            );
        """);
    }


    public IReadOnlyList<LibraryTrack> LoadAllTracks()
    {
        var list = new List<LibraryTrack>();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id,path,title,artist,album,duration_s,bpm,year,added_at FROM tracks ORDER BY artist,album,title";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new LibraryTrack(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetString(4),
                reader.GetInt32(5),
                reader.GetInt32(6),
                reader.GetInt32(7),
                reader.GetString(8)));
        }
        return list;
    }

    public bool UpsertTrack(LibraryTrack track)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO tracks(id,path,title,artist,album,duration_s,bpm,year,added_at)
            VALUES($id,$path,$title,$artist,$album,$dur,$bpm,$year,$added)
            ON CONFLICT(path) DO UPDATE SET
                title=excluded.title,
                artist=excluded.artist,
                album=excluded.album,
                duration_s=excluded.duration_s,
                bpm=excluded.bpm,
                year=excluded.year
            RETURNING (changes() > 0) as changed;
        """;
        cmd.Parameters.AddWithValue("$id",    track.Id);
        cmd.Parameters.AddWithValue("$path",  track.FilePath);
        cmd.Parameters.AddWithValue("$title", track.Title);
        cmd.Parameters.AddWithValue("$artist",track.Artist);
        cmd.Parameters.AddWithValue("$album", track.Album);
        cmd.Parameters.AddWithValue("$dur",   track.DurationSeconds);
        cmd.Parameters.AddWithValue("$bpm",   track.Bpm);
        cmd.Parameters.AddWithValue("$year",  track.Year);
        cmd.Parameters.AddWithValue("$added", track.AddedAt);
        using var r = cmd.ExecuteReader();
        return r.Read() && r.GetInt32(0) > 0;
    }

    public void DeleteTrack(string id)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "DELETE FROM tracks WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }


    public IReadOnlyList<Playlist> LoadAllPlaylists()
    {
        var list = new List<Playlist>();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "SELECT id,name FROM playlists ORDER BY name";
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new Playlist(r.GetString(0), r.GetString(1)));
        return list;
    }

    public Playlist CreatePlaylist(string name)
    {
        var id = Guid.NewGuid().ToString("N");
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "INSERT INTO playlists(id,name) VALUES($id,$name)";
        cmd.Parameters.AddWithValue("$id",   id);
        cmd.Parameters.AddWithValue("$name", name);
        cmd.ExecuteNonQuery();
        return new Playlist(id, name);
    }

    public void RenamePlaylist(string id, string newName)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "UPDATE playlists SET name=$name WHERE id=$id";
        cmd.Parameters.AddWithValue("$id",   id);
        cmd.Parameters.AddWithValue("$name", newName);
        cmd.ExecuteNonQuery();
    }

    public void DeletePlaylist(string id)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "DELETE FROM playlists WHERE id=$id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }


    public IReadOnlyList<LibraryTrack> LoadPlaylistTracks(string playlistId)
    {
        var list = new List<LibraryTrack>();
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            SELECT t.id,t.path,t.title,t.artist,t.album,t.duration_s,t.bpm,t.year,t.added_at
            FROM tracks t
            JOIN playlist_tracks pt ON pt.track_id = t.id
            WHERE pt.playlist_id = $pid
            ORDER BY pt.position
        """;
        cmd.Parameters.AddWithValue("$pid", playlistId);
        using var r = cmd.ExecuteReader();
        while (r.Read())
            list.Add(new LibraryTrack(r.GetString(0),r.GetString(1),r.GetString(2),r.GetString(3),
                                      r.GetString(4),r.GetInt32(5),r.GetInt32(6),r.GetInt32(7),r.GetString(8)));
        return list;
    }

    public void AddTrackToPlaylist(string playlistId, string trackId)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = """
            INSERT OR IGNORE INTO playlist_tracks(playlist_id,track_id,position)
            VALUES($pid,$tid, COALESCE((SELECT MAX(position)+1 FROM playlist_tracks WHERE playlist_id=$pid),0))
        """;
        cmd.Parameters.AddWithValue("$pid", playlistId);
        cmd.Parameters.AddWithValue("$tid", trackId);
        cmd.ExecuteNonQuery();
    }

    public void RemoveTrackFromPlaylist(string playlistId, string trackId)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = "DELETE FROM playlist_tracks WHERE playlist_id=$pid AND track_id=$tid";
        cmd.Parameters.AddWithValue("$pid", playlistId);
        cmd.Parameters.AddWithValue("$tid", trackId);
        cmd.ExecuteNonQuery();
    }


    private void Exec(string sql)
    {
        using var cmd = _conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }
}
