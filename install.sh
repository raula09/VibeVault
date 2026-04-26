#!/usr/bin/env bash
set -euo pipefail

if ! command -v git >/dev/null 2>&1; then
  echo "git is required"
  exit 1
fi

TMP_DIR="$(mktemp -d)"
cleanup() { rm -rf "${TMP_DIR}"; }
trap cleanup EXIT

git clone --depth 1 https://github.com/raula09/VibeVault.git "${TMP_DIR}/VibeVault"
bash "${TMP_DIR}/VibeVault/scripts/install-local.sh"
