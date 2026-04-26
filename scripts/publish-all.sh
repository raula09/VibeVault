#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT_DIR="${ROOT_DIR}/dist"
VERSION_TAG="${1:-local}"

RIDS=(
  "linux-x64"
  "linux-arm64"
  "osx-x64"
  "osx-arm64"
  "win-x64"
  "win-arm64"
)

mkdir -p "${OUT_DIR}"

for RID in "${RIDS[@]}"; do
  TARGET_DIR="${OUT_DIR}/${VERSION_TAG}/${RID}"
  dotnet publish "${ROOT_DIR}/VibeVault.csproj" \
    -c Release \
    -r "${RID}" \
    --self-contained true \
    -p:PublishSingleFile=true \
    -p:PublishTrimmed=false \
    -o "${TARGET_DIR}"
done

echo "Published artifacts:"
echo "${OUT_DIR}/${VERSION_TAG}"
