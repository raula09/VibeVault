$ErrorActionPreference = "Stop"

$RepoUrl = if ($env:VIBEVAULT_REPO_URL) { $env:VIBEVAULT_REPO_URL } else { "https://github.com/raula09/VibeVault.git" }
$InstallBaseDir = if ($env:VIBEVAULT_INSTALL_BASE_DIR) { $env:VIBEVAULT_INSTALL_BASE_DIR } else { Join-Path $env:LOCALAPPDATA "VibeVault" }
$RepoDir = Join-Path $InstallBaseDir "repo"

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
  throw "git is required to install VibeVault."
}

New-Item -ItemType Directory -Force -Path $InstallBaseDir | Out-Null

if (Test-Path (Join-Path $RepoDir ".git")) {
  git -C $RepoDir fetch --depth=1 origin main
  git -C $RepoDir checkout -f main
  git -C $RepoDir reset --hard origin/main
}
else {
  if (Test-Path $RepoDir) {
    Remove-Item -Recurse -Force $RepoDir
  }
  git clone --depth=1 $RepoUrl $RepoDir
}

& (Join-Path $RepoDir "scripts\install-local.ps1")
