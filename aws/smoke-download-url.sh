#!/bin/bash
# JiApp post-deploy smoke test — verifies the live MP3 download pipeline end to
# end: the download-link endpoint returns a PUBLIC downloadUrl (not the docker
# container hostname), the job reaches "ready", and the actual MP3 file
# downloads with a non-trivial size (>100 KB).
#
# Catches the G4.4 regression (2026-08-07) where App__PublicBaseUrl was missing
# and downloadUrl leaked "http://ytdownloader:6702/..." — phones then failed
# with "Unable to resolve host ytdownloader". The real download also catches
# media-URL 403s (2026-08-16, PR #163 forced the tv player client) that a
# URL-format check alone cannot see.
#
# Usage: bash aws/smoke-download-url.sh
# Exit 0 = PASS (public URL + live download), Exit 1 = FAIL (leaked host, auth
# error, 403, timeout, or other).
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Source AWS identifiers (AWS_REGION is unused here but harmless).
if [ -f "$SCRIPT_DIR/.env" ]; then
    set -a; source "$SCRIPT_DIR/.env"; set +a
fi

# Source prod secrets — JWT_KEY is required to mint the token.
if [ ! -f "$SCRIPT_DIR/.env.prod" ]; then
    echo "FAIL: aws/.env.prod not found — required for JWT_KEY" >&2
    exit 1
fi
set -a; source "$SCRIPT_DIR/.env.prod"; set +a

if [ -z "${JWT_KEY:-}" ]; then
    echo "FAIL: JWT_KEY is empty/missing in aws/.env.prod — cannot mint a signed JWT" >&2
    exit 1
fi

# Gateway base from YT_PUBLIC_BASE_URL; fall back to the known prod value.
API_BASE="${YT_PUBLIC_BASE_URL:-https://18.153.244.141:6700}"
API_BASE="${API_BASE%/}"
ENDPOINT="${API_BASE}/api/v1/yt/downloads/mp3"

# Mint a short-lived HS256 JWT (iss/aud are the PROD values — NOT the
# appsettings dev "JiApp-Identity"/"jiapp-gateway"). PyJWT when available,
# otherwise a stdlib-only HMAC-SHA256 implementation. The key is passed via the
# environment, never on argv and never echoed.
TOKEN="$(JWT_KEY="$JWT_KEY" python3 - <<'PY'
import base64, hashlib, hmac, json, os, sys, time, uuid

key = os.environ["JWT_KEY"].encode("utf-8")
now = int(time.time())

payload = {
    "iss": "JiApp",
    "aud": "JiAppMobile",
    "exp": now + 600,
    "jti": str(uuid.uuid4()),
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier": "1",
    "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name": "SmokeTest",
    "permission": ["ytdownloader.access"],
}
header = {"alg": "HS256", "typ": "JWT"}

def b64url(data: bytes) -> str:
    return base64.urlsafe_b64encode(data).rstrip(b"=").decode("ascii")

def sign(header: dict, payload: dict, key: bytes) -> str:
    seg = (
        b64url(json.dumps(header, separators=(",", ":"), sort_keys=True).encode("utf-8"))
        + "."
        + b64url(json.dumps(payload, separators=(",", ":"), sort_keys=True).encode("utf-8"))
    )
    sig = b64url(hmac.new(key, seg.encode("ascii"), hashlib.sha256).digest())
    return f"{seg}.{sig}"

try:
    import jwt  # PyJWT
    token = jwt.encode(payload, os.environ["JWT_KEY"], algorithm="HS256", headers=header)
except ImportError:
    token = sign(header, payload, key)
print(token)
PY
)"

# POST to the download-link endpoint, capturing HTTP code separately.
RESP="$(curl -sk --max-time 30 -w $'\n%{http_code}' -X POST \
    -H "Authorization: Bearer ${TOKEN}" \
    -H "Content-Type: application/json" \
    -d '{"videoId":"dQw4w9WgXcQ","videoUrl":"https://www.youtube.com/watch?v=dQw4w9WgXcQ","title":"Smoke Test","description":"","imageUrl":""}' \
    "${ENDPOINT}" 2>/dev/null || true)"

HTTP_CODE=""
BODY=""
if [ -n "${RESP}" ]; then
    HTTP_CODE="$(printf '%s' "${RESP}" | tail -n1)"
    BODY="$(printf '%s' "${RESP}" | sed '$d')"
fi

if [ "${HTTP_CODE}" != "200" ]; then
    if [ -n "${HTTP_CODE}" ]; then
        echo "FAIL: download-link endpoint returned HTTP ${HTTP_CODE}"
    else
        echo "FAIL: no HTTP response from ${ENDPOINT} (network/TLS?)"
    fi
    if [ -n "${BODY}" ]; then
        echo "FAIL: response: $(printf '%s' "${BODY}" | head -c 500)"
    fi
    exit 1
fi

# Parse downloadUrl + host from the JSON response, and the expected host from
# API_BASE. The body is passed via an env var (NOT stdin — stdin is consumed by
# the heredoc that supplies the python script itself).
mapfile -t URL_PARTS <<< "$(BODY="${BODY}" API_BASE="${API_BASE}" python3 - <<'PY'
import json, os, urllib.parse

api_base = os.environ["API_BASE"]
body = os.environ["BODY"]

def netloc(url: str) -> str:
    try:
        return urllib.parse.urlparse(url).netloc
    except Exception:
        return ""

