#!/usr/bin/env bash
# JiApp APK Publisher — upload a release APK + metadata to the public S3 downloads bucket.
# Usage: ./publish-apk.sh [--apk <path>] [--version <name>] [--dry-run] [--help]
set -euo pipefail

# ── Helpers ──────────────────────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[0;33m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

info()    { echo -e "${CYAN}[INFO]${NC}    $*"; }
success() { echo -e "${GREEN}[OK]${NC}      $*"; }
warn()    { echo -e "${YELLOW}[WARN]${NC}    $*"; }
error()   { echo -e "${RED}[ERROR]${NC}   $*"; }

# ── Paths ────────────────────────────────────────────────────────────────
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
DIST_DIR="$SCRIPT_DIR/dist"

# ── Flags ────────────────────────────────────────────────────────────────
APK_PATH=""
VERSION_NAME=""
DRY_RUN=false
PRINT_METADATA=false

print_help() {
    echo "Usage: $0 [--apk <path>] [--version <name>] [--dry-run] [--print-metadata] [--help]"
    echo ""
    echo "  --apk <path>      Publish a specific APK file (default: newest dist/JiAppMobile-*-release.apk)"
    echo "  --version <name>  Human-readable version name (default: derived from versionCode — e.g. \"1.0.0+42\")"
    echo "  --dry-run         Resolve APK, parse metadata, compute hashes, but do NOT upload to S3"
    echo "  --print-metadata  Resolve APK, compute metadata, print ONLY the apk-metadata.json to stdout"
    echo "  --help, -h        Show this help"
    echo ""
    echo "  Examples:"
    echo "    $0                              # publish newest release APK"
    echo "    $0 --dry-run                    # preview what would be published"
    echo "    $0 --apk dist/JiAppMobile-42-release.apk --version \"1.2.0\""
    exit 0
}

while [ $# -gt 0 ]; do
    case "$1" in
        --apk)
            APK_PATH="$2"
            shift 2
            ;;
        --version)
            VERSION_NAME="$2"
            shift 2
            ;;
        --dry-run)
            DRY_RUN=true
            shift
            ;;
        --print-metadata)
            PRINT_METADATA=true
            shift
            ;;
        --help|-h)
            print_help
            ;;
        *)
            error "Unknown flag: $1. Use --help for usage."
            exit 1
            ;;
    esac
done

# ── Print-metadata: redirect all log output to stderr so stdout stays pure JSON
if $PRINT_METADATA; then
    info()    { echo -e "${CYAN}[INFO]${NC}    $*" >&2; }
    success() { echo -e "${GREEN}[OK]${NC}      $*" >&2; }
    warn()    { echo -e "${YELLOW}[WARN]${NC}    $*" >&2; }
    error()   { echo -e "${RED}[ERROR]${NC}   $*" >&2; }
fi

# ── Load env ─────────────────────────────────────────────────────────────
if ! $PRINT_METADATA; then
    ENV_FILE="$SCRIPT_DIR/aws/.env"
    if [ -f "$ENV_FILE" ]; then
        set -a
        source "$ENV_FILE"
        set +a
        success "aws/.env loaded"
    else
        error "aws/.env not found at $ENV_FILE"
        error "This file is required — it contains AWS account/region config (gitignored)."
        exit 1
    fi

    REGION="${AWS_REGION:-eu-central-1}"
    ACCOUNT_ID="${AWS_ACCOUNT_ID:-}"
    if [ -z "$ACCOUNT_ID" ]; then
        error "AWS_ACCOUNT_ID not set in aws/.env"
        exit 1
    fi

    DOWNLOADS_BUCKET="jiapp-downloads-${ACCOUNT_ID}"
fi

# ── Resolve APK ──────────────────────────────────────────────────────────
if [ -n "$APK_PATH" ]; then
    # Explicit path — verify it exists
    if [ ! -f "$APK_PATH" ]; then
        error "APK not found: $APK_PATH"
        exit 1
    fi
else
    # Default: newest dist/JiAppMobile-*-release.apk
    APK_PATH=$(ls -1t "$DIST_DIR"/JiAppMobile-*-release.apk 2>/dev/null | head -1)
    if [ -z "$APK_PATH" ]; then
        error "No release APK found in $DIST_DIR/"
        error "Run build-apk.sh --prod --release first, or pass --apk <path>."
        exit 1
    fi
