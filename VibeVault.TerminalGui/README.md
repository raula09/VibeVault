# VibeVault.TerminalGui

This is a Terminal.Gui rewrite of VibeVault that reuses the existing backend/state/data/audio layers and replaces the Tessera UI layer.

## Run

```bash
dotnet run --project VibeVault.TerminalGui/VibeVault.TerminalGui.csproj
```

## Controls

- `F1`: Library view
- `F2`: Playlists view
- `F4`: Browser/import view
- `Space`: Play/pause
- `F5` / `F6`: Previous/next track
- `Left` / `Right`: Seek -5s / +5s
- `Up` / `Down`: Move selection in active view
- `s`: Toggle shuffle
- `+` / `-`: Volume
- `n`: New playlist dialog
- `Ctrl+F`: Focus search
- `Esc`: Exit browser to library
- `Ctrl+Q`: Quit
