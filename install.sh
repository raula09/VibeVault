#!/usr/bin/env bash
set -euo pipefail

REPO_URL="${VIBEVAULT_REPO_URL:-https://github.com/raula09/VibeVault.git}"
INSTALL_BASE_DIR="${VIBEVAULT_INSTALL_BASE_DIR:-$HOME/.local/share/VibeVault}"
REPO_DIR="${INSTALL_BASE_DIR}/repo"

command_exists() {
  command -v "$1" >/dev/null 2>&1
}

if ! command_exists git; then
  echo "git is required to install VibeVault."
  exit 1
fi

mkdir -p "${INSTALL_BASE_DIR}"

if [[ -d "${REPO_DIR}/.git" ]]; then
  git -C "${REPO_DIR}" fetch --depth=1 origin main
  git -C "${REPO_DIR}" checkout -f main
  git -C "${REPO_DIR}" reset --hard origin/main
else
  rm -rf "${REPO_DIR}"
  git clone --depth=1 "${REPO_URL}" "${REPO_DIR}"
fi

bash "${REPO_DIR}/scripts/install-local.sh"