fi

APK_FILENAME=$(basename "$APK_PATH")
info "APK: $APK_PATH"

# ── Parse versionCode from filename ──────────────────────────────────────
# Expected pattern: JiAppMobile-{versionCode}-{variant}.apk
VERSION_CODE=$(echo "$APK_FILENAME" | sed -nE 's/^JiAppMobile-([0-9]+)-.*\.apk$/\1/p')
if [ -z "$VERSION_CODE" ]; then
    error "Could not parse versionCode from filename: $APK_FILENAME"
    error "Expected pattern: JiAppMobile-{versionCode}-{variant}.apk"
    exit 1
fi
success "versionCode: $VERSION_CODE"

# ── Version name ─────────────────────────────────────────────────────────
if [ -z "$VERSION_NAME" ]; then
    # Derive from versionCode: "1.0.0+42"
    # Try to find versionName from the mobile build.gradle as a better fallback
    GRADLE_FILE="$SCRIPT_DIR/mobile/android/app/build.gradle"
    if [ -f "$GRADLE_FILE" ]; then
        GRADLE_VERSION=$(grep -oP 'versionName "\K[^"]+' "$GRADLE_FILE" 2>/dev/null || true)
        if [ -n "$GRADLE_VERSION" ]; then
            VERSION_NAME="${GRADLE_VERSION}+${VERSION_CODE}"
        else
            VERSION_NAME="0.0.0+${VERSION_CODE}"
        fi
    else
        VERSION_NAME="0.0.0+${VERSION_CODE}"
    fi
    info "Derived version name: $VERSION_NAME (from build.gradle versionName + versionCode)"
fi

# ── Compute size & hash ──────────────────────────────────────────────────
if ! command -v jq &>/dev/null; then
    error "jq is not installed. Please install jq (apt install jq / brew install jq)."
    exit 1
fi

SIZE_BYTES=$(stat -c%s "$APK_PATH" 2>/dev/null || stat -f%z "$APK_PATH" 2>/dev/null)
SHA256=$(sha256sum "$APK_PATH" | awk '{print $1}')
RELEASE_DATE=$(date -u +%Y-%m-%d)

success "sizeBytes:   $SIZE_BYTES"
success "sha256:      $SHA256"
success "releaseDate: $RELEASE_DATE"

[ -n "$SIZE_BYTES" ] || { error "Failed to determine APK size"; exit 1; }
[ -n "$SHA256" ]    || { error "Failed to compute APK sha256"; exit 1; }

# ── Print-metadata output ─────────────────────────────────────────────────
if $PRINT_METADATA; then
    jq -n \
        --arg version "$VERSION_NAME" \
        --argjson versionCode "$VERSION_CODE" \
        --argjson sizeBytes "$SIZE_BYTES" \
        --arg releaseDate "$RELEASE_DATE" \
        --arg sha256 "$SHA256" \
        '{version:$version, versionCode:$versionCode, sizeBytes:$sizeBytes, releaseDate:$releaseDate, sha256:$sha256}'
    exit 0
fi

# ── S3 object keys ───────────────────────────────────────────────────────
LATEST_KEY="JiApp-latest.apk"
VERSIONED_KEY="JiApp-${VERSION_CODE}.apk"
METADATA_KEY="apk-metadata.json"

BASE_URL="https://${DOWNLOADS_BUCKET}.s3.${REGION}.amazonaws.com"