url = ""
temp_id = ""
try:
    parsed = json.loads(body) or {}
    url = parsed.get("downloadUrl") or ""
    temp_id = parsed.get("tempId") or ""
except Exception:
    pass

print(url)
print(netloc(url))
print(netloc(api_base))
print(temp_id)
PY
)"
DOWNLOAD_URL="${URL_PARTS[0]:-}"
DOWNLOAD_HOST="${URL_PARTS[1]:-}"
EXPECTED_HOST="${URL_PARTS[2]:-}"
TEMP_ID="${URL_PARTS[3]:-}"

if [ -z "${DOWNLOAD_URL}" ]; then
    echo "FAIL: HTTP 200 but response contained no downloadUrl: ${BODY}" >&2
    exit 1
fi

if [[ "${DOWNLOAD_HOST,,}" == *"ytdownloader"* ]]; then
    echo "FAIL: downloadUrl host leaked the docker hostname: ${DOWNLOAD_HOST}"
    echo "FAIL: downloadUrl = ${DOWNLOAD_URL}"
    exit 1
fi

if [ "${DOWNLOAD_HOST}" != "${EXPECTED_HOST}" ]; then
    echo "FAIL: downloadUrl host ${DOWNLOAD_HOST} != expected public host ${EXPECTED_HOST}"
    echo "FAIL: downloadUrl = ${DOWNLOAD_URL}"
    exit 1
fi

echo "PASS: downloadUrl is public (host ${DOWNLOAD_HOST})"
echo "PASS: ${DOWNLOAD_URL}"

# ── Live download verification ──────────────────────────────────────────────
# The link-format checks above cannot catch a YouTube media-URL 403 (2026-08-16,
# PR #163): the returned URL is public, but yt-dlp's player client was being
# rejected. So actually run the job and download the MP3.

if [ -z "${TEMP_ID}" ]; then
    echo "FAIL: HTTP 200 but response contained no tempId: ${BODY}" >&2
    exit 1
fi

STATUS_URL="${API_BASE}/api/v1/yt/downloads/mp3/status/${TEMP_ID}"
MAX_POLLS=30
POLL=0
STATUS=""
ERROR_MSG=""
while [ "${POLL}" -lt "${MAX_POLLS}" ]; do
    POLL=$((POLL + 1))
    RESP="$(curl -sk --max-time 30 -w $'\n%{http_code}' \
        -H "Authorization: Bearer ${TOKEN}" \
        "${STATUS_URL}" 2>/dev/null || true)"
    HTTP_CODE=""
    BODY=""
    if [ -n "${RESP}" ]; then
        HTTP_CODE="$(printf '%s' "${RESP}" | tail -n1)"
        BODY="$(printf '%s' "${RESP}" | sed '$d')"
    fi

    # Parse status + error from the poll response. Body passed via env var
    # (matches the heredoc style used above).
    mapfile -t STATUS_PARTS <<< "$(BODY="${BODY}" python3 - <<'PY'
import json, os

body = os.environ["BODY"]
status = ""
error = ""
try:
    parsed = json.loads(body) or {}
    status = parsed.get("status") or ""
    error = parsed.get("error") or parsed.get("errorMessage") or parsed.get("message") or ""
except Exception:
    pass
print(status)
print(error)
PY
)"
    STATUS="${STATUS_PARTS[0]:-}"
    ERROR_MSG="${STATUS_PARTS[1]:-}"

    case "${STATUS}" in
        ready)
            echo "PASS: job ${TEMP_ID} reached status 'ready' after ${POLL} poll(s)"
            break
            ;;
        failed)
            echo "FAIL: download job ${TEMP_ID} failed: ${ERROR_MSG:-${BODY}}" >&2
            exit 1
            ;;
        *)
            echo "    [${POLL}/${MAX_POLLS}] status: ${STATUS:-<empty/http ${HTTP_CODE}>} — waiting..."
            sleep 5
            ;;
    esac
done

if [ "${STATUS}" != "ready" ]; then
    echo "FAIL: timed out waiting for job ${TEMP_ID} to become 'ready' (last status: ${STATUS:-<none>}, last HTTP: ${HTTP_CODE:-<none>})" >&2
    exit 1
fi

# Fetch the actual MP3; require HTTP 200 AND a payload >100 KB. A 403/error
# page can still return HTTP 200 with a tiny body, so size is the real signal.
FILE_RESP="$(curl -sk --max-time 120 -o /dev/null -w '%{http_code} %{size_download}' \
    -H "Authorization: Bearer ${TOKEN}" \
    "${API_BASE}/api/v1/yt/downloads/mp3/file/${TEMP_ID}" 2>/dev/null || true)"
FILE_HTTP_CODE="$(printf '%s' "${FILE_RESP}" | awk '{print $1}')"
FILE_SIZE="$(printf '%s' "${FILE_RESP}" | awk '{print $2}')"

if [ "${FILE_HTTP_CODE}" != "200" ]; then
    echo "FAIL: download file endpoint returned HTTP ${FILE_HTTP_CODE:-<none>}" >&2
    exit 1
fi

if [ -z "${FILE_SIZE}" ] || [ "${FILE_SIZE}" -le 100000 ]; then
    echo "FAIL: downloaded payload too small (${FILE_SIZE:-0} bytes) — likely a 403/error body, not an MP3" >&2
    exit 1
fi

echo "PASS: live download verified (${FILE_SIZE} bytes)"
exit 0
