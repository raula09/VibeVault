#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

OS="$(uname -s)"
ARCH="$(uname -m)"

case "${OS}" in
  Linux) OS_NAME="linux" ;;
  Darwin) OS_NAME="osx" ;;
  *)
    echo "Unsupported OS: ${OS}"
    exit 1
    ;;
esac

case "${ARCH}" in
  x86_64|amd64) ARCH_NAME="x64" ;;
  aarch64|arm64) ARCH_NAME="arm64" ;;
  *)
    echo "Unsupported architecture: ${ARCH}"
    exit 1
    ;;
esac

RID="${OS_NAME}-${ARCH_NAME}"
PUBLISH_DIR="${ROOT_DIR}/dist/local-install/${RID}"
INSTALL_BIN_DIR="${HOME}/.local/bin"
INSTALL_APP_DIR="${HOME}/.local/share/VibeVault/current"

dotnet publish "${ROOT_DIR}/VibeVault.csproj" \
  -c Release \
  -r "${RID}" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false \
  -o "${PUBLISH_DIR}"

mkdir -p "${INSTALL_BIN_DIR}" "${INSTALL_APP_DIR}"
cp -a "${PUBLISH_DIR}/." "${INSTALL_APP_DIR}/"
ln -sf "${INSTALL_APP_DIR}/VibeVault" "${INSTALL_BIN_DIR}/vibevault"
chmod +x "${INSTALL_APP_DIR}/VibeVault" "${INSTALL_BIN_DIR}/vibevault"

echo "Installed: ${INSTALL_BIN_DIR}/vibevault"
if command -v vibevault >/dev/null 2>&1; then
  echo "Run with: vibevault"
  exit 0
fi

echo "Command not currently on PATH in this shell."
echo "Run now with: ${INSTALL_BIN_DIR}/vibevault"

if [[ ":$PATH:" != *":${INSTALL_BIN_DIR}:"* ]]; then
  EXPORT_LINE='export PATH="$HOME/.local/bin:$PATH"'
  for RC_FILE in "${HOME}/.profile" "${HOME}/.bashrc" "${HOME}/.zprofile"; do
    if [[ -f "${RC_FILE}" ]] && ! grep -Fq "${EXPORT_LINE}" "${RC_FILE}"; then
      printf '\n%s\n' "${EXPORT_LINE}" >> "${RC_FILE}"
      echo "Updated PATH in ${RC_FILE}"
    fi
  done
fi

echo "Open a new terminal (or run: export PATH=\"\$HOME/.local/bin:\$PATH\")"
