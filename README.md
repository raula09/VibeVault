# VibeVault

Terminal music library player written in C# using Tessera UI framework.

Listen to local MP3s in a fast keyboard-first TUI with playlists, search, timeline seeking, loudness meter, and album-art visual mode.

## Supported Format

VibeVault currently imports and indexes MP3 files only.

| Capability | MP3 |
|------------|-----|
| Import from browser | Yes |
| Playback | Yes |
| Metadata read (title/artist/album/year/bpm) | Yes |
| Embedded cover art visual mode | Yes (if cover exists) |

## Installation

### Requirements

| Dependency | Required | Purpose |
|------------|----------|---------|
| .NET SDK 10.0+ | Yes | Build and run app |
| Tessera | Yes | TUI framework |
| Microsoft.Data.Sqlite | Yes | Persistent library/playlists |
| TagLibSharp | Yes | MP3 metadata parsing |
| `ffplay` / `mpv` / `mpg123` / `vlc` | Yes | Audio backend (auto-detected) |
| `ffmpeg` or `avconv` | Optional | Loudness analysis and embedded cover extraction |

### Cross-Platform Targets

| OS | Architectures |
|----|---------------|
| Linux | `x64`, `arm64` |
| macOS | `x64`, `arm64` |
| Windows | `x64`, `arm64` |

Check out Tessera: https://georgetsouvaltzis.github.io/tessera/

### Install (Terminal, Recommended)

#### Linux / macOS

```bash
curl -fsSL https://raw.githubusercontent.com/raula09/VibeVault/main/install.sh | sh
```

#### Windows (PowerShell)

```powershell
iwr -useb https://raw.githubusercontent.com/raula09/VibeVault/main/install.ps1 | iex
```

### Build Release Artifacts (All Platforms)

```bash
chmod +x scripts/publish-all.sh
./scripts/publish-all.sh v1.0.0
```

Artifacts are written to `dist/<version>/<rid>/`.

### Run From Source

```bash
dotnet restore
dotnet run
```

### Download Link vs Terminal Install

For this project, terminal install is better for active development and quick testing.

- Use terminal install when iterating locally (`scripts/install-local.*`)
- Use release download links once you publish artifacts from `dist/` to your release page

## Keybindings

### Global

| Key | Action |
|-----|--------|
| `F1` / `1` | Library view |
| `F2` / `2` | Playlists view |
| `F4` / `4` | Import browser |
| `v` | Toggle cover visual mode |
| `i` | Toggle visual render (`ASCII` / `IMAGE`) in visual mode |
| `Space` | Play/Pause |
| `n` / `p` | Next/Previous track |
| `s` | Shuffle on/off |
| `+` / `-` | Volume up/down |
| `c` | Cycle UI palette |
| `?` | Show/hide controls panel |
| `Ctrl+C` | Quit |

### Library View

| Key | Action |
|-----|--------|
| `j` / `k` or `↓` / `↑` | Move selection |
| `Enter` | Play selected track |
| `a` | Add selected track to playlist |
| `d` / `Delete` | Remove selected track from library |
| `Ctrl+F` | Start search |
| `Esc` | Clear search |

### Playlists View

| Key | Action |
|-----|--------|
| `j` / `k` | Move playlist selection |
| `Tab` / `l` / `h` | Switch focus between playlists and tracks |
| `Enter` | Open playlist / play focused track |
| `n` | New playlist |
| `r` | Remove selected track from active playlist |
| `D` | Delete active playlist |
| `Ctrl+F` | Search playlist tracks |

### Import Browser

| Key | Action |
|-----|--------|
| `j` / `k` or `↓` / `↑` | Move cursor |
| `Enter` | Open folder or import file |
| `Backspace` | Go up directory |
| `Space` | Single-select file |
| `Ctrl+Space` | Toggle marked file |
| `Shift+Up/Down` | Range-select files |
| `Esc` | Exit import browser |

## Backends

Audio backend is auto-selected from available executables.

| Backend | Used For |
|---------|----------|
| `mpv` | Playback (preferred if present) |
| `ffplay` | Playback fallback |
| `mpg123` | Playback fallback |
| `cvlc` / `vlc` | Playback fallback |

## Files

Configuration and data are stored in the VibeVault app-data directory.

| System | Base Path |
|--------|-----------|
| Linux | `~/.config/VibeVault/` |
| macOS | `~/Library/Application Support/VibeVault/` |
| Windows | `%APPDATA%\\VibeVault\\` |

| File | Description |
|------|-------------|
| `library.db` | SQLite library + playlists |
| `ui-settings.json` | Theme index, controls-panel visibility, visual render mode |

## License

No license file is currently included in this repository.
