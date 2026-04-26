$ErrorActionPreference = "Stop"

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
  throw "git is required."
}

$tmpDir = Join-Path $env:TEMP ("VibeVault-install-" + [Guid]::NewGuid().ToString("N"))
git clone --depth 1 https://github.com/raula09/VibeVault.git $tmpDir | Out-Null

try {
  powershell -ExecutionPolicy Bypass -File (Join-Path $tmpDir "scripts\install-local.ps1")
}
finally {
  Remove-Item -Recurse -Force $tmpDir -ErrorAction SilentlyContinue
}
