---
name: android-deploy-local
description: Use when deploying the JiApp release APK to a local Android device over ADB — pushes the HTTPS dev cert for manual CA install, then builds and installs the latest release APK
type: flexible
---

# android-deploy-local — Deploy Release APK to Local Device

Deploys the JiApp release APK to a connected Android device with HTTPS dev cert trust. Covers the full pipeline: cert extraction → ADB push → user CA install → build → install APK.

## Prerequisites Check

Run these before proceeding. If any fail, tell the user what's missing.

| Check | Command |
|-------|---------|
| Backend running | `curl -sk https://{DEV_IP}:6703/api/v1/health` → expect `{"status":"healthy"}` |
| Dev cert exists | `ls backend/certs/dev-cert.pfx` |
| ADB sees device | `timeout 30 adb devices -l` — at least one `device` entry (not `offline`/`unauthorized`). Always use `timeout 30` to prevent hangs when ADB daemon is unresponsive. |
| Build script | `ls build-apk.sh` |

### ADB Connection (WSL2) — MANDATORY TCP MODE

**ALWAYS use TCP mode when running from WSL2.** ADB in WSL2 cannot access USB directly. The Windows ADB binary must NOT be used — always route through the WSL2 `adb` after switching the device to TCP mode. Every ADB command in this skill assumes you have completed these steps first.

**Step A: Switch device to TCP mode** (via Windows ADB — the ONLY time it's used):

```bash
/mnt/c/Users/jakub/AppData/Local/Android/Sdk/platform-tools/adb.exe tcpip 5555
```

**Step B: Get the device IP** (if unknown):

```bash
/mnt/c/Users/jakub/AppData/Local/Android/Sdk/platform-tools/adb.exe shell "ip addr show wlan0 | grep 'inet '"
```

**Step C: Connect from WSL2:**

```bash
timeout 30 adb connect <device_ip>:5555
```

**Step D: Verify:**

```bash
timeout 30 adb devices -l
```

The device should appear as `<device_ip>:5555` with status `device`. Use this TCP identifier with `-s <device_ip>:5555` for all subsequent ADB commands. If `adb devices` shows the USB entry as well, ignore it — always use the TCP entry (ends with `:5555`).

If the connection drops later, re-run Steps A and C.

## Deployment Steps

### Step 0: Check Certificate Status

Ask the user:

> Is the JiApp dev CA certificate already installed on the device? (Settings → Security & privacy → More security settings → Encryption & credentials → Trusted credentials → User tab → look for "JiApp Dev CA")

- **If yes** → skip to Step 4 (Build Release APK).
- **If no or unsure** → proceed through Steps 1–3 below.

### Step 1: Extract Dev Cert

Read the cert password from `backend/src/JiApp.Gateway/appsettings.Development.json` (path: `Kestrel.Endpoints.Https.Certificate.Password`), then:

```bash
openssl pkcs12 -in backend/certs/dev-cert.pfx \
  -passin pass:"<PASSWORD>" -nokeys -clcerts 2>/dev/null \
  | openssl x509 -outform PEM -out /tmp/jiapp-dev-ca.crt 2>/dev/null
```

### Step 2: Push Cert to Device

```bash
adb -s <device_id> push /tmp/jiapp-dev-ca.crt /sdcard/Download/
```

### Step 3: User Installs CA Certificate (MANUAL)

Tell the user:

> **On the phone:** Settings → Security & privacy → More security settings → Encryption & credentials → Install a certificate → CA certificate → select `jiapp-dev-ca.crt` from Downloads.
>
> Tell me when done.

**Wait for user confirmation before continuing.** The APK's network security config embeds this cert — if the device doesn't trust it, HTTPS connections will hang and time out.

### Step 4: Build Release APK

```bash
./build-apk.sh --release
```

This bumps `versionCode`, builds the signed release APK, and copies it to `dist/`.

### Step 5: Install APK to Device

The app package name is `com.jiappmobile`. First uninstall any existing version, then install:

```bash
adb -s <device_id> uninstall com.jiappmobile
adb -s <device_id> install -r dist/JiAppMobile-*-release.apk
```

If uninstall fails with `DELETE_FAILED_INTERNAL_ERROR` (common on Samsung devices), skip uninstall and use the downgrade flag:

```bash
adb -s <device_id> install -r -d dist/JiAppMobile-*-release.apk
```

### Step 6: Verify

Tell the user to open the app and test. The backend must be running in Development mode (`ASPNETCORE_ENVIRONMENT=Development`) for the dev cert endpoint to be active.

## Background: Why This Process Exists

The release APK embeds the dev certificate as a raw resource (`res/raw/jiapp_dev_ca`) and trusts it for the dev machine domain. This is necessary because **Android API 24+ ignores user-installed CAs for release builds**. Embedding the cert directly in the network security config bypasses this restriction while keeping the device-level CA install as an additional trust anchor.

## Network Troubleshooting

If the app spins and times out after deployment:

| Symptom | Diagnosis | Fix |
|----------|-----------|-----|
| App times out (30s spinner) | `adb shell "nc {DEV_IP} 6703 < /dev/null && echo OK \|\| echo FAIL"` | If FAIL → firewall issue |
| `nc` fails from device | Packet blocked before reaching WSL2 | Check firewall chain below |
| `nc` works but app fails | TLS issue | Re-check cert installation |
| Windows browser can't reach `https://{DEV_IP}:6703` | Hyper-V firewall active | `firewall=false` in `.wslconfig` |

### Firewall Chain (WSL2 Mirrored Networking)

Three layers must all allow traffic:

1. **Windows Defender Firewall** — Inbound rules for TCP 6701, 6703 (Profile: Any)
2. **Hyper-V Firewall** — `firewall=false` in `C:\Users\jakub\.wslconfig` under `[wsl2]`, then `wsl --shutdown` + restart
3. **Verify end-to-end:** Open `https://{DEV_IP}:6703/api/v1/health` in Windows browser — must return `{"status":"healthy"}`

## Notes

- If the dev machine IP changes, you must regenerate the PFX cert with the new IP in SANs and update `res/raw/jiapp_dev_ca`
- The raw cert resource and the PFX must stay in sync — rerun Step 1 whenever the PFX is regenerated
- This skill is JiApp-project-specific; the cert paths and IP are hardcoded to this setup
