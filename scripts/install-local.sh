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
INSTALL_DIR="${HOME}/.local/bin"

dotnet publish "${ROOT_DIR}/VibeVault.csproj" \
  -c Release \
  -r "${RID}" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=false \
  -o "${PUBLISH_DIR}"

mkdir -p "${INSTALL_DIR}"
cp "${PUBLISH_DIR}/VibeVault" "${INSTALL_DIR}/vibevault"
chmod +x "${INSTALL_DIR}/vibevault"

echo "Installed: ${INSTALL_DIR}/vibevault"
echo "Run with: vibevault"
