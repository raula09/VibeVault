$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

function Test-DotnetSdk10Plus {
  if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    return $false
  }

  try {
    $sdkMajors = dotnet --list-sdks |
      ForEach-Object { ($_ -split "\.")[0] } |
      Where-Object { $_ -match '^\d+$' } |
      ForEach-Object { [int]$_ }
    if (-not $sdkMajors) {
      return $false
    }

    return (($sdkMajors | Measure-Object -Maximum).Maximum -ge 10)
  }
  catch {
    return $false
  }
}

function Ensure-DotnetSdk {
  if (Test-DotnetSdk10Plus) {
    return
  }

  Write-Host "Installing .NET SDK 10.0+ via winget..."
  if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    throw ".NET SDK 10.0+ is required. Install it manually and rerun install."
  }

  winget install --id Microsoft.DotNet.SDK.10 --source winget --accept-package-agreements --accept-source-agreements
  if (-not (Test-DotnetSdk10Plus)) {
    throw "Failed to install .NET SDK 10.0+."
  }
}

function Get-AudioBackendCommand {
  foreach ($cmd in @("ffplay", "mpv", "mpg123", "vlc")) {
    if (Get-Command $cmd -ErrorAction SilentlyContinue) {
      return $cmd
    }
  }
  return $null
}

function Ensure-RequiredAudioBackend {
  if (Get-AudioBackendCommand) {
    return
  }

  Write-Host "Installing required audio backend (ffplay/mpv/mpg123/vlc) via winget..."
  if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    throw "A supported audio backend is required. Install one of: ffplay, mpv, mpg123, vlc."
  }

  winget install --id Gyan.FFmpeg --source winget --accept-package-agreements --accept-source-agreements
  if (-not (Get-AudioBackendCommand)) {
    throw "No supported audio backend found. Install one of: ffplay, mpv, mpg123, vlc."
  }
}

function Ensure-OptionalFfmpeg {
  if ((Get-Command ffmpeg -ErrorAction SilentlyContinue) -or (Get-Command avconv -ErrorAction SilentlyContinue)) {
    return
  }

  if (-not (Get-Command winget -ErrorAction SilentlyContinue)) {
    Write-Host "Optional dependency not installed: ffmpeg/avconv"
    return
  }

  Write-Host "Installing optional ffmpeg via winget..."
  try {
    winget install --id Gyan.FFmpeg --source winget --accept-package-agreements --accept-source-agreements | Out-Null
  }
  catch {
    Write-Host "Optional dependency not installed: ffmpeg/avconv"
  }
}

Ensure-DotnetSdk
Ensure-RequiredAudioBackend
Ensure-OptionalFfmpeg

$osArch = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
$arch = switch ($osArch) {
  ([System.Runtime.InteropServices.Architecture]::Arm64) { "arm64" }
  ([System.Runtime.InteropServices.Architecture]::X64) { "x64" }
  default { throw "Unsupported Windows architecture: $osArch. Supported: x64, arm64." }
}
$rid = "win-$arch"

$publishDir = Join-Path $RootDir "dist\local-install\$rid"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\VibeVault"
$targetExe = Join-Path $installDir "VibeVault.exe"

dotnet restore (Join-Path $RootDir "VibeVault.csproj")
dotnet publish (Join-Path $RootDir "VibeVault.csproj") `
  -c Release `
  -r $rid `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=false `
  -o $publishDir

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item (Join-Path $publishDir "*") $installDir -Force

Write-Host "Installed: $targetExe"

$userPath = [Environment]::GetEnvironmentVariable("Path", "User")
if (($userPath -split ";") -contains $installDir) {
  Write-Host "Run with: vibevault"
  exit 0
}

$newUserPath = if ([string]::IsNullOrWhiteSpace($userPath)) { $installDir } else { "$userPath;$installDir" }
[Environment]::SetEnvironmentVariable("Path", $newUserPath, "User")

Write-Host "Added to user PATH: $installDir"
Write-Host "Open a new terminal, then run: vibevault"
