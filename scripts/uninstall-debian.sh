#!/usr/bin/env bash
set -euo pipefail

# Purge: remove package and config (e.g. /etc). Remove flag to keep config.
PURGE="${1:-}"

if [[ "${PURGE}" == "--purge" ]]; then
  echo "Removing file-transfer-app and configuration..."
  sudo apt purge -y file-transfer-app || sudo dpkg --purge file-transfer-app
else
  echo "Removing file-transfer-app..."
  sudo apt remove -y file-transfer-app || sudo dpkg -r file-transfer-app
fi

echo "Uninstalled: file-transfer-app"
