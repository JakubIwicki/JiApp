#!/usr/bin/env bash
set -euo pipefail

# ── Colors ──────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

# ── Config ──────────────────────────────────────────────
BINARY_NAME="yt-dlp"
INSTALL_DIR="${HOME}/.local/bin"
INSTALL_PATH="${INSTALL_DIR}/${BINARY_NAME}"

REPO="yt-dlp/yt-dlp"
API_URL="https://api.github.com/repos/${REPO}/releases/latest"

echo -e "${CYAN}${BOLD}yt-dlp Updater${NC}"
echo ""

# ── Resolve latest version ──────────────────────────────
echo -e "Fetching latest release from ${CYAN}${REPO}${NC}..."
LATEST_TAG=$(curl -sL "${API_URL}" | grep '"tag_name":' | sed -E 's/.*"tag_name": *"([^"]*)".*/\1/')

if [ -z "${LATEST_TAG}" ]; then
    echo -e "${RED}ERROR: Could not determine latest version.${NC}"
    exit 1
fi

echo -e "  Latest: ${GREEN}${LATEST_TAG}${NC}"

# ── Check current version ───────────────────────────────
if command -v "${BINARY_NAME}" &>/dev/null; then
    CURRENT_VER=$("${BINARY_NAME}" --version 2>/dev/null || echo "unknown")
    echo -e "  Current: ${YELLOW}${CURRENT_VER}${NC}"
    if [ "${CURRENT_VER}" = "${LATEST_TAG}" ]; then
        echo -e "\n${GREEN}${BOLD}Already up to date.${NC}"
        exit 0
    fi
else
    echo -e "  Current: ${YELLOW}not installed${NC}"
fi

# ── Download ─────────────────────────────────────────────
DOWNLOAD_URL="https://github.com/${REPO}/releases/download/${LATEST_TAG}/${BINARY_NAME}"
echo -e "\nDownloading ${CYAN}${LATEST_TAG}${NC}..."

mkdir -p "${INSTALL_DIR}"
curl -L --progress-bar "${DOWNLOAD_URL}" -o "${INSTALL_PATH}"
chmod +x "${INSTALL_PATH}"

echo -e "\n${GREEN}${BOLD}✓ yt-dlp ${LATEST_TAG} installed to ${INSTALL_PATH}${NC}"

# ── PATH check ──────────────────────────────────────────
if [[ ":$PATH:" != *":${INSTALL_DIR}:"* ]]; then
    echo -e "\n${YELLOW}${BOLD}⚠  ${INSTALL_DIR} is not in PATH${NC}"
    echo -e "   Add this to your shell profile:"
    echo -e "   ${CYAN}export PATH=\"\${HOME}/.local/bin:\${PATH}\"${NC}"
    echo ""
    echo -e "   Or use the full path directly:"
    echo -e "   ${CYAN}${INSTALL_PATH}${NC}"
fi

# ── Verify ───────────────────────────────────────────────
echo -e "\nVerifying..."
"${INSTALL_PATH}" --version
echo -e "${GREEN}${BOLD}Done.${NC}"
