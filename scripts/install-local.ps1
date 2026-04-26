$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

$arch = if ([System.Environment]::Is64BitOperatingSystem) { "x64" } else { throw "Only 64-bit Windows is supported." }
$rid = "win-$arch"

$publishDir = Join-Path $RootDir "dist\local-install\$rid"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\VibeVault"
$targetExe = Join-Path $installDir "vibevault.exe"

dotnet publish (Join-Path $RootDir "VibeVault.csproj") `
  -c Release `
  -r $rid `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:PublishTrimmed=false `
  -o $publishDir

New-Item -ItemType Directory -Force -Path $installDir | Out-Null
Copy-Item (Join-Path $publishDir "VibeVault.exe") $targetExe -Force

Write-Host "Installed: $targetExe"
Write-Host "If needed, add this directory to PATH: $installDir"
