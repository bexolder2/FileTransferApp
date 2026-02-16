# File Transfer App

Cross-platform desktop file transfer application built with Avalonia and .NET 10.
The app sends and receives files/folders over TCP + TLS streaming and uses trust-on-first-use (TOFU) for peer certificates on LAN.

## Architecture

- `src/FileTransfer.App`: Avalonia UI, startup, DI, platform services.
- `src/FileTransfer.ViewModels`: MVVM state and commands for Main/Settings pages.
- `src/FileTransfer.Core`: contracts and domain models.
- `src/FileTransfer.Infrastructure`: settings persistence, LAN scanning, transfer protocol and transport, TOFU, protocol activation.
- `test/FileTransfer.UnitTests`: unit tests for settings, activation, trust logic, orchestration behavior.
- `test/FileTransfer.IntegrationTests`: integration tests for protocol framing and end-to-end transfer path.

## Requirements

- .NET SDK 10.x
- Windows or Linux
- Optional:
  - WiX v4 CLI (`wix`) for MSI generation on Windows
  - `dpkg-deb` for Debian package generation on Linux

## Run Locally

```bash
dotnet build FileTransfer.slnx
dotnet run --project src/FileTransfer.App/FileTransfer.App.csproj
```

## Tests

```bash
dotnet test FileTransfer.slnx
```

Integration tests are marked with `Trait("Category", "Integration")`.

## Protocol Activation

The app supports the `filetransfer://` URI scheme.

Examples:

- `filetransfer://192.168.1.25/send`
- `filetransfer://open?target=192.168.1.25`

On startup with a protocol URI, the app stores the target IP into settings (`LastSelectedTargetIp`).

## Packaging

### Windows (WiX / fallback ZIP)

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\build-windows-installer.ps1
```

- If `wix` is installed, output is generated in `bin/file-transfer-app-windows-x64.msi`.
- Otherwise a ZIP fallback is generated in `bin/file-transfer-app-windows-x64.zip`.

### Debian

```bash
chmod +x ./scripts/build-debian-package.sh
./scripts/build-debian-package.sh
```

Output: `bin/file-transfer-app_1.0.0_amd64.deb`

## Protocol Registration Helpers

- Windows current-user protocol registration:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\register-protocol-windows.ps1
```

- Debian/local Linux protocol registration:

```bash
chmod +x ./scripts/register-protocol-debian.sh
./scripts/register-protocol-debian.sh
```

## Troubleshooting

- Device discovery returns empty list:
  - Ensure peers are on the same subnet.
  - Allow ICMP and TCP port `50505` in local firewall.
- TLS mismatch blocks transfer:
  - Use **Re-trust selected** in Settings and retry.
- Activation URI does not open app:
  - Re-register protocol handlers using scripts in `scripts/`.

## Smoke Checklist

See `docs/smoke-checklist.md`.
