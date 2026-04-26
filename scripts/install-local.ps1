$ErrorActionPreference = "Stop"

$RootDir = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

$arch = if ([System.Environment]::Is64BitOperatingSystem) { "x64" } else { throw "Only 64-bit Windows is supported." }
$rid = "win-$arch"

$publishDir = Join-Path $RootDir "dist\local-install\$rid"
$installDir = Join-Path $env:LOCALAPPDATA "Programs\VibeVault"
$targetExe = Join-Path $installDir "VibeVault.exe"

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
