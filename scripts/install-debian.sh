#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BIN_DIR="${ROOT_DIR}/bin"
VERSION="${1:-1.0.0}"
PACKAGE_NAME="file-transfer-app_${VERSION}_amd64.deb"

# Allow passing path to .deb explicitly (e.g. ./scripts/install-debian.sh bin/file-transfer-app_1.0.0_amd64.deb)
if [[ -f "${1:-}" && "${1}" == *.deb ]]; then
  DEB_PATH="$(cd "$(dirname "$1")" && pwd)/$(basename "$1")"
else
  DEB_PATH="${BIN_DIR}/${PACKAGE_NAME}"
fi

if [[ ! -f "$DEB_PATH" ]]; then
  echo "Error: Debian package not found: $DEB_PATH" >&2
  echo "Build it first with: ./scripts/build-debian-package.sh" >&2
  exit 1
fi

echo "Installing: $DEB_PATH"
sudo apt install -y "$DEB_PATH" 2>/dev/null || sudo dpkg -i "$DEB_PATH"

# Fix broken deps if dpkg was used (e.g. missing libssl3)
if ! dpkg -s file-transfer-app >/dev/null 2>&1; then
  sudo apt install -f -y
fi

# If user previously ran register-protocol-debian.sh, their desktop file points to
# ~/.local/bin/file-transfer-app and overrides the system one. Remove it so the
# system handler (/opt/file-transfer-app/FileTransfer.App) is used.
USER_DESKTOP="${HOME}/.local/share/applications/file-transfer-app.desktop"
if [[ -f "$USER_DESKTOP" ]] && grep -q '.local/bin/file-transfer-app' "$USER_DESKTOP" 2>/dev/null; then
  rm -f "$USER_DESKTOP"
  if command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "${HOME}/.local/share/applications" || true
  fi
  echo "Removed conflicting user protocol handler; system handler will be used."
fi

echo "Installed: file-transfer-app"
