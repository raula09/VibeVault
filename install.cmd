@echo off
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -Command "iwr -useb https://raw.githubusercontent.com/raula09/VibeVault/main/install.ps1 | iex"
endlocal
