#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET_DIR="${HOME}/.dotnet"
DOTNET_TOOLS_DIR="${HOME}/.dotnet/tools"

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

export PATH="${DOTNET_DIR}:${DOTNET_TOOLS_DIR}:${PATH}"

command_exists() {
  command -v "$1" >/dev/null 2>&1
}

have_audio_backend() {
  command_exists ffplay || command_exists mpv || command_exists mpg123 || command_exists vlc
}

dotnet_sdk_10_plus() {
  if ! command_exists dotnet; then
    return 1
  fi

  local major
  major="$(dotnet --list-sdks 2>/dev/null | awk -F. 'NF > 0 { print $1 }' | sort -nr | head -n1)"
  [[ -n "${major}" ]] && [[ "${major}" -ge 10 ]]
}

run_as_root() {
  if [[ "$(id -u)" -eq 0 ]]; then
    "$@"
    return
  fi

  if command_exists sudo; then
    sudo "$@"
    return
  fi

  return 1
}

install_dotnet_sdk_if_needed() {
  if dotnet_sdk_10_plus; then
    return
  fi

  echo "Installing .NET SDK 10.0+ to ${DOTNET_DIR}..."
  if ! command_exists curl; then
    echo "curl is required to bootstrap .NET SDK."
    exit 1
  fi

  local install_script
  install_script="$(mktemp)"
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "${install_script}"
  bash "${install_script}" --channel 10.0 --install-dir "${DOTNET_DIR}"
  rm -f "${install_script}"

  export PATH="${DOTNET_DIR}:${DOTNET_TOOLS_DIR}:${PATH}"
  if ! dotnet_sdk_10_plus; then
    echo "Failed to install .NET SDK 10.0+."
    exit 1
  fi
}

install_with_manager() {
  local package="$1"
  if command_exists apt-get; then
    run_as_root apt-get update
    run_as_root apt-get install -y "${package}"
    return $?
  fi
  if command_exists dnf; then
    run_as_root dnf install -y "${package}"
    return $?
  fi
  if command_exists yum; then
    run_as_root yum install -y "${package}"
    return $?
  fi
  if command_exists pacman; then
    run_as_root pacman -Sy --noconfirm "${package}"
    return $?
  fi
  if command_exists zypper; then
    run_as_root zypper --non-interactive install "${package}"
    return $?
  fi
  if command_exists apk; then
    run_as_root apk add --no-cache "${package}"
    return $?
  fi
  if command_exists brew; then
    brew install "${package}"
    return $?
  fi
  return 1
}

ensure_required_audio_backend() {
  if have_audio_backend; then
    return
  fi

  echo "Installing required audio backend (ffplay/mpv/mpg123/vlc)..."
  local candidate
  local installed=0
  for candidate in mpg123 mpv vlc ffmpeg; do
    if install_with_manager "${candidate}"; then
      installed=1
      break
    fi
  done

  if [[ "${installed}" -ne 1 ]]; then
    echo "Could not auto-install a required audio backend."
    echo "Install one of: ffplay, mpv, mpg123, vlc"
    exit 1
  fi

  if ! have_audio_backend; then
    echo "No supported audio backend found after installation."
    echo "Install one of: ffplay, mpv, mpg123, vlc"
    exit 1
  fi
}

ensure_optional_ffmpeg() {
  if command_exists ffmpeg || command_exists avconv; then
    return
  fi

  echo "Installing optional ffmpeg (for loudness analysis and cover extraction)..."
  if ! install_with_manager ffmpeg; then
    echo "Optional dependency not installed: ffmpeg/avconv"
  fi
}

install_dotnet_sdk_if_needed
ensure_required_audio_backend
ensure_optional_ffmpeg

dotnet restore "${ROOT_DIR}/VibeVault.csproj"
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
