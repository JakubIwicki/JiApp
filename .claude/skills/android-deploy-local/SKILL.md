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
| Backend running | `curl -sk https://192.168.100.105:6703/api/v1/health` → expect `{"status":"healthy"}` |
| Dev cert exists | `ls backend/src/JiApp.Api/Infrastructure/dev-cert.pfx` |
| ADB sees device | `adb devices` — at least one `device` entry (not `offline`/`unauthorized`) |
| Build script | `ls build-apk.sh` |

### ADB Connection (WSL2)

ADB in WSL2 cannot access USB directly. If the device is USB-connected to Windows, switch it to TCP mode first:

```bash
# From Windows side (via WSL interop):
/mnt/c/Users/jakub/AppData/Local/Android/Sdk/platform-tools/adb.exe tcpip 5555

# Then connect from WSL2:
adb connect <device_ip>:5555
```

Get the device IP if unknown:
```bash
/mnt/c/Users/jakub/AppData/Local/Android/Sdk/platform-tools/adb.exe shell "ip addr show wlan0 | grep 'inet '"
```

When multiple devices appear in `adb devices`, use `-s <device_id>` with every command. Prefer the TCP-connected entry (ends with `:5555`).

## Deployment Steps

### Step 0: Check Certificate Status

Ask the user:

> Is the JiApp dev CA certificate already installed on the device? (Settings → Security & privacy → More security settings → Encryption & credentials → Trusted credentials → User tab → look for "JiApp Dev CA")

- **If yes** → skip to Step 4 (Build Release APK).
- **If no or unsure** → proceed through Steps 1–3 below.

### Step 1: Extract Dev Cert

Read the cert password from `backend/src/JiApp.Api/appsettings.Development.json` (path: `Kestrel.Endpoints.Https.Certificate.Password`), then:

```bash
openssl pkcs12 -in backend/src/JiApp.Api/Infrastructure/dev-cert.pfx \
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

```bash
adb -s <device_id> install -r dist/JiAppMobile-*-release.apk
```

### Step 6: Verify

Tell the user to open the app and test. The backend must be running in Development mode (`ASPNETCORE_ENVIRONMENT=Development`) for the dev cert endpoint to be active.

## Background: Why This Process Exists

The release APK embeds the dev certificate as a raw resource (`res/raw/jiapp_dev_ca`) and trusts it for the `192.168.100.105` domain. This is necessary because **Android API 24+ ignores user-installed CAs for release builds**. Embedding the cert directly in the network security config bypasses this restriction while keeping the device-level CA install as an additional trust anchor.

## Network Troubleshooting

If the app spins and times out after deployment:

| Symptom | Diagnosis | Fix |
|----------|-----------|-----|
| App times out (30s spinner) | `adb shell "nc 192.168.100.105 6703 < /dev/null && echo OK \|\| echo FAIL"` | If FAIL → firewall issue |
| `nc` fails from device | Packet blocked before reaching WSL2 | Check firewall chain below |
| `nc` works but app fails | TLS issue | Re-check cert installation |
| Windows browser can't reach `https://192.168.100.105:6703` | Hyper-V firewall active | `firewall=false` in `.wslconfig` |

### Firewall Chain (WSL2 Mirrored Networking)

Three layers must all allow traffic:

1. **Windows Defender Firewall** — Inbound rules for TCP 6701, 6703 (Profile: Any)
2. **Hyper-V Firewall** — `firewall=false` in `C:\Users\jakub\.wslconfig` under `[wsl2]`, then `wsl --shutdown` + restart
3. **Verify end-to-end:** Open `https://192.168.100.105:6703/api/v1/health` in Windows browser — must return `{"status":"healthy"}`

## Notes

- If the dev machine IP changes, you must regenerate the PFX cert with the new IP in SANs and update `res/raw/jiapp_dev_ca`
- The raw cert resource and the PFX must stay in sync — rerun Step 1 whenever the PFX is regenerated
- This skill is JiApp-project-specific; the cert paths and IP are hardcoded to this setup
