# VibeVault 🎵

A terminal-based music library manager and player built on the **Tessera** TUI framework.

```
┌─────────────────────────────────────────────────────────────┐
│  VibeVault ✦ Now Playing                                    │
│  NIGHT WINDOW                                               │
│  Mina Vale  •  Velvet Proof  •  2024                        │
│  [▶ playing]  [→ linear]  [library]                         │
│  01:32 / 04:26   -02:54                                     │
├──────────────────────────┬──────────────────────────────────┤
│  Library · F1  ✦         │                                  │
│  · Mina Vale – Night … 04:26  │  Player   BPM  92          │
│  ◆ Mina Vale – Slow …   05:11  │           Year 2024       │
│  · Lune Harbor – Rose … 03:47  │  Library  Tracks 5        │
│  ● Vesper Choir – Cedar 04:41  │           Lists  2        │
└──────────────────────────┴──────────────────────────────────┘
  vibevault  playing Mina Vale – Night Window        F1 F2 F4 …
```

## Features
- **SQLite library** – your tracks persist across sessions
- **MP3-only import** – only `.mp3` files are shown when browsing
- **ID3 tag reading** – title, artist, album, BPM, year extracted automatically
- **Playlists** – create, rename, delete; add/remove tracks
- **Player** – play/pause, next, previous, shuffle
- **Purple + amber-gold** colour theme

## Prerequisites

| Requirement | Version |
|-------------|---------|
| .NET SDK    | 9.0 +   |
| Tessera NuGet | latest |
| TagLibSharp  | 2.3.*  |
| Microsoft.Data.Sqlite | 9.* |
| External player | `ffplay` (recommended) / `mpv` / `mpg123` / `vlc` |

```bash
dotnet restore
dotnet run
```

> **Note**: Tessera is not yet published on NuGet.org.  
> Reference your local build with a `<ProjectReference>` in the `.csproj` if needed.

## Keybindings

### Global
| Key | Action |
|-----|--------|
| `F1` | Library view |
| `F2` | Playlists view |
| `F4` | Open MP3 browser / import |
| `Space` | Play / Pause |
| `n` | Next track |
| `p` | Previous track |
| `s` | Toggle shuffle |
| `Ctrl+C` | Quit |

### Library view
| Key | Action |
|-----|--------|
| `j` / `↓` | Move selection down |
| `k` / `↑` | Move selection up |
| `Enter` | Play selected track |
| `a` | Add selected track to active playlist |
| `d` / `Del` | Remove track from library |

### Playlists view
| Key | Action |
|-----|--------|
| `j` / `↓` | Move playlist selection |
| `Enter` | Open playlist |
| `n` | New playlist |
| `r` | Remove highlighted track from playlist |
| `D` | Delete active playlist |
| `Tab` / `l` | Focus playlist tracks panel |

### Browser (import)
| Key | Action |
|-----|--------|
| `j` / `↓` | Move down |
| `k` / `↑` | Move up |
| `Enter` | Enter directory / import MP3 |
| `Backspace` | Go up one directory |
| `Esc` | Cancel |

## Colour palette

| Role | Hex | Usage |
|------|-----|-------|
| Primary | `#C47EFF` | Amethyst violet – selected items, chips |
| Secondary | `#F3BE5A` | Amber gold – focus ring, playlist highlights |
| Surface | `#100818` | Deep violet-black background |
| Text | `#EDE3F6` | Soft lavender-white foreground |
| Muted | `#7A5A99` | Dim violet – meta / durations |

## Database location

| OS | Path |
|----|------|
| Linux / macOS | `~/.config/VibeVault/library.db` (XDG via `ApplicationData`) |
| Windows | `%APPDATA%\VibeVault\library.db` |
