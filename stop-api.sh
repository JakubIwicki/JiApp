#!/usr/bin/env bash
set -euo pipefail

# ── Colors ──────────────────────────────────────────────
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
CYAN='\033[0;36m'
BOLD='\033[1m'
NC='\033[0m'

# ── Helper: discover PID listening on a TCP port ───────
discover_pid_by_port() {
    local port="$1"
    local pid=""
    if command -v fuser &>/dev/null; then
        pid=$(fuser "$port/tcp" 2>/dev/null | grep -oP '\d+' | head -1)
    elif command -v lsof &>/dev/null; then
        pid=$(lsof -ti :"$port" 2>/dev/null | head -1)
    elif command -v ss &>/dev/null; then
        pid=$(ss -tlnp "sport = :$port" 2>/dev/null | grep -oP 'pid=\K\d+' | head -1)
    fi
    echo "$pid"
}

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PID_FILE="$SCRIPT_DIR/backend/.api-pid"

echo -e "${CYAN}${BOLD}╭──────────────────────────────────────────╮${NC}"
echo -e "${CYAN}${BOLD}│${NC}         ${BOLD}JiApp API — Stop${NC}                    ${CYAN}${BOLD}│${NC}"
echo -e "${CYAN}${BOLD}╰──────────────────────────────────────────╯${NC}"
echo ""

# ── Read stored PID (if any) ────────────────────────────
STORED_PID=""
if [ -f "$PID_FILE" ]; then
    STORED_PID=$(cat "$PID_FILE")
fi

# ── Discover actual app PIDs ────────────────────────────
APP_PIDS=()

# By port (primary — finds the actual process holding our ports)
for port in 5001 5003; do
    pid=$(discover_pid_by_port "$port")
    if [ -n "$pid" ]; then
        APP_PIDS+=("$pid")
    fi
done

# By process name (fallback for processes that may have released ports)
while IFS= read -r pid; do
    if [ -n "$pid" ] && [ "$pid" != "$$" ]; then
        APP_PIDS+=("$pid")
    fi
done < <(pgrep -f "JiApp\.Api" 2>/dev/null || true)

# Deduplicate
if [ ${#APP_PIDS[@]} -gt 0 ]; then
    mapfile -t APP_PIDS < <(printf "%s\n" "${APP_PIDS[@]}" | sort -u)
fi

# ── No processes found ──────────────────────────────────
if [ ${#APP_PIDS[@]} -eq 0 ]; then
    if [ -n "$STORED_PID" ]; then
        echo -e "${YELLOW}No app processes found. Cleaning up stale PID file.${NC}"
        rm -f "$PID_FILE"
    else
        echo -e "${YELLOW}No running API found.${NC}"
    fi
    exit 0
fi

echo -e "  Stopping PID(s): ${BOLD}${APP_PIDS[*]}${NC}"

# ── Graceful shutdown (SIGTERM) ─────────────────────────
echo -n "  Sending SIGTERM..."
for pid in "${APP_PIDS[@]}"; do
    kill "$pid" 2>/dev/null || true
done
echo -e " ${GREEN}sent${NC}"

# ── Wait for graceful shutdown ──────────────────────────
TIMEOUT=10
for ((i=1; i<=TIMEOUT; i++)); do
    all_dead=true
    for pid in "${APP_PIDS[@]}"; do
        if kill -0 "$pid" 2>/dev/null; then
            all_dead=false
            break
        fi
    done
    if $all_dead; then
        echo -e "\n${GREEN}${BOLD}✓ API stopped gracefully${NC} (after ${i}s)"
        rm -f "$PID_FILE"
        exit 0
    fi
    sleep 1
done

# ── Force kill (SIGKILL) ────────────────────────────────
echo -e "\n${YELLOW}Not stopped after ${TIMEOUT}s. Sending SIGKILL...${NC}"
for pid in "${APP_PIDS[@]}"; do
    kill -9 "$pid" 2>/dev/null || true
done
sleep 1

# ── pkill fallback ──────────────────────────────────────
any_alive=false
for pid in "${APP_PIDS[@]}"; do
    if kill -0 "$pid" 2>/dev/null; then
        any_alive=true
        break
    fi
done

if $any_alive; then
    echo -e "${YELLOW}SIGKILL incomplete. Trying pkill fallback...${NC}"
    pkill -9 -f "JiApp\.Api" 2>/dev/null || true
    sleep 1
fi

# ── Final port check ────────────────────────────────────
for port in 5001 5003; do
    pid=$(discover_pid_by_port "$port")
    if [ -n "$pid" ]; then
        echo -e "${YELLOW}Port $port still held by PID $pid, force-killing...${NC}"
        kill -9 "$pid" 2>/dev/null || true
        sleep 1
    fi
done

# ── Final verification ──────────────────────────────────
for port in 5001 5003; do
    pid=$(discover_pid_by_port "$port")
    if [ -n "$pid" ]; then
        echo -e "${RED}${BOLD}✗ Failed to stop all API processes${NC}"
        echo -e "  Port $port still held by PID $pid"
        echo -e "  Manual: ${BOLD}pkill -9 -f JiApp.Api${NC}"
        exit 1
    fi
done

echo -e "${GREEN}${BOLD}✓ API stopped${NC}"
rm -f "$PID_FILE"
