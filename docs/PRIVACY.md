# Privacy Policy

**File Transfer App**  
Last updated: February 17, 2025

## Overview

File Transfer App is a cross-platform desktop application that lets you send and receive files and folders over your local network (LAN) using encrypted connections. This policy describes what data the application uses and where it is stored.

## Data We Do Not Collect

- **No account or sign-in.** The app does not require registration or login.
- **No analytics or telemetry.** The app does not send usage data, crash reports, or diagnostics to any external server.
- **No cloud storage.** File transfers happen directly between your device and other devices on your local network. No files or metadata are sent to third-party or cloud services.

## Data Stored Locally on Your Device

All data used by the app is stored only on your computer, in the application’s directory (or equivalent app data location on your platform). Nothing is uploaded to the internet for storage or analysis.

### Settings (`settings.json`)

- **Download folder** – Path where received files are saved (e.g. `C:\Users\You\Downloads`).
- **Last selected target IP** – The most recently chosen peer device IP (e.g. from the device list or a `filetransfer://` link).
- **Previously scanned IPs** – IP addresses discovered on your LAN during device detection, so the app can suggest them again.
- **Maximum parallel uploads** – Your preference for how many uploads run at once (1–16).
- **Theme** – Your choice of System, Light, or Dark theme.
- **Trusted peers** – For each peer you have explicitly trusted:
  - A peer identifier (derived from the connection).
  - The TLS certificate fingerprint for that peer (used for “trust on first use” / TOFU).

These settings are used only to provide and improve your experience inside the app (e.g. default download location, device list, theme, and which peers are trusted).

### TLS and Certificates

- **Server certificate** – A TLS certificate (stored as `tls-server.pfx` in the app directory) is created and used so other devices can connect to your app over TLS. It is generated and stored locally; it is not sent to any central server.
- **Trusted peer fingerprints** – When you trust another device (e.g. after the first connection), the app stores that device’s certificate fingerprint locally so it can recognize and trust that device in future transfers.

## How File Transfers Work

- Transfers use **TCP with TLS** between your device and the other device(s) on your LAN.
- Data flows **directly** between the two devices. No intermediary or relay server is used.
- You choose which device to send to (e.g. by IP or from the detected device list) and which files or folders to send. Received files are saved to the download folder you configured in settings.

## Protocol Activation (`filetransfer://`)

If you open a link such as `filetransfer://192.168.1.25/send` or `filetransfer://open?target=192.168.1.25`, the app may store the target IP address in your local settings as “last selected target” so it can pre-fill or suggest that device. This is stored only on your device.

## Your Choices and Control

- **Change or clear settings** – You can change the download folder, theme, parallel uploads, and device list in Settings. Clearing or changing the “last selected target” or device list is done through the app.
- **Remove trusted peers** – In Settings you can “Re-trust selected” or otherwise manage which peers are trusted; this updates the locally stored trusted peer list.
- **Delete local data** – Uninstalling the app (and, if you wish, deleting its folder or app data) removes `settings.json`, the TLS certificate file, and any other local app data. Debian uninstall can be run with `--purge` to also remove configuration.

## Data Sharing

We do not sell, rent, or share your data. The app does not send your settings, file names, file contents, or any other information to us or to any third party. Communication is only between your device and other devices you choose on your local network.

## Third-Party Software

The app is built with frameworks such as **Avalonia** and **.NET**. Their use does not change the fact that we do not collect or transmit your data; any behavior of those components is governed by their respective licenses and policies.

## Changes to This Policy

We may update this privacy policy from time to time. The “Last updated” date at the top will be revised when we do. Continued use of the app after changes means you accept the updated policy.

## Contact

If you have questions about this privacy policy or how the app handles data, please open an issue or contact the project maintainers through the repository or contact channel you use for this project.
