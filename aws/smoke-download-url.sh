#!/bin/bash
# JiApp post-deploy smoke test — asserts the live MP3 download-link endpoint
# returns a PUBLIC downloadUrl (not the docker container hostname).
#
# Catches the G4.4 regression (2026-08-07) where App__PublicBaseUrl was missing
# and downloadUrl leaked "http://ytdownloader:6702/..." — phones then failed
# with "Unable to resolve host ytdownloader".
#
# Usage: bash aws/smoke-download-url.sh
# Exit 0 = PASS (public URL), Exit 1 = FAIL (leaked host, auth error, or other).
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
    "exp": now + 300,
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
try:
    url = (json.loads(body) or {}).get("downloadUrl") or ""
except Exception:
    pass

print(url)
print(netloc(url))
print(netloc(api_base))
PY
)"
DOWNLOAD_URL="${URL_PARTS[0]}"
DOWNLOAD_HOST="${URL_PARTS[1]}"
EXPECTED_HOST="${URL_PARTS[2]}"

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
exit 0