# ── Dry-run output ───────────────────────────────────────────────────────
if $DRY_RUN; then
    echo ""
    echo -e "${CYAN}╔══════════════════════════════════════════════╗${NC}"
    echo -e "${CYAN}║           DRY RUN — no uploads               ║${NC}"
    echo -e "${CYAN}╚══════════════════════════════════════════════╝${NC}"
    echo ""
    echo "  Bucket:      s3://${DOWNLOADS_BUCKET}/"
    echo "  Region:      $REGION"
    echo ""
    echo "  Would upload:"
    echo "    1. $APK_PATH"
    echo "       → s3://${DOWNLOADS_BUCKET}/${LATEST_KEY}"
    echo "         Content-Type: application/vnd.android.package-archive"
    echo "         Cache-Control: public, max-age=300"
    echo "    2. $APK_PATH"
    echo "       → s3://${DOWNLOADS_BUCKET}/${VERSIONED_KEY}"
    echo "         Content-Type: application/vnd.android.package-archive"
    echo "         Cache-Control: public, max-age=31536000, immutable"
    echo "    3. apk-metadata.json"
    echo "       → s3://${DOWNLOADS_BUCKET}/${METADATA_KEY}"
    echo "         Content-Type: application/json"
    echo "         Cache-Control: public, max-age=60"
    echo ""
    echo "  Metadata content:"
    METADATA_PREVIEW=$(jq -n \
        --arg version "$VERSION_NAME" \
        --argjson versionCode "$VERSION_CODE" \
        --argjson sizeBytes "$SIZE_BYTES" \
        --arg releaseDate "$RELEASE_DATE" \
        --arg sha256 "$SHA256" \
        '{version:$version, versionCode:$versionCode, sizeBytes:$sizeBytes, releaseDate:$releaseDate, sha256:$sha256}')
    echo "$METADATA_PREVIEW" | sed 's/^/    /'
    echo ""
    echo "  Public URLs (after upload):"
    echo "    Latest:   ${BASE_URL}/${LATEST_KEY}"
    echo "    Versioned: ${BASE_URL}/${VERSIONED_KEY}"
    echo "    Metadata:  ${BASE_URL}/${METADATA_KEY}"
    echo ""
    exit 0
fi

# ── Upload to S3 ─────────────────────────────────────────────────────────
echo ""
info "Uploading to s3://${DOWNLOADS_BUCKET}/ ..."

# 1. Latest APK (short cache — always serves the freshest)
aws s3 cp "$APK_PATH" "s3://${DOWNLOADS_BUCKET}/${LATEST_KEY}" \
    --region "$REGION" \
    --content-type "application/vnd.android.package-archive" \
    --cache-control "public, max-age=300" \
    --no-cli-pager
success "Uploaded: ${LATEST_KEY}"

# 2. Versioned APK (immutable — permanent archive)
aws s3 cp "$APK_PATH" "s3://${DOWNLOADS_BUCKET}/${VERSIONED_KEY}" \
    --region "$REGION" \
    --content-type "application/vnd.android.package-archive" \
    --cache-control "public, max-age=31536000, immutable" \
    --no-cli-pager
success "Uploaded: ${VERSIONED_KEY}"

# 3. Metadata JSON (built with jq so VERSION_NAME is escaped correctly)
METADATA_TMP=$(mktemp)
jq -n \
    --arg version "$VERSION_NAME" \
    --argjson versionCode "$VERSION_CODE" \
    --argjson sizeBytes "$SIZE_BYTES" \
    --arg releaseDate "$RELEASE_DATE" \
    --arg sha256 "$SHA256" \
    '{version:$version, versionCode:$versionCode, sizeBytes:$sizeBytes, releaseDate:$releaseDate, sha256:$sha256}' \
    > "$METADATA_TMP"
aws s3 cp "$METADATA_TMP" "s3://${DOWNLOADS_BUCKET}/${METADATA_KEY}" \
    --region "$REGION" \
    --content-type "application/json" \
    --cache-control "public, max-age=60" \
    --no-cli-pager
rm -f "$METADATA_TMP"
success "Uploaded: ${METADATA_KEY}"

# ── Done ──
echo ""
echo -e "${GREEN}╔══════════════════════════════════════════════╗${NC}"
echo -e "${GREEN}║           APK published!                      ║${NC}"
echo -e "${GREEN}╚══════════════════════════════════════════════╝${NC}"
echo ""
echo "  Version:     $VERSION_NAME  (code: $VERSION_CODE)"
echo "  Size:        $SIZE_BYTES bytes"
echo "  sha256:      $SHA256"
echo ""
echo "  Public URLs:"
echo "    Latest:    ${BASE_URL}/${LATEST_KEY}"
echo "    Versioned: ${BASE_URL}/${VERSIONED_KEY}"
echo "    Metadata:  ${BASE_URL}/${METADATA_KEY}"
echo ""
