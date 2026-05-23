#!/usr/bin/env bash
set -euo pipefail

# ── Colors ──────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PID_FILE="$SCRIPT_DIR/backend/.api-pid"

echo -e "${CYAN}${BOLD}╭──────────────────────────────────────────╮${NC}"
echo -e "${CYAN}${BOLD}│${NC}         ${BOLD}JiApp API — Stop${NC}                    ${CYAN}${BOLD}│${NC}"
echo -e "${CYAN}${BOLD}╰──────────────────────────────────────────╯${NC}"
echo ""

# ── Check if PID file exists ────────────────────────────
if [ ! -f "$PID_FILE" ]; then
    echo -e "${YELLOW}No running API found (PID file missing: ${PID_FILE}).${NC}"
    exit 0
fi

API_PID=$(cat "$PID_FILE")

# ── Check if process is running ─────────────────────────
if ! kill -0 "$API_PID" 2>/dev/null; then
    echo -e "${YELLOW}Process $API_PID is not running. Cleaning up stale PID file.${NC}"
    rm -f "$PID_FILE"
    exit 0
fi

echo -e "  PID: ${BOLD}$API_PID${NC}"

# ── Graceful shutdown (SIGTERM) ─────────────────────────
echo -n "  Sending SIGTERM..."
kill "$API_PID"
echo -e " ${GREEN}sent${NC}"

# ── Wait for graceful shutdown ──────────────────────────
TIMEOUT=10
for ((i=1; i<=TIMEOUT; i++)); do
    if ! kill -0 "$API_PID" 2>/dev/null; then
        echo -e "\n${GREEN}${BOLD}✓ API stopped gracefully${NC} (after ${i}s)"
        rm -f "$PID_FILE"
        exit 0
    fi
    sleep 1
done

# ── Force kill if still running ─────────────────────────
echo -e "\n${YELLOW}Process did not stop after ${TIMEOUT}s. Sending SIGKILL...${NC}"
kill -9 "$API_PID" 2>/dev/null || true
sleep 1

if ! kill -0 "$API_PID" 2>/dev/null; then
    echo -e "${YELLOW}${BOLD}✓ API stopped forcefully${NC}"
else
    echo -e "${RED}${BOLD}✗ Failed to stop API (PID $API_PID)${NC}"
    exit 1
fi

rm -f "$PID_FILE"
