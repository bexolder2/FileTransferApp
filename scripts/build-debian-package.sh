#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="${1:-Release}"
ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUBLISH_DIR="${ROOT_DIR}/artifacts/publish/linux-x64"
PACKAGE_ROOT="${ROOT_DIR}/artifacts/debian-package"
BIN_DIR="${ROOT_DIR}/bin"
VERSION="1.0.0"
PACKAGE_NAME="file-transfer-app_${VERSION}_amd64.deb"

mkdir -p "${PUBLISH_DIR}" "${PACKAGE_ROOT}" "${BIN_DIR}"

dotnet publish "${ROOT_DIR}/src/FileTransfer.App/FileTransfer.App.csproj" \
  -c "${CONFIGURATION}" \
  -r linux-x64 \
  --self-contained false \
  -o "${PUBLISH_DIR}"

rm -rf "${PACKAGE_ROOT:?}"/*
mkdir -p "${PACKAGE_ROOT}/DEBIAN" "${PACKAGE_ROOT}/opt/file-transfer-app" "${PACKAGE_ROOT}/usr/share/applications"

cp "${ROOT_DIR}/installer/linux/debian/control" "${PACKAGE_ROOT}/DEBIAN/control"
cp "${ROOT_DIR}/installer/linux/debian/postinst" "${PACKAGE_ROOT}/DEBIAN/postinst"
cp "${ROOT_DIR}/installer/linux/debian/prerm" "${PACKAGE_ROOT}/DEBIAN/prerm"
cp "${ROOT_DIR}/installer/linux/debian/postrm" "${PACKAGE_ROOT}/DEBIAN/postrm"
chmod 0755 "${PACKAGE_ROOT}/DEBIAN/postinst" "${PACKAGE_ROOT}/DEBIAN/prerm" "${PACKAGE_ROOT}/DEBIAN/postrm"

cp -R "${PUBLISH_DIR}/." "${PACKAGE_ROOT}/opt/file-transfer-app/"
cp "${ROOT_DIR}/installer/linux/debian/file-transfer-app.desktop" "${PACKAGE_ROOT}/usr/share/applications/file-transfer-app.desktop"

dpkg-deb --build "${PACKAGE_ROOT}" "${BIN_DIR}/${PACKAGE_NAME}"
echo "Created Debian package: ${BIN_DIR}/${PACKAGE_NAME}"
