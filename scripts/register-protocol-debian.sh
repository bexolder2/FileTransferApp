#!/usr/bin/env bash
set -euo pipefail

SCHEME="filetransfer"
APP_PATH="${1:-$HOME/.local/bin/file-transfer-app}"
DESKTOP_FILE="$HOME/.local/share/applications/file-transfer-app.desktop"

mkdir -p "$(dirname "$DESKTOP_FILE")"

cat > "$DESKTOP_FILE" <<EOF
[Desktop Entry]
Name=File Transfer App
Exec=${APP_PATH} %u
Type=Application
Terminal=false
MimeType=x-scheme-handler/${SCHEME};
EOF

update-desktop-database "$HOME/.local/share/applications" || true
xdg-mime default "$(basename "$DESKTOP_FILE")" "x-scheme-handler/${SCHEME}" || true

echo "Registered protocol '${SCHEME}' using ${DESKTOP_FILE}"
