#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT_DIR="${ROOT_DIR}/dist"
VERSION_TAG="${1:-local-docker}"

if ! command -v docker >/dev/null 2>&1; then
  echo "docker is required"
  exit 1
fi

RIDS=(
  "linux-x64"
  "linux-arm64"
  "osx-x64"
  "osx-arm64"
  "win-x64"
  "win-arm64"
)

for RID in "${RIDS[@]}"; do
  TARGET_DIR="${OUT_DIR}/${VERSION_TAG}/${RID}"
  mkdir -p "${TARGET_DIR}"

  docker build \
    --file "${ROOT_DIR}/docker/Dockerfile.publish" \
    --build-arg "RID=${RID}" \
    --output "type=local,dest=${TARGET_DIR}" \
    "${ROOT_DIR}"
done

echo "Published artifacts via Docker:"
echo "${OUT_DIR}/${VERSION_TAG}"
