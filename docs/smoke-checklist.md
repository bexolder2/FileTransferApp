# Smoke Checklist

## Core App

- Launch app and verify Main page opens.
- Open Settings page and return to Main.
- Verify minimum window size stays at least 500x500.

## Settings Flow

- Click **Detect devices** and confirm list updates.
- Change **Maximum parallel uploads**, save, restart app, verify value persists.
- Change download folder via **Browse**, save, restart app, verify value persists.
- Switch theme between System/Light/Dark and verify realtime update.

## Transfer Flow

- Queue one file and one folder.
- Start upload and verify button switches to **Cancel**.
- Verify progress updates for file count and bytes.
- Cancel upload and verify state returns to non-uploading.

## TLS TOFU

- First transfer to a peer should succeed and trust certificate.
- Replace peer certificate (or clear trust entry) and verify mismatch blocks transfer.
- Use **Re-trust selected** and verify next transfer re-establishes trust.

## Protocol Activation

- Trigger `filetransfer://open?target=<peer-ip>` and verify target IP is stored.
- Trigger `filetransfer://<peer-ip>/send` and verify target IP is stored.

## Packaging

- Build Windows package and verify artifact appears in `bin/`.
- Build Debian package and verify `.deb` appears in `bin/`.
- Install package and verify protocol registration is active.
